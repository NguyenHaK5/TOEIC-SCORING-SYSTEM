using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Toeic.Application.Auth;
using Toeic.Application.Auth.Dtos;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.Common.Interfaces;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;
using Toeic.Infrastructure.Security;

namespace Toeic.Infrastructure.Auth;

/// <summary>
/// Implement IAuthService dùng thẳng AppDbContext (đúng quyết định Phase 0, mục G — không thêm
/// tầng Repository&lt;T&gt; tổng quát vì DbSet đã là Repository/Unit of Work).
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<UserProfileDto> RegisterAsync(RegisterRequest request, UserRole role, CancellationToken ct = default)
    {
        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(
            u => u.Username == normalizedUsername || u.Email == normalizedEmail, ct);
        if (exists)
        {
            throw new ConflictAppException("Username hoặc Email đã được sử dụng.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            Status = UserStatus.ACTIVE,
            CreatedAt = now,
        };
        _db.Users.Add(user);

        Candidate? candidate = null;
        if (role == UserRole.CANDIDATE)
        {
            candidate = new Candidate
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CandidateCode = GenerateCandidateCode(),
                FullName = user.FullName,
                Status = CandidateStatus.ACTIVE,
                CreatedAt = now,
            };
            _db.Candidates.Add(candidate);
        }

        await _db.SaveChangesAsync(ct);

        return ToProfileDto(user, candidate?.Id);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var identifier = request.UsernameOrEmail.Trim();
        var identifierLower = identifier.ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Username == identifier || u.Email == identifierLower, ct);

        // Cố ý dùng cùng 1 thông báo lỗi cho "không tồn tại" và "sai mật khẩu" để không lộ
        // thông tin username/email nào tồn tại trong hệ thống (chống user enumeration).
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Username/Email hoặc mật khẩu không đúng.");
        }

        if (user.Status != UserStatus.ACTIVE)
        {
            throw new UnauthorizedAppException("Tài khoản đang bị khóa hoặc không hoạt động.");
        }

        return await IssueTokenPairAsync(user, ipAddress, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var now = DateTime.UtcNow;

        var existing = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        if (existing is null)
        {
            throw new UnauthorizedAppException("Refresh token không hợp lệ.");
        }

        if (existing.IsRevoked)
        {
            // Token đã bị revoke nhưng vẫn được đem dùng lại -> dấu hiệu bị đánh cắp.
            // Revoke toàn bộ token còn active của user này để chặn kịp thời.
            await RevokeAllActiveTokensForUserAsync(existing.UserId, now, ct);
            throw new UnauthorizedAppException("Refresh token đã bị thu hồi. Vui lòng đăng nhập lại.");
        }

        if (existing.IsExpired(now))
        {
            throw new UnauthorizedAppException("Refresh token đã hết hạn. Vui lòng đăng nhập lại.");
        }

        if (existing.User.Status != UserStatus.ACTIVE)
        {
            throw new UnauthorizedAppException("Tài khoản đang bị khóa hoặc không hoạt động.");
        }

        // Rotation: revoke token cũ, phát hành cặp token mới.
        var response = await IssueTokenPairAsync(existing.User, ipAddress, ct, existing);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        if (existing is null || existing.IsRevoked)
        {
            return; // Idempotent: logout token không tồn tại/đã revoke vẫn coi là thành công.
        }

        existing.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundAppException("Không tìm thấy người dùng.");

        Guid? candidateId = null;
        if (user.Role == UserRole.CANDIDATE)
        {
            candidateId = await _db.Candidates
                .Where(c => c.UserId == user.Id)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        return ToProfileDto(user, candidateId);
    }

    // --- Helpers ------------------------------------------------------------

    private async Task<AuthResponse> IssueTokenPairAsync(
        User user, string? ipAddress, CancellationToken ct, RefreshToken? tokenToRevoke = null)
    {
        var now = DateTime.UtcNow;
        var access = _jwtTokenService.GenerateAccessToken(user);
        var rawRefreshToken = _jwtTokenService.GenerateRefreshTokenValue();
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var refreshExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = now,
            CreatedByIp = ipAddress,
        };
        _db.RefreshTokens.Add(newRefreshToken);

        if (tokenToRevoke is not null)
        {
            tokenToRevoke.RevokedAt = now;
            tokenToRevoke.ReplacedByTokenHash = refreshTokenHash;
        }

        await _db.SaveChangesAsync(ct);

        Guid? candidateId = null;
        if (user.Role == UserRole.CANDIDATE)
        {
            candidateId = await _db.Candidates
                .Where(c => c.UserId == user.Id)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        return new AuthResponse
        {
            AccessToken = access.Token,
            AccessTokenExpiresAtUtc = access.ExpiresAtUtc,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAt,
            User = ToProfileDto(user, candidateId),
        };
    }

    private async Task RevokeAllActiveTokensForUserAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        if (activeTokens.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    private static UserProfileDto ToProfileDto(User user, Guid? candidateId) => new()
    {
        UserId = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        CandidateId = candidateId,
    };

    /// <summary>
    /// Sinh mã thí sinh nghiệp vụ. Đơn giản hoá cho Phase 1: dùng 8 ký tự đầu của 1 Guid mới —
    /// xác suất trùng gần như bằng 0 nên chưa cần vòng lặp retry khi va chạm UNIQUE constraint.
    /// Có thể thay bằng chuỗi tuần tự có prefix năm nếu nghiệp vụ yêu cầu sau này.
    /// </summary>
    private static string GenerateCandidateCode()
        => $"CAND-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
