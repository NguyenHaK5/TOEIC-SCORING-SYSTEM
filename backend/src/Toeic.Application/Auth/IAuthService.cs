using Toeic.Application.Auth.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Application.Auth;

public interface IAuthService
{
    /// <summary>
    /// Tạo tài khoản mới. <paramref name="role"/> do CONTROLLER quyết định dựa trên endpoint/caller
    /// (không lấy từ request body của client) — xem RegisterRequest.
    /// Nếu role = CANDIDATE, tự động tạo hồ sơ Candidate liên kết 1:1.
    /// Ném ConflictAppException nếu Username/Email đã tồn tại.
    /// </summary>
    Task<UserProfileDto> RegisterAsync(RegisterRequest request, UserRole role, CancellationToken ct = default);

    /// <summary>
    /// Xác thực username/email + password, phát hành access token (JWT) + refresh token (opaque).
    /// Ném UnauthorizedAppException nếu sai thông tin đăng nhập hoặc tài khoản không ACTIVE.
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Đổi refresh token hợp lệ lấy cặp token mới (rotation) — refresh token cũ bị revoke ngay.
    /// Ném UnauthorizedAppException nếu token không tồn tại/đã revoke/hết hạn.
    /// </summary>
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>Revoke 1 refresh token cụ thể (logout khỏi thiết bị hiện tại).</summary>
    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);

    /// <summary>Lấy hồ sơ user hiện tại theo UserId lấy từ JWT claim "sub".</summary>
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
}
