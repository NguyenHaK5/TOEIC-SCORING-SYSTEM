using Toeic.Domain.Common;
using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>Bộ đề / Test form (ví dụ FORM-A). Có thể tái sử dụng cho nhiều Exam.</summary>
public class Test : BaseEntity
{
    public string TestCode { get; set; } = default!;
    public string TestName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>Thời lượng tính bằng giây.</summary>
    public int ListeningDurationSeconds { get; set; }
    public int ReadingDurationSeconds { get; set; }
    public int WritingDurationSeconds { get; set; }

    public TestStatus Status { get; set; } = TestStatus.DRAFT;
    public Guid? CreatedByUserId { get; set; }

    // Navigation
    public ICollection<ExamTest> ExamTests { get; set; } = new List<ExamTest>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
}
