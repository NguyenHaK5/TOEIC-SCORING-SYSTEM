using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Toeic.Application.Common.Interfaces;
using Toeic.Domain.Entities;

namespace Toeic.Infrastructure.Security;

/// <summary>
/// Sinh access token dạng JWT (ký HMAC-SHA256) chứa claim sub/role/email/username/jti.
/// Refresh token KHÔNG phải JWT — chỉ là chuỗi ngẫu nhiên 256-bit opaque, tra cứu qua bảng
/// RefreshTokens (lưu dạng hash). Việc tách 2 loại token giúp access token tự-chứa (self-contained,
/// verify được mà không cần query DB), còn refresh token thì luôn phải tra DB để hỗ trợ revoke.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessTokenResult GenerateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("username", user.Username),
            new("full_name", user.FullName),
            //new(ClaimTypes.Role, user.Role.ToString()), 
            new("role", user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(tokenString, expiresAt);
    }

    public string GenerateRefreshTokenValue()
    {
        // 256-bit random, URL-safe base64 — đủ entropy để không cần thêm cơ chế chống đoán mò.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
