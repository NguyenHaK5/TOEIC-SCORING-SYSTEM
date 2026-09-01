using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Toeic.Application.Common.Exceptions;
using Toeic.Application.WritingAssessment;
using Toeic.Application.WritingAssessment.Dtos;
using Toeic.Domain.Enums;
using Toeic.Domain.Rubrics;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.WritingAssessment;

/// <summary>
/// Orchestrates the pipeline described in section 13 of the original prompt:
///
///   Controller -> Application Service -> AI Assessment Service -> Prompt
///   Builder -> LLM Provider -> Structured AI Response -> Validation ->
///   Rubric -> Scoring Engine
///
/// Reads AppDbContext directly to load the ACTIVE RubricSet (same pattern
/// as every other Infrastructure service since Phase 1 — no generic
/// Repository&lt;T&gt;). Does NOT write anything to the database — this Part
/// stops at returning a WritingAssessmentResult; persisting it into
/// WritingAssessment/AssessmentCriterionScores is Phase 3 Part 4.
/// </summary>
public class AiAssessmentService : IAiAssessmentService
{
    private readonly AppDbContext _db;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILlmProvider _llmProvider;
    private readonly AiAssessmentOptions _options;

    public AiAssessmentService(
        AppDbContext db,
        IPromptBuilder promptBuilder,
        ILlmProvider llmProvider,
        IOptions<AiAssessmentOptions> options)
    {
        _db = db;
        _promptBuilder = promptBuilder;
        _llmProvider = llmProvider;
        _options = options.Value;
    }

    public async Task<WritingAssessmentResult> AssessAsync(WritingAssessmentRequest request, CancellationToken ct)
    {
        var rubricSet = await _db.RubricSets
            .Include(r => r.Rubrics)
            .Where(r => r.QuestionType == request.QuestionType && r.Status == RubricSetStatus.ACTIVE)
            .FirstOrDefaultAsync(ct)
            ?? throw new ConflictAppException(
                $"No ACTIVE RubricSet configured for QuestionType '{request.QuestionType}'. An Admin must " +
                "activate one first (Phase 3 Part 1: POST /rubric-sets/{id}/activate).");

        var promptRequest = _promptBuilder.Build(request.QuestionType, request, rubricSet.Rubrics.ToList());

        Exception? lastError = null;
        var maxAttempts = Math.Max(1, _options.MaxAttempts);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var requestId = Guid.NewGuid().ToString("N");

            var userPrompt = attempt == 1
                ? promptRequest.UserPrompt
                : promptRequest.UserPrompt + BuildCorrectiveSuffix(lastError);

            var completion = await _llmProvider.CompleteAsync(new LlmCompletionRequest
            {
                SystemPrompt = promptRequest.SystemPrompt,
                UserPrompt = userPrompt,
                ModelName = _options.ModelName,
                Temperature = _options.Temperature,
                MaxTokens = _options.MaxTokens,
                RequestId = requestId
            }, ct);
            // Note: AiProviderException from the LLM provider (network/HTTP failure) is NOT
            // retried here — AnthropicLlmProvider already retries transient HTTP failures
            // internally. If it still throws, that's a transport problem, not something a
            // different prompt will fix, so we let it propagate immediately.

            RawAiAssessmentOutput parsed;
            try
            {
                parsed = AiOutputParser.Parse(completion.RawText);
            }
            catch (FormatException ex)
            {
                lastError = ex;
                if (attempt == maxAttempts)
                {
                    throw new AiOutputValidationException(
                        $"AI returned malformed output after {attempt} attempt(s): {ex.Message}");
                }
                continue;
            }

            WritingScoringResult scoring;
            try
            {
                scoring = WritingScoringEngine.CalculateOverallScore(
                    rubricSet.Rubrics.ToList(), parsed.CriterionScores, _options.TargetMaxScore);
            }
            catch (RubricScoringException ex)
            {
                lastError = ex;
                if (attempt == maxAttempts)
                {
                    throw new AiOutputValidationException(
                        $"AI output failed rubric validation after {attempt} attempt(s): " +
                        string.Join("; ", ex.Errors));
                }
                continue;
            }

            return new WritingAssessmentResult
            {
                Scoring = scoring,
                RawOutputJson = completion.RawText,
                Feedback = parsed.Feedback,
                Strengths = parsed.Strengths ?? new List<string>(),
                Weaknesses = parsed.Weaknesses ?? new List<string>(),
                Corrections = parsed.Corrections ?? new List<string>(),
                RubricSetId = rubricSet.Id,
                ModelName = completion.ModelName,
                PromptVersion = promptRequest.PromptVersion,
                RequestId = requestId
            };
        }

        // Unreachable — the loop above always either returns or throws on the final attempt.
        throw new AiOutputValidationException(
            $"AI assessment failed after {maxAttempts} attempt(s): {lastError?.Message}");
    }

    private static string BuildCorrectiveSuffix(Exception? lastError) =>
        "\n\nIMPORTANT: Your previous response was rejected because: " +
        (lastError?.Message ?? "it did not match the required format") +
        ". Re-read the required JSON structure above carefully and respond again with ONLY a single " +
        "valid JSON object that exactly matches it — no markdown fences, no extra text.";
}
