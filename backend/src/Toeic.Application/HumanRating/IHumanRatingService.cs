using Toeic.Application.Common.Dtos;
using Toeic.Application.HumanRating.Dtos;

namespace Toeic.Application.HumanRating;

/// <summary>
/// Phase 4 Part 1 — Rater Dashboard: xem danh sách/ chi tiết bài Writing đã submit,
/// nhập điểm theo Criterion (đi qua WritingScoringEngine — Phase 3 Part 2 — để đảm bảo
/// AI và Human luôn dùng chung 1 công thức tính Final Score).
/// Chỉ gọi bởi user có Role = RATER (kiểm tra ở Controller qua [Authorize(Roles = "RATER")]).
/// </summary>
public interface IHumanRatingService
{
    /// <summary>Danh sách bài Writing đã SUBMITTED, theo bộ lọc trạng thái chấm.</summary>
    Task<PagedResult<WritingAnswerQueueItemDto>> GetQueueAsync(
        HumanRatingQueueFilter filter, CancellationToken ct = default);

    /// <summary>Chi tiết 1 câu trả lời Writing: câu hỏi, câu trả lời, AI assessment hiện hành,
    /// toàn bộ HumanAssessment đã có, và Rubric ACTIVE hiện hành cho QuestionType tương ứng.</summary>
    Task<WritingAnswerDetailDto> GetDetailAsync(Guid candidateAnswerId, CancellationToken ct = default);

    /// <summary>
    /// Rater nhập điểm theo Criterion → tính OverallScore qua WritingScoringEngine → lưu
    /// HumanAssessment + AssessmentCriterionScores(AssessmentType=HUMAN). Nếu request.SetAsFinal=true,
    /// mọi HumanAssessment IsFinal=true trước đó (cùng CandidateAnswerId) bị chuyển về false trong
    /// cùng 1 transaction (bảo toàn bất biến "tối đa 1 bản IsFinal=true" — ISSUE-04 Phase 0).
    /// </summary>
    Task<HumanScoreResultDto> SubmitScoreAsync(
        Guid candidateAnswerId, Guid raterId, SubmitHumanScoreRequestDto request, CancellationToken ct = default);

    /// <summary>Phase 4 Part 2 — dataset so sánh AI vs Human theo Criterion, cho Rater UI và Phase 5.</summary>
    Task<PagedResult<AiHumanComparisonDto>> GetAiHumanComparisonAsync(
        AiHumanComparisonFilter filter, CancellationToken ct = default);
}
