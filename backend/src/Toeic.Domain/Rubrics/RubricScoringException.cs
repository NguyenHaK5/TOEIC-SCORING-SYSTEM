namespace Toeic.Domain.Rubrics;

/// <summary>
/// Thrown by <see cref="WritingScoringEngine"/> when the rubric itself is
/// invalid, or the supplied criterion scores do not match what the rubric
/// requires (missing criterion, unexpected criterion, or a raw score outside
/// the rubric's valid range for that criterion).
///
/// This is a plain domain exception (not an AppException) because
/// Toeic.Domain must not depend on Toeic.Application. The Infrastructure/
/// Application layer that calls the engine (Phase 3 Part 4) is expected to
/// catch this and translate it into whatever AppException shape the API
/// uses (e.g. wrap `Errors` into a ValidationAppException), the same way
/// Phase 2's ObjectiveScoringEngine keeps domain and application exceptions
/// separate.
/// </summary>
public sealed class RubricScoringException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public RubricScoringException(IReadOnlyList<string> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildMessage(IReadOnlyList<string> errors)
        => "Writing scoring failed: " + string.Join(" | ", errors);
}
