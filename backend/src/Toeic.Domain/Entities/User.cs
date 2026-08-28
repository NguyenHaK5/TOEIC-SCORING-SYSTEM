using Toeic.Domain.Common;
using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>
/// Bảng tài khoản DUY NHẤT cho cả 3 role: ADMIN, RATER, CANDIDATE (ISSUE-08, Phase 0).
/// Candidate là hồ sơ mở rộng (1:1) áp dụng riêng cho Role = CANDIDATE.
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.ACTIVE;

    // Navigation
    public Candidate? Candidate { get; set; }
    public ICollection<HumanAssessment> HumanAssessments { get; set; } = new List<HumanAssessment>();
}
