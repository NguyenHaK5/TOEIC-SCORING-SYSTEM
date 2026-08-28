using System.ComponentModel.DataAnnotations;
using Toeic.Domain.Enums;

namespace Toeic.Application.Auth.Dtos;

/// <summary>
/// Dùng cho cả 2 endpoint đăng ký (public self-register và admin-tạo-staff).
/// Role bị controller ghi đè tùy theo endpoint/caller — Application layer không tự suy luận quyền
/// từ chính request body (tránh việc client tự đặt Role=ADMIN trong request public).
/// </summary>
public class RegisterRequest
{
    [Required, MinLength(3), MaxLength(100)]
    public string Username { get; set; } = default!;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = default!;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = default!;
}

/// <summary>
/// Dùng riêng cho endpoint ADMIN-only tạo tài khoản ADMIN/RATER (KHÔNG dùng để tạo CANDIDATE —
/// endpoint public /auth/register đã lo việc đó và luôn ép Role=CANDIDATE bất kể client gửi gì).
/// </summary>
public class RegisterStaffRequest : RegisterRequest
{
    [Required]
    public UserRole Role { get; set; }
}

public class LoginRequest
{
    [Required]
    public string UsernameOrEmail { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = default!;
}

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = default!;
}

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public UserRole Role { get; set; }
    public Guid? CandidateId { get; set; }
}

public class AuthResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = default!;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public UserProfileDto User { get; set; } = default!;
}
