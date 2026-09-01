namespace Toeic.Infrastructure.WritingAssessment;

/// <summary>
/// Bound from configuration section "AiAssessment". ApiKey MUST be set via
/// user-secrets (or environment/secret manager in production) — never
/// hard-coded (section 8/18 of the original prompt), same pattern as
/// Jwt:Key since Phase 1 Part 2.
/// </summary>
public class AiAssessmentOptions
{
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model identifier sent to the provider. Left blank by default on
    /// purpose — set the actual current model string for your provider in
    /// appsettings/user-secrets (do not assume any specific value is
    /// current at the time you read this).
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Defaults to https://api.anthropic.com if left blank.</summary>
    public string? BaseUrl { get; set; }

    public int MaxTokens { get; set; } = 1500;
    public decimal Temperature { get; set; } = 0.2m;
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Semantic retry count (malformed JSON / rubric-criteria mismatch),
    /// handled in AiAssessmentService — separate from the LLM provider's
    /// own internal transport-level retry (HTTP 429/5xx), which is fixed
    /// inside AnthropicLlmProvider.
    /// </summary>
    public int MaxAttempts { get; set; } = 2;

    /// <summary>
    /// Passed straight through to WritingScoringEngine.CalculateOverallScore.
    /// Kept configurable, not hard-coded — see Phase 3 Part 2 notes on why
    /// TOEIC scaled-score (5-495) conversion is deferred and applied, if
    /// ever, as a separate step after this engine.
    /// </summary>
    public decimal TargetMaxScore { get; set; } = 100m;
}
