using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.WritingAssessment;

namespace Toeic.Infrastructure.WritingAssessment;

public class GeminiLlmProvider : ILlmProvider
{
    private const int MaxHttpAttempts = 3;

    private readonly HttpClient _httpClient;
    private readonly AiAssessmentOptions _options;
    private readonly ILogger<GeminiLlmProvider> _logger;

    public GeminiLlmProvider(HttpClient httpClient, IOptions<AiAssessmentOptions> options, ILogger<GeminiLlmProvider> logger)
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
                "AiAssessment:ApiKey is not configured. Set it via user-secrets.");
        }

        // Cấu trúc payload dành riêng cho Google Gemini
        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = request.SystemPrompt } } },
            contents = new[]
            {
                new { parts = new[] { new { text = request.UserPrompt } } }
            },
            generationConfig = new
            {
                temperature = request.Temperature,
                maxOutputTokens = request.MaxTokens,
                responseMimeType = "application/json" // Ép Gemini trả về định dạng JSON
            }
        };

        Exception? lastError = null;
        // Endpoint của Gemini yêu cầu chèn ModelName và API Key trực tiếp vào URL
        var requestUri = $"/v1beta/models/{request.ModelName}:generateContent?key={_options.ApiKey}";

        for (var attempt = 1; attempt <= MaxHttpAttempts; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
                httpRequest.Content = JsonContent.Create(payload);

                using var response = await _httpClient.SendAsync(httpRequest, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);

                    if (IsTransient(response.StatusCode) && attempt < MaxHttpAttempts)
                    {
                        _logger.LogWarning(
                            "Gemini API returned {StatusCode} on attempt {Attempt}/{MaxAttempts} " +
                            "(requestId={RequestId}), retrying: {Body}",
                            response.StatusCode, attempt, MaxHttpAttempts, request.RequestId, body);
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
                        continue;
                    }

                    throw new AiProviderException(
                        $"Gemini API returned {(int)response.StatusCode} {response.StatusCode}: {body}");
                }

                var json = await response.Content.ReadFromJsonAsync<GeminiMessageResponse>(cancellationToken: ct)
                    ?? throw new AiProviderException("Gemini API returned an empty response body.");

                var text = json.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                    ?? throw new AiProviderException("Gemini API response did not contain a text block.");

                return new LlmCompletionResult
                {
                    RawText = text,
                    ModelName = request.ModelName,
                    InputTokens = json.UsageMetadata?.PromptTokenCount,
                    OutputTokens = json.UsageMetadata?.CandidatesTokenCount,
                    RequestId = request.RequestId
                };
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                lastError = new TimeoutException(
                    $"Gemini API call timed out after {_options.TimeoutSeconds}s (attempt {attempt}/{MaxHttpAttempts}).");
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
                    throw new AiProviderException($"Gemini API call failed: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
            }
        }

        throw new AiProviderException($"Gemini API call failed after {MaxHttpAttempts} attempts: {lastError?.Message}");
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}