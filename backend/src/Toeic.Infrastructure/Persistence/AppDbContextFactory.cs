using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Toeic.Infrastructure.Persistence;

/// <summary>
/// Cho phép chạy `dotnet ef migrations add ...` trực tiếp từ Toeic.Infrastructure mà không cần
/// khởi động toàn bộ Toeic.Api. Connection string chỉ dùng cho design-time (tạo migration),
/// KHÔNG dùng khi chạy ứng dụng thật (Program.cs của Toeic.Api tự đọc appsettings/ENV).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TOEIC_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=toeic_scoring;Username=toeic;Password=toeic_dev_only";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
