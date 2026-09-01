using Microsoft.EntityFrameworkCore;
using Toeic.Application.Scoring;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.Scoring;

/// <summary>
/// Implement ISSUE-04 Phase 0. ⚠️ Giả định entity giống hệt các file Phase 3 Part 4 / Phase 4 Part 1
/// đã dùng (WritingAssessments.IsCurrent, HumanAssessments.IsFinal, CandidateAnswers.AnswerText) —
/// xem PHASE4_PART2_INTEGRATION_GUIDE.md nếu build lỗi.
/// </summary>
public class WritingScoreResolver : IWritingScoreResolver
{
    private readonly AppDbContext _db;

    public WritingScoreResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OfficialWritingScoreDto> ResolveAsync(Guid candidateAnswerId, CancellationToken ct = default)
    {
        var result = await ResolveManyAsync(new[] { candidateAnswerId }, ct);
        return result.TryGetValue(candidateAnswerId, out var dto)
            ? dto
            : new OfficialWritingScoreDto
            {
                CandidateAnswerId = candidateAnswerId,
                Source = OfficialScoreSource.None,
                Status = OfficialScoreStatus.NotAnswered
            };
    }

    public async Task<IReadOnlyDictionary<Guid, OfficialWritingScoreDto>> ResolveManyAsync(
        IEnumerable<Guid> candidateAnswerIds, CancellationToken ct = default)
    {
        var ids = candidateAnswerIds.Distinct().ToList();
        var resultMap = new Dictionary<Guid, OfficialWritingScoreDto>();
        if (ids.Count == 0)
        {
            return resultMap;
        }

        // 1) CandidateAnswers hiện có (để phân biệt Pending vs NotAnswered khi không có nguồn nào).
        var answers = await _db.CandidateAnswers
            .Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.AnswerText })
            .ToListAsync(ct);

        // 2) Ưu tiên 1: HumanAssessment.IsFinal = true (ISSUE-04 Phase 0).
        var finalHuman = await _db.HumanAssessments
            .Where(h => ids.Contains(h.CandidateAnswerId) && h.IsFinal)
            .Select(h => new { h.CandidateAnswerId, h.OverallScore })
            .ToListAsync(ct);
        var finalHumanMap = finalHuman.ToDictionary(h => h.CandidateAnswerId, h => h.OverallScore);

        // 3) Fallback: WritingAssessment.IsCurrent = true (điểm AI hiện hành).
        var currentAi = await _db.WritingAssessments
            .Where(w => ids.Contains(w.CandidateAnswerId) && w.IsCurrent)
            .Select(w => new { w.CandidateAnswerId, w.OverallScore })
            .ToListAsync(ct);
        var currentAiMap = currentAi.ToDictionary(w => w.CandidateAnswerId, w => w.OverallScore);

        foreach (var answer in answers)
        {
            var id = answer.Id;

            if (finalHumanMap.TryGetValue(id, out var humanScore))
            {
                resultMap[id] = new OfficialWritingScoreDto
                {
                    CandidateAnswerId = id,
                    Score = humanScore,
                    Source = OfficialScoreSource.Human,
                    Status = OfficialScoreStatus.Scored
                };
                continue;
            }

            if (currentAiMap.TryGetValue(id, out var aiScore))
            {
                resultMap[id] = new OfficialWritingScoreDto
                {
                    CandidateAnswerId = id,
                    Score = aiScore,
                    Source = OfficialScoreSource.Ai,
                    Status = OfficialScoreStatus.Scored
                };
                continue;
            }

            // Không có Human final, không có AI current: câu chưa được chấm.
            // Nếu đã có AnswerText -> đang chờ AI/Human chấm (Pending); nếu không -> chưa trả lời.
            var hasAnswerText = !string.IsNullOrWhiteSpace(answer.AnswerText);
            resultMap[id] = new OfficialWritingScoreDto
            {
                CandidateAnswerId = id,
                Score = null,
                Source = OfficialScoreSource.None,
                Status = hasAnswerText ? OfficialScoreStatus.Pending : OfficialScoreStatus.NotAnswered
            };
        }

        return resultMap;
    }
}
