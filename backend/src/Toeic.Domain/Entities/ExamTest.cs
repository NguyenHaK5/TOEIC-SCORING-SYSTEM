namespace Toeic.Domain.Entities;

/// <summary>
/// Bảng N:N gán Test cho Exam (ISSUE-01 — Phase 0). Composite PK (ExamId, TestId).
/// Quy tắc nghiệp vụ (Application layer): khi tạo ExamSession, TestId bắt buộc phải
/// tồn tại trong ExamTests với ExamId tương ứng và IsActive = true.
/// </summary>
public class ExamTest
{
    public Guid ExamId { get; set; }
    public Guid TestId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Exam Exam { get; set; } = default!;
    public Test Test { get; set; } = default!;
}
