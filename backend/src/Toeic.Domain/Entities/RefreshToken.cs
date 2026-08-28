namespace Toeic.Domain.Entities;

/// <summary>
/// Lưu refresh token dưới dạng HASH (SHA-256), không bao giờ lưu raw token trong DB —
/// nếu DB bị lộ, token vẫn không dùng lại được. Hỗ trợ rotation: mỗi lần refresh,
/// token cũ bị revoke và ghi nhận ReplacedByTokenHash trỏ sang token mới, tạo thành chuỗi
/// audit — nếu phát hiện 1 token đã revoke bị dùng lại (reuse), có thể revoke toàn bộ chuỗi
/// (dấu hiệu token bị đánh cắp).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    // Navigation
    public User User { get; set; } = default!;
}
