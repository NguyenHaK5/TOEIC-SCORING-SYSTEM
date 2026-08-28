using Toeic.Domain.Common;
using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>Bộ rubric có version, áp dụng cho một QuestionType cụ thể (Q1–5 / Q6–7 / Q8).</summary>
public class RubricSet : BaseEntity
{
    public string Name { get; set; } = default!;
    public QuestionType QuestionType { get; set; }
    public string? Description { get; set; }
    public string Version { get; set; } = default!;
    public RubricSetStatus Status { get; set; } = RubricSetStatus.DRAFT;
    public Guid? CreatedByUserId { get; set; }

    // Navigation
    public ICollection<Rubric> Rubrics { get; set; } = new List<Rubric>();
    public ICollection<WritingAssessment> WritingAssessments { get; set; } = new List<WritingAssessment>();
}
