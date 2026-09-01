using System.Threading.Channels;
using Toeic.Application.WritingAssessment;

namespace Toeic.Infrastructure.WritingAssessment;

/// <summary>
/// Backed by System.Threading.Channels (built into .NET, no extra NuGet
/// package) — a bounded, thread-safe, async producer/consumer queue that
/// lives in process memory. Registered as a Singleton (see DI notes in the
/// integration guide) so the same instance is shared between request
/// threads (producers) and the background worker (consumer).
///
/// Capacity is bounded (default 200) with FullMode=Wait: if the queue is
/// ever completely full (200 exam sessions submitted Writing faster than
/// the worker can process them), EnqueueAsync will asynchronously wait for
/// room rather than throw or drop jobs — acceptable back-pressure for this
/// project's scale; revisit if load testing shows otherwise.
/// </summary>
public class InMemoryWritingAssessmentQueue : IWritingAssessmentQueue
{
    private readonly Channel<WritingAssessmentJob> _channel;

    public InMemoryWritingAssessmentQueue()
    {
        var options = new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<WritingAssessmentJob>(options);
    }

    public ValueTask EnqueueAsync(WritingAssessmentJob job, CancellationToken ct)
        => _channel.Writer.WriteAsync(job, ct);

    public ValueTask<WritingAssessmentJob> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAsync(ct);
}
