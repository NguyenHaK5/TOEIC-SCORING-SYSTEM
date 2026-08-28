using Toeic.Domain.Common;

namespace Toeic.Domain.Entities;

/// <summary>
/// Một tiêu chí + mức đánh giá trong RubricSet (ví dụ Grammar - Level 3 - 0.6..0.8).
/// Cũng chính là nơi khai báo tiêu chí "Image Relevance" cho Q1–5 (ISSUE-09) —
/// không cần cột/bảng riêng, chỉ là một Criterion như các tiêu chí khác.
/// </summary>
public class Rubric : BaseEntity
{
    public Guid RubricSetId { get; set; }
    public string Criterion { get; set; } = default!;
    public int Level { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public string? Description { get; set; }

    /// <summary>Quy tắc ánh xạ kết quả phân tích AI/Human → mức điểm (dùng bởi Scoring Engine).</summary>
    public string? ScoringRule { get; set; }

    public decimal Weight { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation
    public RubricSet RubricSet { get; set; } = default!;
    public ICollection<AssessmentCriterionScore> CriterionScores { get; set; } = new List<AssessmentCriterionScore>();
}
