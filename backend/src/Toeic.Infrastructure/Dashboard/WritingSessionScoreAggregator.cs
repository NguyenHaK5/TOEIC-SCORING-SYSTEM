using Microsoft.EntityFrameworkCore;
using Toeic.Application.Dashboard;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.Dashboard;

/// <summary>
/// Xem ghi chú quan trọng ở IWritingSessionScoreAggregator (Application layer) trước khi
/// đọc file này — đặc biệt phần "công thức trung bình cộng là giả định, cần đối chiếu với
/// GetResultAsync".
///
/// Dùng thẳng AppDbContext (không Repository&lt;T&gt;), đúng quyết định kiến trúc đã chốt từ
/// Phase 0 (mục G) và áp dụng xuyên suốt từ Phase 1.
/// </summary>
public sealed class WritingSessionScoreAggregator : IWritingSessionScoreAggregator
{
    private readonly AppDbContext _db;

    public WritingSessionScoreAggregator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<Guid, decimal?>> GetWritingScoresForSessionsAsync(
        IReadOnlyCollection<Guid> examSessionIds,
        CancellationToken cancellationToken)
    {
        var result = examSessionIds.ToDictionary(id => id, _ => (decimal?)null);
        if (examSessionIds.Count == 0)
            return result;

        // 1) Toàn bộ CandidateAnswer thuộc Section=WRITING trong các phiên yêu cầu.
        var writingAnswers = await _db.CandidateAnswers
            .Where(a => examSessionIds.Contains(a.ExamSessionId)
                        && a.Question.Section == Section.WRITING)
            .Select(a => new { a.Id, a.ExamSessionId })
            .ToListAsync(cancellationToken);

        if (writingAnswers.Count == 0)
            return result; // không có câu Writing nào -> toàn bộ null, đúng ý nghĩa

        var answerIds = writingAnswers.Select(a => a.Id).ToList();

        // 2) Human IsFinal — ưu tiên cao nhất (ISSUE-04, Phase 0).
        var finalHuman = await _db.HumanAssessments
            .Where(h => answerIds.Contains(h.CandidateAnswerId) && h.IsFinal)
            .Select(h => new { h.CandidateAnswerId, h.OverallScore })
            .ToListAsync(cancellationToken);

        // 3) Fallback: AI IsCurrent.
        var currentAi = await _db.WritingAssessments
            .Where(w => answerIds.Contains(w.CandidateAnswerId) && w.IsCurrent)
            .Select(w => new { w.CandidateAnswerId, w.OverallScore })
            .ToListAsync(cancellationToken);

        var humanByAnswer = finalHuman.ToDictionary(h => h.CandidateAnswerId, h => h.OverallScore);
        var aiByAnswer = currentAi
            .Where(a => !humanByAnswer.ContainsKey(a.CandidateAnswerId))
            .ToDictionary(a => a.CandidateAnswerId, a => a.OverallScore);

        // 4) Gộp theo ExamSessionId.
        var scoresBySession = new Dictionary<Guid, List<decimal>>();
        foreach (var answer in writingAnswers)
        {
            decimal? score = humanByAnswer.TryGetValue(answer.Id, out var hScore)
                ? hScore
                : aiByAnswer.TryGetValue(answer.Id, out var aScore)
                    ? aScore
                    : null;

            if (score is null)
                continue; // câu chưa được chấm (Pending) -> không tính vào trung bình

            if (!scoresBySession.TryGetValue(answer.ExamSessionId, out var list))
            {
                list = new List<decimal>();
                scoresBySession[answer.ExamSessionId] = list;
            }

            list.Add(score.Value);
        }

        foreach (var sessionId in examSessionIds)
        {
            if (scoresBySession.TryGetValue(sessionId, out var scores) && scores.Count > 0)
            {
                result[sessionId] = Math.Round(scores.Average(), 2);
            }
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetWritingQuestionCountsAsync(
        IReadOnlyCollection<Guid> examSessionIds,
        CancellationToken cancellationToken)
    {
        var result = examSessionIds.ToDictionary(id => id, _ => 0);
        if (examSessionIds.Count == 0)
            return result;

        var counts = await _db.CandidateAnswers
            .Where(a => examSessionIds.Contains(a.ExamSessionId)
                        && a.Question.Section == Section.WRITING)
            .GroupBy(a => a.ExamSessionId)
            .Select(g => new { ExamSessionId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var c in counts)
            result[c.ExamSessionId] = c.Count;

        return result;
    }
}
