namespace Toeic.Application.WritingAssessment;

public class LlmCompletionRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public int MaxTokens { get; set; }

    /// <summary>Correlation id for logging/audit — generated per assessment attempt.</summary>
    public string RequestId { get; set; } = string.Empty;
}

public class LlmCompletionResult
{
    public string RawText { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public string RequestId { get; set; } = string.Empty;
}

/// <summary>
/// Adapter boundary between the TOEIC backend and any LLM provider (section
/// 13 of the original prompt: "Không gọi LLM trực tiếp từ Controller" — no
/// caller above this interface knows or cares which provider is behind it).
/// This Part ships one reference implementation, <c>AnthropicLlmProvider</c>
/// — swapping providers means implementing this interface again and
/// changing one DI registration line, nothing else in the pipeline changes.
/// </summary>
public interface ILlmProvider
{
    Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken ct);
}
