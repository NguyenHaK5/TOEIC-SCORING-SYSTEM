using Toeic.Domain.Common;
using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>Hồ sơ thí sinh, mở rộng 1:1 cho User có Role = CANDIDATE.</summary>
public class Candidate : BaseEntity
{
    public Guid UserId { get; set; }
    public string CandidateCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public CandidateStatus Status { get; set; } = CandidateStatus.ACTIVE;

    // Navigation
    public User User { get; set; } = default!;
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
}
