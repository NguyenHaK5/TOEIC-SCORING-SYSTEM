using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Exceptions; // TODO: đổi namespace nếu project bạn đặt khác
using Toeic.Application.ObjectiveScoring;
using Toeic.Application.ObjectiveScoring.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Domain.Scoring;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.ObjectiveScoring;

/// <summary>
/// Objective Scoring Engine (Phase 2). Dùng thẳng <see cref="AppDbContext"/>, không qua
/// Repository&lt;T&gt; tổng quát — nhất quán với quyết định đã ghi ở Phase 1 (README Part 2).
/// </summary>
public sealed class ObjectiveScoringService : IObjectiveScoringService
{
    private static readonly Section[] ObjectiveSections =
    {
        Section.LISTENING,
        Section.READING,
    };

    private readonly AppDbContext _db;
    private readonly Toeic.Application.WritingAssessment.IWritingResultService _writingResultService;

    public ObjectiveScoringService(AppDbContext db, Application.WritingAssessment.IWritingResultService writingResultService)
    {
        _db = db;
        _writingResultService = writingResultService;
    }

    public async Task<ObjectiveScoringSummaryDto> ScoreSessionAsync(Guid examSessionId, CancellationToken ct = default)
    {
        var session = await _db.ExamSessions
            .FirstOrDefaultAsync(s => s.Id == examSessionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy phiên thi.");

        // Chỉ chấm khi phiên đã SUBMITTED — trong lúc IN_PROGRESS, CandidateAnswers vẫn có thể
        // bị auto-save ghi đè bất kỳ lúc nào nên chấm ở giai đoạn này là vô nghĩa và có thể gây
        // race condition với SaveAnswerAsync.
        if (session.Status != ExamSessionStatus.SUBMITTED)
        {
            throw new ConflictAppException(
                "Chỉ có thể chấm điểm khách quan khi phiên thi đã ở trạng thái SUBMITTED.");
        }

        // Toàn bộ câu hỏi Listening/Reading dạng MULTIPLE_CHOICE thuộc Test đã gán cho phiên này,
        // kèm QuestionOptions để tra Answer Key (IsCorrect).
        var questions = await _db.Questions
            .Where(q => q.TestId == session.TestId
                        && ObjectiveSections.Contains(q.Section)
                        && q.QuestionType == QuestionType.MULTIPLE_CHOICE)
            .Include(q => q.Options)
            .AsSplitQuery()
            .ToListAsync(ct);

        var existingAnswers = await _db.CandidateAnswers
            .Where(a => a.ExamSessionId == examSessionId)
            .ToDictionaryAsync(a => a.QuestionId, a => a, ct);

        int listeningTotal = 0, listeningCorrect = 0, readingTotal = 0, readingCorrect = 0;
        decimal listeningScore = 0m, readingScore = 0m;

        foreach (var question in questions)
        {
            existingAnswers.TryGetValue(question.Id, out var answer);

            var result = ObjectiveScoringEngine.EvaluateMultipleChoice(
                answer?.SelectedOptionId, question.Options.ToList(), question.Points);

            if (answer is null)
            {
                // Câu thí sinh không đụng tới lúc thi (không có CandidateAnswer nào được auto-save).
                // Tạo 1 dòng NOT_ANSWERED để thống kê đúng/sai đầy đủ và nhất quán, thay vì chỉ
                // đếm ngầm trong bộ nhớ — dữ liệu này cần tồn tại trong DB cho Phase 4 (Rater)
                // và Phase 6 (Dashboard thống kê lỗi thường gặp) truy vấn lại sau này.
                answer = new CandidateAnswer
                {
                    Id = Guid.NewGuid(),
                    ExamSessionId = examSessionId,
                    QuestionId = question.Id,
                    SelectedOptionId = null,
                    Status = AnswerStatus.NOT_ANSWERED,
                };
                _db.CandidateAnswers.Add(answer);
            }

            answer.IsCorrect = result.IsCorrect;
            answer.Score = result.Score;

            if (question.Section == Section.LISTENING)
            {
                listeningTotal++;
                listeningScore += result.Score;
                if (result.IsCorrect) listeningCorrect++;
            }
            else
            {
                readingTotal++;
                readingScore += result.Score;
                if (result.IsCorrect) readingCorrect++;
            }
        }

        session.ListeningScore = listeningScore;
        session.ReadingScore = readingScore;
        // WritingScore thuộc Phase 3/4 (AI/Human Assessment) — Phase 2 không đụng tới, chỉ cộng
        // vào TotalScore nếu đã có sẵn giá trị (ví dụ trường hợp Writing được chấm trước, dù
        // không phải thứ tự thông thường theo roadmap).
        session.TotalScore = listeningScore + readingScore + (session.WritingScore ?? 0m);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // xmin conflict — một request khác (auto-submit khác, hoặc gọi ScoreSessionAsync
            // song song) đã ghi đè trước. Theo đúng cách xử lý đã áp dụng ở ExamSessionService
            // (Phase 1 Part 4): map sang ConflictAppException thay vì để lộ exception EF Core.
            throw new ConflictAppException(
                "Phiên thi vừa được cập nhật bởi một yêu cầu khác, vui lòng thử lại.", ex);
        }

        return new ObjectiveScoringSummaryDto(
            examSessionId,
            listeningScore, listeningCorrect, listeningTotal,
            readingScore, readingCorrect, readingTotal,
            session.TotalScore);
    }

    public async Task<ExamSessionResultDto> GetResultAsync(Guid examSessionId, Guid requestingUserId, CancellationToken ct = default)
    {
        var session = await _db.ExamSessions
            .FirstOrDefaultAsync(s => s.Id == examSessionId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy phiên thi.");

        // Kiểm tra quyền sở hữu qua Candidates.UserId — không dùng navigation property để tránh
        // giả định về tên property có thể khác trong codebase thật của bạn (điều chỉnh nếu cần).
        var ownerUserId = await _db.Candidates
            .Where(c => c.Id == session.CandidateId)
            .Select(c => c.UserId)
            .FirstOrDefaultAsync(ct);

        if (ownerUserId != requestingUserId)
        {
            throw new ForbiddenAppException("Bạn không có quyền xem kết quả của phiên thi này.");
        }

        if (session.Status != ExamSessionStatus.SUBMITTED)
        {
            throw new ConflictAppException("Phiên thi chưa được nộp, chưa có kết quả để xem.");
        }

        var alreadyScored = await _db.CandidateAnswers
            .AnyAsync(a => a.ExamSessionId == examSessionId && a.IsCorrect != null, ct);

        var summary = alreadyScored
            ? await BuildSummaryFromExistingAsync(session, ct)
            : await ScoreSessionAsync(examSessionId, ct); // lazy scoring — xem ghi chú ở interface


        var writingResult = session.Status == ExamSessionStatus.SUBMITTED
            ? await _writingResultService.GetSectionResultAsync(examSessionId, ct)
            : new Toeic.Application.WritingAssessment.Dtos.WritingSectionResultDto();


        return new ExamSessionResultDto(
            session.Id,
            session.Status.ToString(),
            session.SubmittedAt,
            session.ListeningScore,
            summary.ListeningCorrectCount,
            summary.ListeningTotalCount,
            session.ReadingScore,
            summary.ReadingCorrectCount,
            summary.ReadingTotalCount,
            //session.WritingScore,
            //session.TotalScore,
            writingResult.OverallScore, // Lấy trực tiếp từ kết quả Writing đã phân giải
            (session.ListeningScore ?? 0m) + (session.ReadingScore ?? 0m) + (writingResult.OverallScore ?? 0m), // Cập nhật TotalScore

            writingResult);
    }

    /// <summary>
    /// Dựng lại summary (đếm đúng/sai) từ dữ liệu CandidateAnswers đã chấm sẵn trong DB —
    /// tránh chấm lại không cần thiết mỗi lần Candidate mở trang kết quả.
    /// </summary>
    private async Task<ObjectiveScoringSummaryDto> BuildSummaryFromExistingAsync(ExamSession session, CancellationToken ct)
    {
        var rows = await (
            from a in _db.CandidateAnswers
            join q in _db.Questions on a.QuestionId equals q.Id
            where a.ExamSessionId == session.Id
                  && ObjectiveSections.Contains(q.Section)
                  && q.QuestionType == QuestionType.MULTIPLE_CHOICE
            select new { a.IsCorrect, q.Section }
        ).ToListAsync(ct);

        int listeningTotal = rows.Count(r => r.Section == Section.LISTENING);
        int listeningCorrect = rows.Count(r => r.Section == Section.LISTENING && r.IsCorrect == true);
        int readingTotal = rows.Count(r => r.Section == Section.READING);
        int readingCorrect = rows.Count(r => r.Section == Section.READING && r.IsCorrect == true);

        return new ObjectiveScoringSummaryDto(
            session.Id,
            session.ListeningScore ?? 0m, listeningCorrect, listeningTotal,
            session.ReadingScore ?? 0m, readingCorrect, readingTotal,
            session.TotalScore);
    }
}
