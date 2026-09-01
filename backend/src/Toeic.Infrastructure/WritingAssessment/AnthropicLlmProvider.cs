using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.WritingAssessment;

namespace Toeic.Infrastructure.WritingAssessment;

/// <summary>
/// Reference ILlmProvider implementation for the Anthropic Messages API
/// (https://api.anthropic.com/v1/messages). This is ONE example adapter —
/// swapping to a different provider means implementing ILlmProvider again
/// and changing the DI registration; nothing in PromptBuilder or
/// AiAssessmentService needs to change (PHASE3-ISSUE-05, see integration
/// guide).
///
/// Handles transport-level concerns only (HTTP call, timeout, retry on
/// transient 429/5xx). Semantic validation of the returned text (is it
/// valid JSON matching the rubric?) happens one layer up, in
/// AiAssessmentService + AiOutputParser + WritingScoringEngine.
/// </summary>
public class AnthropicLlmProvider : ILlmProvider
{
    private const int MaxHttpAttempts = 3;
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient _httpClient;
    private readonly AiAssessmentOptions _options;
    private readonly ILogger<AnthropicLlmProvider> _logger;

    public AnthropicLlmProvider(HttpClient httpClient, IOptions<AiAssessmentOptions> options, ILogger<AnthropicLlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new AiProviderException(
                "AiAssessment:ApiKey is not configured. Set it via user-secrets (never hard-code it) — " +
                "see PHASE3_PART3_INTEGRATION_GUIDE.md.");
        }

        var payload = new
        {
            model = request.ModelName,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            system = request.SystemPrompt,
            messages = new[] { new { role = "user", content = request.UserPrompt } }
        };

        Exception? lastError = null;

        for (var attempt = 1; attempt <= MaxHttpAttempts; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
                httpRequest.Headers.Add("x-api-key", _options.ApiKey);
                httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
                httpRequest.Content = JsonContent.Create(payload);

                using var response = await _httpClient.SendAsync(httpRequest, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);

                    if (IsTransient(response.StatusCode) && attempt < MaxHttpAttempts)
                    {
                        _logger.LogWarning(
                            "Anthropic API returned {StatusCode} on attempt {Attempt}/{MaxAttempts} " +
                            "(requestId={RequestId}), retrying: {Body}",
                            response.StatusCode, attempt, MaxHttpAttempts, request.RequestId, body);
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
                        continue;
                    }

                    throw new AiProviderException(
                        $"Anthropic API returned {(int)response.StatusCode} {response.StatusCode}: {body}");
                }

                var json = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(cancellationToken: ct)
                    ?? throw new AiProviderException("Anthropic API returned an empty response body.");

                var text = json.Content?.FirstOrDefault(c => c.Type == "text")?.Text
                    ?? throw new AiProviderException("Anthropic API response did not contain a text content block.");

                return new LlmCompletionResult
                {
                    RawText = text,
                    ModelName = json.Model ?? request.ModelName,
                    InputTokens = json.Usage?.InputTokens,
                    OutputTokens = json.Usage?.OutputTokens,
                    RequestId = request.RequestId
                };
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                lastError = new TimeoutException(
                    $"Anthropic API call timed out after {_options.TimeoutSeconds}s (attempt {attempt}/{MaxHttpAttempts}).");
                if (attempt == MaxHttpAttempts)
                {
                    throw new AiProviderException(lastError.Message);
                }
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
                if (attempt == MaxHttpAttempts)
                {
                    throw new AiProviderException($"Anthropic API call failed: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
            }
        }

        // Unreachable in practice (every branch above either returns or throws on the last
        // attempt) — kept only to satisfy the compiler's flow analysis.
        throw new AiProviderException(
            $"Anthropic API call failed after {MaxHttpAttempts} attempts: {lastError?.Message}");
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
