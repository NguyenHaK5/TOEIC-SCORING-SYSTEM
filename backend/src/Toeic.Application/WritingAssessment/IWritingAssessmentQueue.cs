namespace Toeic.Application.WritingAssessment;

/// <summary>One job = "go score every not-yet-scored Writing answer in this ExamSession".</summary>
public record WritingAssessmentJob(Guid ExamSessionId);

/// <summary>
/// In-process background queue contract (PHASE3-ISSUE-06). Producers
/// (ExamSessionService.SubmitAsync, and the self-healing check in
/// WritingResultService.GetSectionResultAsync) call EnqueueAsync and return
/// immediately — they never wait for AI scoring to finish. A single
/// consumer (WritingAssessmentBackgroundWorker) calls DequeueAsync in a
/// loop.
///
/// NOTE: the shipped implementation (InMemoryWritingAssessmentQueue) is an
/// in-memory .NET Channel — not durable across an app restart. This is a
/// known, documented trade-off for this project's scope (see integration
/// guide); swapping to a durable queue (Azure Service Bus, RabbitMQ,
/// Hangfire...) later only means implementing this same interface again,
/// no changes needed in the producers or in WritingResultService.
/// </summary>
public interface IWritingAssessmentQueue
{
    ValueTask EnqueueAsync(WritingAssessmentJob job, CancellationToken ct);

    ValueTask<WritingAssessmentJob> DequeueAsync(CancellationToken ct);
}
