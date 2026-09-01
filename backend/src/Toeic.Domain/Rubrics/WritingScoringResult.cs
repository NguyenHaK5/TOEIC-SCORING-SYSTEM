namespace Toeic.Domain.Rubrics;

/// <summary>
/// Per-criterion detail of how much a single criterion contributed to the
/// final OverallScore. Kept around (not thrown away after summing) because
/// Phase 3 Part 4 needs to persist one row per criterion into
/// AssessmentCriterionScores, and because "audit vì sao bài được điểm đó"
/// (Phase 0 principle #14) requires this breakdown to be inspectable, not
/// just the final number.
/// </summary>
public sealed record CriterionScoreBreakdown(
    string Criterion,
    decimal RawScore,
    decimal MinScore,
    decimal MaxScore,
    decimal Weight,
    decimal NormalizedScore,
    decimal WeightedContribution);

/// <summary>
/// Result of <see cref="WritingScoringEngine.CalculateOverallScore"/>.
/// </summary>
public sealed record WritingScoringResult(
    decimal OverallScore,
    decimal TargetMaxScore,
    IReadOnlyList<CriterionScoreBreakdown> CriterionBreakdown);
