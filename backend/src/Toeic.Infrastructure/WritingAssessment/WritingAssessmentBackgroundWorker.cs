using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Toeic.Application.WritingAssessment;

namespace Toeic.Infrastructure.WritingAssessment;

/// <summary>
/// Single long-running background consumer (registered via
/// services.AddHostedService — runs for the lifetime of the API process).
/// Dequeues one WritingAssessmentJob at a time and delegates the actual
/// work to IWritingResultService.ProcessExamSessionAsync inside a fresh DI
/// scope (BackgroundService itself is a singleton, but AppDbContext and
/// friends are scoped — same pattern as any ASP.NET Core background
/// worker that needs scoped services).
///
/// Deliberately processes one job at a time (no parallelism) — keeps LLM
/// API usage predictable/rate-limit-friendly and avoids EF Core DbContext
/// concurrency issues. Acceptable for this project's scale; if throughput
/// ever becomes a bottleneck, run multiple worker instances or bump
/// SemaphoreSlim-bounded parallelism here without touching the queue
/// contract or WritingResultService.
/// </summary>
public class WritingAssessmentBackgroundWorker : BackgroundService
{
    private readonly IWritingAssessmentQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WritingAssessmentBackgroundWorker> _logger;

    public WritingAssessmentBackgroundWorker(
        IWritingAssessmentQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<WritingAssessmentBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            WritingAssessmentJob job;
            try
            {
                job = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // app is shutting down
            }

            using var scope = _scopeFactory.CreateScope();
            var writingResultService = scope.ServiceProvider.GetRequiredService<IWritingResultService>();

            try
            {
                _logger.LogInformation(
                    "Writing assessment job started for ExamSession {ExamSessionId}.", job.ExamSessionId);

                await writingResultService.ProcessExamSessionAsync(job.ExamSessionId, stoppingToken);

                _logger.LogInformation(
                    "Writing assessment job finished for ExamSession {ExamSessionId}.", job.ExamSessionId);
            }
            catch (Exception ex)
            {
                // A failed job must not crash the worker loop — log and move on. Any
                // question left unscored will be picked up again the next time someone
                // calls GET .../result (WritingResultService's self-healing re-enqueue,
                // see PHASE3-ISSUE-06).
                _logger.LogError(
                    ex,
                    "Writing assessment job failed for ExamSession {ExamSessionId}. Will retry on next " +
                    "GET .../result call.", job.ExamSessionId);
            }
        }
    }
}
