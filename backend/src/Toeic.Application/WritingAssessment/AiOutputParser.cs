using System.Text.Json;
using Toeic.Application.WritingAssessment.Dtos;

namespace Toeic.Application.WritingAssessment;

/// <summary>
/// Parses and sanity-checks the raw text an LLM returned into
/// <see cref="RawAiAssessmentOutput"/>. Deliberately separated from
/// <c>AiAssessmentService</c> so it stays a pure function — fully
/// unit-testable without mocking HTTP or EF Core, same philosophy as
/// <c>RubricSetValidation</c> and <c>WritingScoringEngine</c>.
///
/// This is ONLY structural/sanity validation (is it JSON? does it have the
/// fields we need? is feedback non-empty?). Semantic validation — do the
/// criterion keys match the rubric, are the scores within range — is
/// intentionally NOT duplicated here: that is exactly what
/// <c>WritingScoringEngine.CalculateOverallScore</c> (Phase 3 Part 2)
/// already does, and <c>AiAssessmentService</c> calls it right after this
/// parser. Keeping the two checks separate (and not re-implementing rubric
/// matching here) avoids two copies of the same validation drifting apart.
/// </summary>
public static class AiOutputParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RawAiAssessmentOutput Parse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new FormatException("AI returned an empty response.");
        }

        var cleaned = StripMarkdownFence(rawText);

        RawAiAssessmentOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<RawAiAssessmentOutput>(cleaned, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new FormatException(
                $"AI output is not valid JSON: {ex.Message}. Raw output (first 500 chars): " +
                $"{Truncate(rawText, 500)}", ex);
        }

        if (output is null)
        {
            throw new FormatException("AI output parsed to null.");
        }

        if (output.CriterionScores is null || output.CriterionScores.Count == 0)
        {
            throw new FormatException("AI output is missing a non-empty 'criterionScores' object.");
        }

        if (string.IsNullOrWhiteSpace(output.Feedback))
        {
            throw new FormatException("AI output is missing a non-empty 'feedback' string.");
        }

        output.Strengths ??= new List<string>();
        output.Weaknesses ??= new List<string>();
        output.Corrections ??= new List<string>();

        return output;
    }

    private static string StripMarkdownFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline >= 0)
        {
            trimmed = trimmed[(firstNewline + 1)..];
        }

        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence >= 0)
        {
            trimmed = trimmed[..lastFence];
        }

        return trimmed.Trim();
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}
