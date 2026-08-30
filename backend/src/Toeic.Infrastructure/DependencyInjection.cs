using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Toeic.Application.Auth;
using Toeic.Application.Common.Interfaces;
using Toeic.Application.ExamManagement;
using Toeic.Application.ExamTaking;
using Toeic.Application.ObjectiveScoring;
using Toeic.Infrastructure.Auth;
using Toeic.Infrastructure.ExamManagement;
using Toeic.Infrastructure.ExamTaking;
using Toeic.Infrastructure.ObjectiveScoring;
using Toeic.Infrastructure.Persistence;
using Toeic.Infrastructure.Security;

namespace Toeic.Infrastructure;

/// <summary>
/// Gom toàn bộ đăng ký DI của Infrastructure layer vào 1 chỗ — Program.cs (Api) chỉ cần gọi
/// AddInfrastructure(configuration), tránh Program.cs phình to khi thêm module mới.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' in configuration.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<ITestService, TestService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IExamSessionService, ExamSessionService>();

        // ... trong AddInfrastructure(...) hoặc tương đương:
        services.AddScoped<IObjectiveScoringService, ObjectiveScoringService>();

        return services;
    }
}
