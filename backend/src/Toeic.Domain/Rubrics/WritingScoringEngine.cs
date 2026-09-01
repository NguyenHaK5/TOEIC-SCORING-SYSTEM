using Toeic.Domain.Entities;

namespace Toeic.Domain.Rubrics;

/// <summary>
/// Domain logic that turns a set of per-criterion scores (produced by AI
/// analysis OR a Human Rater — this engine does not care which) into a
/// single normalized OverallScore, using an ACTIVE RubricSet's criteria and
/// weights.
///
/// This is intentionally the ONLY place that computes a Writing final
/// score. Per the project's non-negotiable principle #3/#5 ("Không để LLM
/// quyết định Final Score một cách không kiểm soát" / "Không trộn AI
/// Analysis với Scoring Engine"), AI Assessment (Part 3) may only produce
/// raw criterion scores + reasoning — never a final score directly. Human
/// Rater (Phase 4) will eventually also feed its per-criterion scores
/// through this same engine so AI and Human are scored with an identical,
/// auditable formula.
///
/// Pure, static, no EF Core / HTTP / IO dependency — fully unit-testable,
/// same pattern as Phase 2's ObjectiveScoringEngine.
///
/// Algorithm:
///   1. Re-validate the rubric itself via RubricSetValidation (defensive —
///      the caller is expected to only ever pass an ACTIVE rubric, which
///      was already validated at activation time in Phase 3 Part 1, but a
///      stale in-memory copy or a bug elsewhere should not silently produce
///      a wrong score).
///   2. For each Criterion in the rubric, find the matching raw score in
///      the input. Every criterion in the rubric must have exactly one raw
///      score — no more, no less.
///   3. Normalize each criterion's raw score to [0, 1] using that
///      criterion's full range (min of its lowest level to max of its
///      highest level).
///   4. Multiply each normalized score by the criterion's Weight (weights
///      already sum to 1.0 per RubricSetValidation) and sum -> a [0, 1]
///      normalized overall.
///   5. Scale the normalized overall by <paramref name="targetMaxScore"/>
///      to get the final OverallScore.
///
/// Note on TOEIC scaled score (5-495): deferred, same as Phase 2's
/// objective scoring (see phase-tracker.md "Open decision"). This engine
/// returns a score on whatever scale the caller asks for via
/// targetMaxScore (e.g. 100 for a percentage-like score, or
/// Question.Points if the caller wants to keep parity with how Listening/
/// Reading questions carry a Points value) — TOEIC scaled conversion, if
/// ever implemented, would be a separate step applied AFTER this engine,
/// not inside it.
/// </summary>
public static class WritingScoringEngine
{
    public const decimal DefaultTargetMaxScore = 100m;

    public static WritingScoringResult CalculateOverallScore(
        IReadOnlyCollection<Rubric> rubrics,
        IReadOnlyDictionary<string, decimal> criterionScores,
        decimal targetMaxScore = DefaultTargetMaxScore)
    {
        if (targetMaxScore <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetMaxScore), "targetMaxScore must be greater than 0.");
        }

        if (criterionScores is null)
        {
            throw new ArgumentNullException(nameof(criterionScores));
        }

        // Step 1 — the rubric must be internally consistent before we can
        // trust its weights/ranges for scoring.
        var rubricErrors = RubricSetValidation.Validate(rubrics);
        if (rubricErrors.Count > 0)
        {
            throw new RubricScoringException(rubricErrors);
        }

        var byCriterion = rubrics
            .GroupBy(r => r.Criterion.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var inputCriteria = criterionScores.Keys
            .Select(k => k.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rubricCriteria = byCriterion.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Step 2 — exact match required between rubric criteria and input criteria.
        var errors = new List<string>();

        var missing = rubricCriteria.Except(inputCriteria, StringComparer.OrdinalIgnoreCase).ToList();
        var unexpected = inputCriteria.Except(rubricCriteria, StringComparer.OrdinalIgnoreCase).ToList();

        if (missing.Count > 0)
        {
            errors.Add($"Missing criterion score(s) for: {string.Join(", ", missing)}.");
        }

        if (unexpected.Count > 0)
        {
            errors.Add($"Unexpected criterion score(s) not defined in the rubric: {string.Join(", ", unexpected)}.");
        }

        if (errors.Count > 0)
        {
            throw new RubricScoringException(errors);
        }

        var breakdown = new List<CriterionScoreBreakdown>();
        decimal normalizedOverall = 0m;

        foreach (var (criterion, levels) in byCriterion)
        {
            var weight = levels[0].Weight; // consistent across levels — guaranteed by RubricSetValidation
            var criterionMin = levels.Min(l => l.MinScore);
            var criterionMax = levels.Max(l => l.MaxScore);

            var rawScore = criterionScores
                .First(kv => string.Equals(kv.Key.Trim(), criterion, StringComparison.OrdinalIgnoreCase))
                .Value;

            // Step 3 — reject (not silently clamp) out-of-range scores. A criterion
            // score outside the rubric's own defined range means either the AI output
            // validation (Part 3) let something invalid through, or a caller bug —
            // both should fail loudly rather than silently distort the OverallScore.
            if (rawScore < criterionMin || rawScore > criterionMax)
            {
                errors.Add(
                    $"Criterion '{criterion}': score {rawScore} is outside the rubric's valid range " +
                    $"[{criterionMin}, {criterionMax}].");
                continue;
            }

            var range = criterionMax - criterionMin;
            var normalizedScore = range == 0m ? 1m : (rawScore - criterionMin) / range;
            var contribution = normalizedScore * weight;
            normalizedOverall += contribution;

            breakdown.Add(new CriterionScoreBreakdown(
                Criterion: criterion,
                RawScore: rawScore,
                MinScore: criterionMin,
                MaxScore: criterionMax,
                Weight: weight,
                NormalizedScore: Math.Round(normalizedScore, 4, MidpointRounding.AwayFromZero),
                WeightedContribution: Math.Round(contribution, 4, MidpointRounding.AwayFromZero)));
        }

        if (errors.Count > 0)
        {
            throw new RubricScoringException(errors);
        }

        var overallScore = Math.Round(normalizedOverall * targetMaxScore, 2, MidpointRounding.AwayFromZero);

        return new WritingScoringResult(
            OverallScore: overallScore,
            TargetMaxScore: targetMaxScore,
            CriterionBreakdown: breakdown.OrderBy(b => b.Criterion, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
