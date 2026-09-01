using Toeic.Domain.Entities;

namespace Toeic.Domain.Rubrics;

/// <summary>
/// Pure domain logic — no EF Core dependency, fully unit-testable.
/// Validates that a RubricSet's Rubric rows form a coherent, scorable rubric
/// BEFORE it is allowed to transition DRAFT -> ACTIVE.
///
/// Rules enforced (see PHASE3-ISSUE-01 in the Part 1 integration guide):
///   1. At least one Rubric row must exist.
///   2. Every level of the same Criterion must share the same Weight
///      (Weight is a property of the criterion, not of a single level row —
///      the DB schema stores it per-row per Phase 0 design, so we must
///      validate consistency ourselves at the Application layer).
///   3. Each Criterion's levels must have non-overlapping MinScore/MaxScore
///      ranges (sorted by score) and unique Level numbers.
///   4. The distinct per-criterion weights must sum to 1.0 (100%), so the
///      Writing Scoring Engine (Phase 3 Part 2) can compute a normalized
///      OverallScore as a weighted average.
/// </summary>
public static class RubricSetValidation
{
    private const decimal WeightTolerance = 0.001m;

    public static IReadOnlyList<string> Validate(IReadOnlyCollection<Rubric> rubrics)
    {
        var errors = new List<string>();

        if (rubrics is null || rubrics.Count == 0)
        {
            errors.Add("RubricSet must have at least one Rubric row before it can be activated.");
            return errors;
        }

        var byCriterion = rubrics
            .GroupBy(r => r.Criterion.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        decimal totalWeight = 0m;

        foreach (var group in byCriterion)
        {
            var criterion = group.Key;
            var levels = group.OrderBy(r => r.Level).ToList();

            var distinctWeights = levels.Select(r => r.Weight).Distinct().ToList();
            if (distinctWeights.Count > 1)
            {
                errors.Add(
                    $"Criterion '{criterion}' has inconsistent Weight across its levels " +
                    $"({string.Join(", ", distinctWeights)}); all levels of the same criterion " +
                    "must share the same Weight.");
            }
            totalWeight += distinctWeights[0];

            var distinctLevelCount = levels.Select(r => r.Level).Distinct().Count();
            if (distinctLevelCount != levels.Count)
            {
                errors.Add($"Criterion '{criterion}' has duplicate Level values.");
            }

            foreach (var r in levels)
            {
                if (r.MinScore > r.MaxScore)
                {
                    errors.Add(
                        $"Criterion '{criterion}' level {r.Level}: MinScore ({r.MinScore}) " +
                        $"must not be greater than MaxScore ({r.MaxScore}).");
                }
            }

            var orderedByScore = levels.OrderBy(r => r.MinScore).ToList();
            for (var i = 1; i < orderedByScore.Count; i++)
            {
                var prev = orderedByScore[i - 1];
                var curr = orderedByScore[i];
                if (curr.MinScore < prev.MaxScore)
                {
                    errors.Add(
                        $"Criterion '{criterion}': score range of level {curr.Level} " +
                        $"({curr.MinScore}-{curr.MaxScore}) overlaps level {prev.Level} " +
                        $"({prev.MinScore}-{prev.MaxScore}).");
                }
            }
        }

        if (Math.Abs(totalWeight - 1.0m) > WeightTolerance)
        {
            errors.Add(
                $"Sum of criterion weights must equal 1.0 (100%); current total is {totalWeight}. " +
                $"Criteria found: {string.Join(", ", byCriterion.Select(g => g.Key))}.");
        }

        return errors;
    }
}
