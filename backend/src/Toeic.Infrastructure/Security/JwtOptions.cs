namespace Toeic.Infrastructure.Security;

/// <summary>
/// Bound từ appsettings section "Jwt". <see cref="Key"/> BẮT BUỘC đến từ user-secrets/biến môi trường
/// khi chạy thật (không hard-code secret — nguyên tắc #8 của prompt gốc). Program.cs validate độ dài
/// tối thiểu của Key khi khởi động.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string Key { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}
