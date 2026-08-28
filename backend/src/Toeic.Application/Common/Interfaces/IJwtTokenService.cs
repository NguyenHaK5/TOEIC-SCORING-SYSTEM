using Toeic.Domain.Entities;

namespace Toeic.Application.Common.Interfaces;

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

/// <summary>
/// Sinh access token (JWT ký bằng khóa đối xứng) và refresh token (chuỗi ngẫu nhiên opaque,
/// KHÔNG phải JWT — chỉ dùng để trao đổi lấy access token mới qua endpoint /auth/refresh).
/// Implementation: Infrastructure.Security.JwtTokenService.
/// </summary>
public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(User user);

    /// <summary>Sinh 1 chuỗi ngẫu nhiên đủ entropy (256-bit) để làm refresh token thô.</summary>
    string GenerateRefreshTokenValue();

    /// <summary>Hash refresh token thô bằng SHA-256 trước khi lưu DB — không bao giờ lưu raw token.</summary>
    string HashRefreshToken(string rawToken);
}
