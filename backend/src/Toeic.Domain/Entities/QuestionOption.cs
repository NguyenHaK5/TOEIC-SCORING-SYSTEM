using Toeic.Domain.Common;

namespace Toeic.Domain.Entities;

/// <summary>
/// Phương án trắc nghiệm. IsCorrect = TRUE chính là Answer Key — không tách bảng riêng (KISS, Phase 0).
/// </summary>
public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public char OptionLabel { get; set; } // A/B/C/D
    public string OptionText { get; set; } = default!;
    public bool IsCorrect { get; set; }

    // Navigation
    public Question Question { get; set; } = default!;
}
