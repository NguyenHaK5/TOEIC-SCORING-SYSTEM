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
///
/// PHASE8-ISSUE-18 (bổ sung): cùng cơ chế này mở rộng seed thêm 1 tài khoản RATER dành
/// riêng cho evaluation-service (Phase 5) — xem <see cref="SeedEvalServiceAsync"/>. Khác
/// SeedAdmin ở chỗ kiểm tra tồn tại theo ĐÚNG Username (không phải theo Role), vì có thể đã
/// có nhiều RATER khác do người thật tạo — "đã có RATER nào đó" không có nghĩa là "đã có
/// eval-service".
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

    /// <summary>
    /// Seed 1 tài khoản RATER dùng riêng cho evaluation-service (Phase 5 — service này tự
    /// login bằng tài khoản đây để gọi GET /rater/writing-answers/comparison, xem
    /// PHASE5-ISSUE-01). Seed CHỈ khi:
    ///   (1) cấu hình "SeedEvalService:Username/Email/Password" đầy đủ, VÀ
    ///   (2) chưa có user nào với ĐÚNG Username này (không check theo Role — có thể đã có
    ///       RATER khác do người thật tạo tay, không liên quan tới tài khoản này).
    /// Idempotent — an toàn khi chạy lại nhiều lần (mỗi lần backend container khởi động).
    /// </summary>
    public static async Task SeedEvalServiceAsync(
        AppDbContext db, IPasswordHasher passwordHasher, IConfiguration configuration, ILogger logger, CancellationToken ct = default)
    {
        var username = configuration["SeedEvalService:Username"];
        var email = configuration["SeedEvalService:Email"];
        var password = configuration["SeedEvalService:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("SeedEvalService chưa được cấu hình đầy đủ — bỏ qua seeding tài khoản eval-service.");
            return;
        }

        var trimmedUsername = username.Trim();
        var exists = await db.Users.AnyAsync(u => u.Username == trimmedUsername, ct);
        if (exists)
        {
            return; // Đã tồn tại (tự seed lần trước, hoặc Admin đã tạo tay) — không tạo trùng.
        }

        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = trimmedUsername,
            FullName = "Evaluation Service Account",
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.RATER,
            Status = UserStatus.ACTIVE,
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);
        logger.LogWarning(
            "Đã seed tài khoản RATER cho evaluation-service (Username={Username}).",
            trimmedUsername);
    }
}
