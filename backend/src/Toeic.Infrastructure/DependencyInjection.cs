using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Toeic.Application.Auth;
using Toeic.Application.Common.Interfaces;
using Toeic.Application.Dashboard;
using Toeic.Application.ExamManagement;
using Toeic.Application.ExamTaking;
using Toeic.Application.HumanRating;
using Toeic.Application.ObjectiveScoring;
using Toeic.Application.RubricManagement;
using Toeic.Application.Scoring;
using Toeic.Application.WritingAssessment;
using Toeic.Infrastructure.Auth;
using Toeic.Infrastructure.Dashboard;
using Toeic.Infrastructure.ExamManagement;
using Toeic.Infrastructure.ExamTaking;
using Toeic.Infrastructure.HumanRating;
using Toeic.Infrastructure.ObjectiveScoring;
using Toeic.Infrastructure.Persistence;
using Toeic.Infrastructure.RubricManagement;
using Toeic.Infrastructure.Scoring;
using Toeic.Infrastructure.Security;
using Toeic.Infrastructure.WritingAssessment;

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
        services.AddScoped<IRubricService, RubricService>();

        services.Configure<AiAssessmentOptions>(configuration.GetSection("AiAssessment"));
        services.AddScoped<IPromptBuilder, PromptBuilder>();


        //// Phần đăng ký cho Anthropic:
        //services.AddHttpClient<ILlmProvider, AnthropicLlmProvider>((sp, client) =>
        //{
        //    var opts = sp.GetRequiredService<IOptions<AiAssessmentOptions>>().Value;
        //    client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(opts.BaseUrl)
        //        ? "https://api.anthropic.com"
        //        : opts.BaseUrl);
        //});

        // Phần đăng ký cho Gemini:
        services.AddHttpClient<ILlmProvider, GeminiLlmProvider>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<AiAssessmentOptions>>().Value;
            // URL gốc của Google Gemini
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(opts.BaseUrl)
                ? "https://generativelanguage.googleapis.com"
                : opts.BaseUrl);
        });

        services.AddScoped<IAiAssessmentService, AiAssessmentService>();

        // Đăng ký Queue dưới dạng Singleton là BẮT BUỘC để chia sẻ luồng dữ liệu
        services.AddSingleton<IWritingAssessmentQueue, InMemoryWritingAssessmentQueue>();
        services.AddHostedService<WritingAssessmentBackgroundWorker>();
        services.AddScoped<IWritingResultService, WritingResultService>();



        services.AddScoped<IHumanRatingService, HumanRatingService>();
        services.AddScoped<IWritingScoreResolver, WritingScoreResolver>();


        services.AddScoped<IWritingSessionScoreAggregator, WritingSessionScoreAggregator>();
        services.AddScoped<IHistoryService, HistoryService>();


        services.AddScoped<ICandidateAnalyticsService, CandidateAnalyticsService>();

        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        return services;
    }
}
