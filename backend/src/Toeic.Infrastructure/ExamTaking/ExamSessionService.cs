using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.ExamTaking;
using Toeic.Application.ExamTaking.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.ExamTaking;

/// <summary>
/// Implement IExamSessionService dùng thẳng AppDbContext. Đây là module "phòng thi trực tuyến"
/// (mục 16, prompt gốc) — server luôn là nguồn xác thực thời gian cuối cùng, KHÔNG dựa vào
/// đồng hồ Frontend.
/// </summary>
public class ExamSessionService : IExamSessionService
{
    private readonly AppDbContext _db;

    public ExamSessionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ExamSessionDto> StartAsync(Guid candidateUserId, StartExamSessionRequest request, CancellationToken ct = default)
    {
        var candidate = await ResolveCandidateAsync(candidateUserId, ct);

        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == request.ExamId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy Exam.");

        if (exam.Status != ExamStatus.ONGOING)
        {
            throw new ConflictAppException($"Exam hiện chưa mở để làm bài (trạng thái hiện tại: {exam.Status}). Chỉ Exam đang ONGOING mới bắt đầu làm bài được.");
        }

        // ISSUE-06: nếu đã có phiên đang NOT_STARTED/IN_PROGRESS cho Exam này -> resume thay vì tạo mới.
        var existing = await _db.ExamSessions
            .Include(s => s.Test)
            .Include(s => s.Exam)
            .FirstOrDefaultAsync(s => s.ExamId == exam.Id && s.CandidateId == candidate.Id
                && (s.Status == ExamSessionStatus.NOT_STARTED || s.Status == ExamSessionStatus.IN_PROGRESS), ct);

        if (existing is not null)
        {
            var stillActive = await AutoSubmitIfExpiredAsync(existing, ct);
            if (stillActive)
            {
                return ToDto(existing);
            }
            // Nếu vừa bị auto-submit vì hết giờ, rơi tiếp xuống dưới để tạo phiên mới (chính sách
            // retake chưa giới hạn ở Phase 1 — có thể siết lại ở phase sau nếu nghiệp vụ yêu cầu).
        }

        // ISSUE-01: chỉ chọn Test đang được gán và IsActive cho Exam này.
        var activeTestIds = await _db.ExamTests
            .Where(et => et.ExamId == exam.Id && et.IsActive)
            .Select(et => et.TestId)
            .ToListAsync(ct);

        if (activeTestIds.Count == 0)
        {
            throw new ConflictAppException("Exam chưa có Test nào đang được gán/kích hoạt — không thể bắt đầu làm bài.");
        }

        // Chọn ngẫu nhiên 1 trong các Test form đang active (hỗ trợ nhiều form FORM-A/FORM-B chống gian lận).
        var chosenTestId = activeTestIds[Random.Shared.Next(activeTestIds.Count)];
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.Id == chosenTestId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy Test đã gán cho Exam.");

        var now = DateTime.UtcNow;
        var totalDurationSeconds = test.ListeningDurationSeconds + test.ReadingDurationSeconds + test.WritingDurationSeconds;

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            CandidateId = candidate.Id,
            TestId = test.Id,
            StartedAt = now,
            ExpiresAt = now.AddSeconds(totalDurationSeconds), // ISSUE-07: chốt 1 lần, không tính lại từ Test sau này.
            Status = ExamSessionStatus.IN_PROGRESS, // Bắt đầu làm bài ngay khi tạo phiên — không dùng NOT_STARTED.
            CreatedAt = now,
        };

        _db.ExamSessions.Add(session);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Race condition: 2 request gần như đồng thời cùng tạo phiên cho cùng (Exam, Candidate)
            // (defense-in-depth cho ISSUE-06 — partial unique index ở DB là chốt chặn cuối cùng).
            throw new ConflictAppException("Bạn đã có 1 phiên thi đang diễn ra cho kỳ thi này. Vui lòng tải lại trang.");
        }

        session.Test = test;
        session.Exam = exam;
        return ToDto(session);
    }

    public async Task<ExamSessionDto> GetByIdAsync(Guid sessionId, Guid requestingUserId, CancellationToken ct = default)
    {
        var session = await LoadOwnedSessionAsync(sessionId, requestingUserId, ct);
        await AutoSubmitIfExpiredAsync(session, ct);
        return ToDto(session);
    }

    public async Task<IReadOnlyList<ExamSessionQuestionDto>> GetQuestionsAsync(Guid sessionId, Guid requestingUserId, CancellationToken ct = default)
    {
        var session = await LoadOwnedSessionAsync(sessionId, requestingUserId, ct);
        await AutoSubmitIfExpiredAsync(session, ct);

        var questions = await _db.Questions
            .Include(q => q.Options)
            .Where(q => q.TestId == session.TestId)
            .OrderBy(q => q.Section).ThenBy(q => q.QuestionNumber)
            .ToListAsync(ct);

        var myAnswers = await _db.CandidateAnswers
            .Where(a => a.ExamSessionId == sessionId)
            .ToListAsync(ct);
        var answersByQuestion = myAnswers.ToDictionary(a => a.QuestionId);

        return questions.Select(q =>
        {
            answersByQuestion.TryGetValue(q.Id, out var myAnswer);
            return new ExamSessionQuestionDto
            {
                QuestionId = q.Id,
                Section = q.Section,
                QuestionType = q.QuestionType,
                QuestionNumber = q.QuestionNumber,
                QuestionText = q.QuestionText,
                ImageUrl = q.ImageUrl,
                AudioUrl = q.AudioUrl,
                RequiredWords = q.RequiredWords,
                Options = q.Options
                    .OrderBy(o => o.OptionLabel)
                    .Select(o => new CandidateVisibleOptionDto
                    {
                        QuestionOptionId = o.Id,
                        OptionLabel = o.OptionLabel,
                        OptionText = o.OptionText,
                        // Cố ý KHÔNG map IsCorrect — Candidate không được thấy Answer Key.
                    })
                    .ToList(),
                MySelectedOptionId = myAnswer?.SelectedOptionId,
                MyAnswerText = myAnswer?.AnswerText,
                MyAnswerStatus = myAnswer?.Status ?? AnswerStatus.NOT_ANSWERED,
            };
        }).ToList();
    }

    public async Task<CandidateAnswerDto> SaveAnswerAsync(Guid sessionId, Guid requestingUserId, SaveAnswerRequest request, CancellationToken ct = default)
    {
        var session = await LoadOwnedSessionAsync(sessionId, requestingUserId, ct);

        var stillActive = await AutoSubmitIfExpiredAsync(session, ct);
        if (!stillActive)
        {
            throw new ConflictAppException("Phiên thi đã hết giờ và được tự động nộp bài — không thể lưu thêm câu trả lời.");
        }
        if (session.Status != ExamSessionStatus.IN_PROGRESS)
        {
            throw new ConflictAppException($"Phiên thi không ở trạng thái đang làm bài (hiện tại: {session.Status}).");
        }

        var question = await _db.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId && q.TestId == session.TestId, ct)
            ?? throw new NotFoundAppException("Câu hỏi không thuộc bộ đề của phiên thi này.");

        ValidateAnswerShape(question, request);

        var now = DateTime.UtcNow;

        try
        {
            var existingAnswer = await _db.CandidateAnswers
                .FirstOrDefaultAsync(a => a.ExamSessionId == sessionId && a.QuestionId == request.QuestionId, ct);

            if (existingAnswer is null)
            {
                existingAnswer = new CandidateAnswer
                {
                    Id = Guid.NewGuid(),
                    ExamSessionId = sessionId,
                    QuestionId = request.QuestionId,
                };
                _db.CandidateAnswers.Add(existingAnswer);
            }

            existingAnswer.SelectedOptionId = request.SelectedOptionId;
            existingAnswer.AnswerText = request.AnswerText;
            existingAnswer.AnsweredAt = now;
            existingAnswer.Status = AnswerStatus.ANSWERED;
            // IsCorrect/Score cố ý để NULL ở đây — thuộc trách nhiệm Objective Scoring Engine (Phase 2),
            // tách biệt "ghi nhận câu trả lời" khỏi "chấm điểm" theo đúng ranh giới module Phase 0.

            await _db.SaveChangesAsync(ct);

            return new CandidateAnswerDto
            {
                CandidateAnswerId = existingAnswer.Id,
                QuestionId = existingAnswer.QuestionId,
                SelectedOptionId = existingAnswer.SelectedOptionId,
                AnswerText = existingAnswer.AnswerText,
                Status = existingAnswer.Status,
                AnsweredAt = existingAnswer.AnsweredAt,
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictAppException("Câu trả lời vừa được cập nhật từ nơi khác (thiết bị/tab khác). Vui lòng tải lại và thử lưu lại.");
        }
    }

    public async Task<ExamSessionDto> SubmitAsync(Guid sessionId, Guid requestingUserId, CancellationToken ct = default)
    {
        var session = await LoadOwnedSessionAsync(sessionId, requestingUserId, ct);

        var stillActive = await AutoSubmitIfExpiredAsync(session, ct);
        if (!stillActive)
        {
            return ToDto(session); // Đã tự động nộp bài vì hết giờ — idempotent, không coi là lỗi.
        }

        if (session.Status == ExamSessionStatus.SUBMITTED)
        {
            return ToDto(session); // Idempotent: bấm nộp bài 2 lần vẫn thành công.
        }

        if (session.Status != ExamSessionStatus.IN_PROGRESS)
        {
            throw new ConflictAppException($"Không thể nộp bài khi phiên thi đang ở trạng thái {session.Status}.");
        }

        try
        {
            session.Status = ExamSessionStatus.SUBMITTED;
            session.SubmittedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictAppException("Phiên thi vừa được cập nhật từ nơi khác. Vui lòng tải lại trang và thử lại.");
        }

        return ToDto(session);
    }

    // --- Helpers ------------------------------------------------------------

    private async Task<Candidate> ResolveCandidateAsync(Guid userId, CancellationToken ct)
        => await _db.Candidates.FirstOrDefaultAsync(c => c.UserId == userId, ct)
           ?? throw new ForbiddenAppException("Tài khoản hiện tại không có hồ sơ Candidate.");

    private async Task<ExamSession> LoadOwnedSessionAsync(Guid sessionId, Guid requestingUserId, CancellationToken ct)
    {
        var session = await _db.ExamSessions
            .Include(s => s.Exam)
            .Include(s => s.Test)
            .Include(s => s.Candidate)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy phiên thi.");

        if (session.Candidate.UserId != requestingUserId)
        {
            throw new ForbiddenAppException("Bạn không có quyền truy cập phiên thi này.");
        }

        return session;
    }

    /// <summary>
    /// Kiểm tra hết giờ theo ExpiresAt (server-side, ISSUE-07). Nếu đã hết giờ và còn IN_PROGRESS,
    /// tự động chuyển sang SUBMITTED (chốt SubmittedAt = ExpiresAt, phản ánh đúng thời điểm hết hạn
    /// thay vì thời điểm phát hiện). Đây là kiểm tra "lazy" — chỉ chạy khi có request chạm tới phiên
    /// thi, MVP chưa có background job chủ động quét hết giờ (ghi nhận là hạn chế đã biết, xem
    /// phase-tracker — có thể bổ sung IHostedService định kỳ ở phase sau nếu cần Status cập nhật
    /// ngay cả khi không có request nào chạm tới phiên).
    /// Trả về false nếu phiên VỪA (hoặc đã) bị auto-submit do hết giờ; true nếu vẫn còn hiệu lực
    /// hoặc đang ở 1 trạng thái khác không liên quan đến hết giờ (SUBMITTED thủ công, CANCELLED...).
    /// </summary>
    private async Task<bool> AutoSubmitIfExpiredAsync(ExamSession session, CancellationToken ct)
    {
        if (session.Status != ExamSessionStatus.IN_PROGRESS)
        {
            return true; // Không phải IN_PROGRESS thì không có gì để auto-expire ở đây.
        }

        if (DateTime.UtcNow < session.ExpiresAt)
        {
            return true;
        }

        session.Status = ExamSessionStatus.SUBMITTED;
        session.SubmittedAt = session.ExpiresAt;
        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return false;
    }

    private static void ValidateAnswerShape(Question question, SaveAnswerRequest request)
    {
        if (question.QuestionType == QuestionType.MULTIPLE_CHOICE)
        {
            if (request.SelectedOptionId is null)
            {
                throw new ValidationAppException("Câu trắc nghiệm bắt buộc phải có SelectedOptionId.");
            }
            if (!question.Options.Any(o => o.Id == request.SelectedOptionId))
            {
                throw new ValidationAppException("SelectedOptionId không thuộc câu hỏi này.");
            }
            if (!string.IsNullOrWhiteSpace(request.AnswerText))
            {
                throw new ValidationAppException("Câu trắc nghiệm không được kèm AnswerText.");
            }
        }
        else
        {
            if (request.SelectedOptionId is not null)
            {
                throw new ValidationAppException("Câu hỏi Writing không được kèm SelectedOptionId.");
            }
            if (string.IsNullOrWhiteSpace(request.AnswerText))
            {
                throw new ValidationAppException("Câu hỏi Writing bắt buộc phải có AnswerText.");
            }
        }
    }

    private static ExamSessionDto ToDto(ExamSession s)
    {
        var remaining = (int)Math.Max(0, (s.ExpiresAt - DateTime.UtcNow).TotalSeconds);
        return new ExamSessionDto
        {
            ExamSessionId = s.Id,
            ExamId = s.ExamId,
            ExamName = s.Exam?.ExamName ?? string.Empty,
            TestId = s.TestId,
            TestCode = s.Test?.TestCode ?? string.Empty,
            StartedAt = s.StartedAt,
            ExpiresAt = s.ExpiresAt,
            SubmittedAt = s.SubmittedAt,
            Status = s.Status,
            RemainingSeconds = s.Status == ExamSessionStatus.IN_PROGRESS ? remaining : 0,
            ListeningDurationSeconds = s.Test?.ListeningDurationSeconds ?? 0,
            ReadingDurationSeconds = s.Test?.ReadingDurationSeconds ?? 0,
            WritingDurationSeconds = s.Test?.WritingDurationSeconds ?? 0,
        };
    }
}
