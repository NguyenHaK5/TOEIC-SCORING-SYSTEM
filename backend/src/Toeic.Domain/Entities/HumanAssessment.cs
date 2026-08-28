using Toeic.Domain.Common;

namespace Toeic.Domain.Entities;

/// <summary>
/// Đánh giá thủ công của Rater cho 1 câu Writing. Một câu có thể được nhiều Rater chấm (1:N).
/// IsFinal đánh dấu bản CHÍNH THỨC hiển thị cho Candidate (ISSUE-04); partial unique index
/// UNIQUE(CandidateAnswerId) WHERE IsFinal = TRUE đảm bảo chỉ có tối đa 1 bản chính thức.
/// Quy tắc nghiệp vụ: điểm hiển thị = HumanAssessment.IsFinal=TRUE nếu tồn tại,
/// ngược lại fallback = WritingAssessment.IsCurrent=TRUE.
/// Các cột điểm theo tiêu chí cứng đã được BỎ (ISSUE-03) — dùng chung AssessmentCriterionScores với AI.
/// </summary>
public class HumanAssessment : BaseEntity
{
    public Guid CandidateAnswerId { get; set; }

    /// <summary>User.Role phải là RATER.</summary>
    public Guid RaterId { get; set; }

    public decimal? OverallScore { get; set; }
    public string? Feedback { get; set; }
    public bool IsFinal { get; set; }

    // Navigation
    public CandidateAnswer CandidateAnswer { get; set; } = default!;
    public User Rater { get; set; } = default!;
    public ICollection<AssessmentCriterionScore> CriterionScores { get; set; } = new List<AssessmentCriterionScore>();
}
