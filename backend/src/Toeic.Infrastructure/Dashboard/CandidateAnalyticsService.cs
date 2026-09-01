using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.Dashboard;
using Toeic.Application.Dashboard.Dtos;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.Dashboard;

/// <summary>
/// ⚠️ Giả định tên enum <c>AssessmentSourceType</c> (giá trị AI/HUMAN) cho cột
/// AssessmentCriterionScores.AssessmentType theo đúng mô tả Phase 0 §E.2.14. Nếu code
/// thật của Dragon đặt tên enum khác, đổi lại `using`/tên enum ở file này.
/// </summary>
public sealed class CandidateAnalyticsService : ICandidateAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly IWritingSessionScoreAggregator _writingAggregator;

    public CandidateAnalyticsService(AppDbContext db, IWritingSessionScoreAggregator writingAggregator)
    {
        _db = db;
        _writingAggregator = writingAggregator;
    }

    public async Task<CandidateAnalyticsResponseDto> GetAnalyticsAsync(
        Guid currentUserId,
        AnalyticsQueryParams query,
        CancellationToken cancellationToken)
    {
        var candidateId = await _db.Candidates
            .Where(c => c.UserId == currentUserId)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (candidateId == Guid.Empty)
        {
            throw new NotFoundAppException("Không tìm thấy hồ sơ Candidate cho tài khoản hiện tại.");
        }

        // Chỉ tính trên các phiên ĐÃ NỘP — phiên đang làm dở không phản ánh năng lực thật.
        var submittedSessions = await _db.ExamSessions
            .Where(s => s.CandidateId == candidateId && s.Status == ExamSessionStatus.SUBMITTED)
            .Select(s => new { s.Id, s.SubmittedAt, s.StartedAt, s.ListeningScore, s.ReadingScore })
            .ToListAsync(cancellationToken);

        var response = new CandidateAnalyticsResponseDto();

        if (submittedSessions.Count == 0)
        {
            return response; // chưa có dữ liệu -> trả về 3 danh sách rỗng, không phải lỗi
        }

        var sessionIds = submittedSessions.Select(s => s.Id).ToList();

        response.ObjectiveWeakness = await BuildObjectiveWeaknessAsync(sessionIds, cancellationToken);
        response.WritingWeakness = await BuildWritingWeaknessAsync(sessionIds, cancellationToken);

        var trendRows = submittedSessions
            .Select(s => new SessionScoreRow(
                s.Id, s.SubmittedAt, s.StartedAt, s.ListeningScore, s.ReadingScore))
            .ToList();
        response.ScoreTrend = await BuildScoreTrendAsync(trendRows, query.TrendLimit, cancellationToken);

        return response;
    }

    /// <summary>
    /// Kiểu cụ thể thay cho anonymous type/dynamic — anonymous type KHÔNG thể truyền qua
    /// tham số phương thức riêng biệt vì List&lt;T&gt; không hỗ trợ covariance
    /// (List&lt;AnonymousType&gt; không gán được cho List&lt;dynamic&gt;). Bản nháp đầu
    /// tiên của file này dùng dynamic và sẽ KHÔNG biên dịch được — đã sửa ở đây.
    /// </summary>
    private sealed record SessionScoreRow(
        Guid ExamSessionId,
        DateTime? SubmittedAt,
        DateTime StartedAt,
        decimal? ListeningScore,
        decimal? ReadingScore);

    // ---- Listening/Reading Strength/Weakness (theo Section, QuestionType) ----
    private async Task<List<ObjectiveWeaknessItemDto>> BuildObjectiveWeaknessAsync(
        List<Guid> sessionIds, CancellationToken cancellationToken)
    {
        // PHASE6-ISSUE-05: nhóm theo (Section, QuestionType) đúng yêu cầu; với schema hiện
        // tại QuestionType chỉ có MULTIPLE_CHOICE cho cả 2 Section nên kết quả tương đương
        // nhóm theo Section — sẵn sàng chi tiết hơn nếu Dragon mở rộng schema sau này.
        var raw = await _db.CandidateAnswers
            .Where(a => sessionIds.Contains(a.ExamSessionId)
                        && a.IsCorrect != null
                        && (a.Question.Section == Section.LISTENING
                            || a.Question.Section == Section.READING))
            .GroupBy(a => new { a.Question.Section, a.Question.QuestionType })
            .Select(g => new
            {
                g.Key.Section,
                g.Key.QuestionType,
                CorrectCount = g.Count(x => x.IsCorrect == true),
                TotalCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        return raw
            .Select(r => new ObjectiveWeaknessItemDto
            {
                Section = r.Section.ToString(),
                QuestionType = r.QuestionType.ToString(),
                CorrectCount = r.CorrectCount,
                TotalCount = r.TotalCount,
                AccuracyRate = r.TotalCount == 0
                    ? 0m
                    : Math.Round((decimal)r.CorrectCount / r.TotalCount, 4),
                SampleSize = r.TotalCount
            })
            .OrderBy(r => r.AccuracyRate) // yếu nhất lên đầu
            .ToList();
    }

    // ---- Writing Strength/Weakness (theo CriterionCode) ----
    private async Task<List<WritingCriterionWeaknessItemDto>> BuildWritingWeaknessAsync(
        List<Guid> sessionIds, CancellationToken cancellationToken)
    {
        var writingAnswerIds = await _db.CandidateAnswers
            .Where(a => sessionIds.Contains(a.ExamSessionId) && a.Question.Section == Section.WRITING)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (writingAnswerIds.Count == 0)
            return new List<WritingCriterionWeaknessItemDto>();

        // PHASE6-ISSUE-04: chỉ lấy bản chấm "chính thức" — Human IsFinal ưu tiên, else AI IsCurrent.
        var finalHuman = await _db.HumanAssessments
            .Where(h => writingAnswerIds.Contains(h.CandidateAnswerId) && h.IsFinal)
            .Select(h => new { h.CandidateAnswerId, h.Id })
            .ToListAsync(cancellationToken);

        var answersWithFinalHuman = finalHuman.Select(h => h.CandidateAnswerId).ToHashSet();
        var finalHumanIds = finalHuman.Select(h => h.Id).ToList();

        var currentAi = await _db.WritingAssessments
            .Where(w => writingAnswerIds.Contains(w.CandidateAnswerId) && w.IsCurrent
                        && !answersWithFinalHuman.Contains(w.CandidateAnswerId))
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        if (finalHumanIds.Count == 0 && currentAi.Count == 0)
            return new List<WritingCriterionWeaknessItemDto>();

        var raw = await _db.AssessmentCriterionScores
            .Where(c =>
                (c.AssessmentType == AssessmentSourceType.HUMAN
                    && c.HumanAssessmentId != null
                    && finalHumanIds.Contains(c.HumanAssessmentId.Value))
                ||
                (c.AssessmentType == AssessmentSourceType.AI
                    && c.WritingAssessmentId != null
                    && currentAi.Contains(c.WritingAssessmentId.Value)))
            .GroupBy(c => c.CriterionCode)
            .Select(g => new
            {
                CriterionCode = g.Key,
                AverageScore = g.Average(x => x.Score),
                SampleSize = g.Count()
            })
            .ToListAsync(cancellationToken);

        return raw
            .Select(r => new WritingCriterionWeaknessItemDto
            {
                CriterionCode = r.CriterionCode,
                AverageScore = Math.Round(r.AverageScore, 2),
                SampleSize = r.SampleSize
            })
            .OrderBy(r => r.AverageScore) // yếu nhất lên đầu
            .ToList();
    }

    // ---- Score trend theo thời gian (tái dùng IWritingSessionScoreAggregator từ Part 1) ----
    private async Task<List<ScoreTrendPointDto>> BuildScoreTrendAsync(
        List<SessionScoreRow> submittedSessions, int trendLimit, CancellationToken cancellationToken)
    {
        var limit = trendLimit is < 1 or > 200 ? 20 : trendLimit;

        var recentSessions = submittedSessions
            .OrderByDescending(s => s.SubmittedAt ?? s.StartedAt)
            .Take(limit)
            .OrderBy(s => s.SubmittedAt ?? s.StartedAt) // trả về tăng dần cho line chart
            .ToList();

        var sessionIds = recentSessions.Select(s => s.ExamSessionId).ToList();
        var writingScores = await _writingAggregator.GetWritingScoresForSessionsAsync(sessionIds, cancellationToken);

        return recentSessions.Select(s => new ScoreTrendPointDto
        {
            ExamSessionId = s.ExamSessionId,
            SubmittedAt = s.SubmittedAt,
            ListeningScore = s.ListeningScore,
            ReadingScore = s.ReadingScore,
            WritingScore = writingScores.TryGetValue(s.ExamSessionId, out var w) ? w : null
        }).ToList();
    }
}
