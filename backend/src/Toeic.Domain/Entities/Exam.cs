using Toeic.Domain.Common;
using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>Kỳ thi. Nhiều Test form (FORM-A, FORM-B...) có thể gán vào 1 Exam qua ExamTests.</summary>
public class Exam : BaseEntity
{
    public string ExamCode { get; set; } = default!;
    public string ExamName { get; set; } = default!;
    public string? Description { get; set; }
    public DateOnly ExamDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.DRAFT;
    public Guid? CreatedByUserId { get; set; }

    // Navigation
    public ICollection<ExamTest> ExamTests { get; set; } = new List<ExamTest>();
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
}
