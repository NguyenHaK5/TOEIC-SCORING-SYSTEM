using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Toeic.Application.Common.Interfaces;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;

namespace Toeic.Infrastructure.Persistence;

/// <summary>
/// Giải quyết bài toán "gà-và-trứng" của RBAC: /auth/register-staff cần 1 ADMIN gọi, nhưng lúc
/// khởi tạo hệ thống chưa có ADMIN nào. Seeder này CHỈ tạo 1 tài khoản ADMIN đầu tiên nếu:
///   (1) cấu hình "SeedAdmin:Username/Email/Password" đầy đủ, VÀ
///   (2) chưa có user nào với Role=ADMIN trong DB.
/// Idempotent — chạy lại nhiều lần (mỗi lần start app) không tạo trùng. Không hard-code
/// password trong code — luôn đọc từ configuration (user-secrets/ENV), đúng nguyên tắc #8.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAdminAsync(
        AppDbContext db, IPasswordHasher passwordHasher, IConfiguration configuration, ILogger logger, CancellationToken ct = default)
    {
        var username = configuration["SeedAdmin:Username"];
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("SeedAdmin chưa được cấu hình đầy đủ — bỏ qua seeding tài khoản ADMIN.");
            return;
        }

        var adminExists = await db.Users.AnyAsync(u => u.Role == UserRole.ADMIN, ct);
        if (adminExists)
        {
            return; // Đã có ít nhất 1 ADMIN — không cần seed nữa.
        }

        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = username.Trim(),
            FullName = "System Administrator",
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.ADMIN,
            Status = UserStatus.ACTIVE,
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);
        logger.LogWarning(
            "Đã seed tài khoản ADMIN đầu tiên (Username={Username}). Hãy đổi mật khẩu ngay sau lần đăng nhập đầu tiên.",
            username);
    }
}
