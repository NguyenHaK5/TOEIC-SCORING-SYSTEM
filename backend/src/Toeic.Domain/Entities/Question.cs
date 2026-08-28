using Toeic.Domain.Common;
using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>
/// Câu hỏi thuộc một Test. Validation điều kiện (vd Section=LISTENING cần AudioUrl)
/// được enforce ở Application/FluentValidation, KHÔNG dùng CHECK constraint (Phase 0, mục ghi chú bổ sung).
/// </summary>
public class Question : BaseEntity
{
    public Guid TestId { get; set; }
    public Section Section { get; set; }
    public QuestionType QuestionType { get; set; }
    public int QuestionNumber { get; set; }
    public string QuestionText { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }

    /// <summary>Từ/cụm từ bắt buộc phải xuất hiện trong bài viết — chủ yếu dùng cho Q1–5.</summary>
    public string? RequiredWords { get; set; }

    public decimal? Points { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation
    public Test Test { get; set; } = default!;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<CandidateAnswer> CandidateAnswers { get; set; } = new List<CandidateAnswer>();
}
