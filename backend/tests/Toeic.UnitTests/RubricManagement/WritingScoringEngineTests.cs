using Toeic.Domain.Entities;
using Toeic.Domain.Rubrics;

namespace Toeic.UnitTests.RubricManagement;

public class WritingScoringEngineTests
{
    private static Rubric R(string criterion, int level, decimal min, decimal max, decimal weight) => new()
    {
        Id = Guid.NewGuid(),
        Criterion = criterion,
        Level = level,
        MinScore = min,
        MaxScore = max,
        Weight = weight
    };

    // Two criteria, equal weight (0.5 each), each spanning 0-4 across 2 levels.
    private static List<Rubric> TwoCriteriaRubric() => new()
    {
        R("Grammar", 1, 0, 2, 0.5m),
        R("Grammar", 2, 2, 4, 0.5m),
        R("Vocabulary", 1, 0, 2, 0.5m),
        R("Vocabulary", 2, 2, 4, 0.5m),
    };

    [Fact]
    public void CalculateOverallScore_BothCriteriaAtMax_ReturnsFullTargetScore()
    {
        var rubrics = TwoCriteriaRubric();
        var scores = new Dictionary<string, decimal>
        {
            ["Grammar"] = 4m,
            ["Vocabulary"] = 4m
        };

        var result = WritingScoringEngine.CalculateOverallScore(rubrics, scores, targetMaxScore: 100m);

        Assert.Equal(100m, result.OverallScore);
        Assert.Equal(2, result.CriterionBreakdown.Count);
        Assert.All(result.CriterionBreakdown, b => Assert.Equal(1m, b.NormalizedScore));
    }

    [Fact]
    public void CalculateOverallScore_BothCriteriaAtMin_ReturnsZero()
    {
        var rubrics = TwoCriteriaRubric();
        var scores = new Dictionary<string, decimal>
        {
            ["Grammar"] = 0m,
            ["Vocabulary"] = 0m
        };

        var result = WritingScoringEngine.CalculateOverallScore(rubrics, scores, targetMaxScore: 100m);

        Assert.Equal(0m, result.OverallScore);
    }

    [Fact]
    public void CalculateOverallScore_UnequalWeights_ComputesWeightedAverage()
    {
        // Grammar weight 0.7, Vocabulary weight 0.3, range 0-4 each.
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 0, 2, 0.7m),
            R("Grammar", 2, 2, 4, 0.7m),
            R("Vocabulary", 1, 0, 2, 0.3m),
            R("Vocabulary", 2, 2, 4, 0.3m),
        };

        // Grammar at max (normalized 1.0), Vocabulary at min (normalized 0.0)
        // Expected normalized overall = 1.0*0.7 + 0.0*0.3 = 0.7 -> 70 on a 100 scale.
        var scores = new Dictionary<string, decimal>
        {
            ["Grammar"] = 4m,
            ["Vocabulary"] = 0m
        };

        var result = WritingScoringEngine.CalculateOverallScore(rubrics, scores, targetMaxScore: 100m);

        Assert.Equal(70m, result.OverallScore);
    }

    [Fact]
    public void CalculateOverallScore_ScalesToArbitraryTargetMaxScore()
    {
        var rubrics = TwoCriteriaRubric();
        var scores = new Dictionary<string, decimal>
        {
            ["Grammar"] = 2m, // midpoint -> normalized 0.5
            ["Vocabulary"] = 2m // midpoint -> normalized 0.5
        };

        var result = WritingScoringEngine.CalculateOverallScore(rubrics, scores, targetMaxScore: 10m);

        Assert.Equal(5m, result.OverallScore);
        Assert.Equal(10m, result.TargetMaxScore);
    }

    [Fact]
    public void CalculateOverallScore_CriterionCaseInsensitiveMatch_Works()
    {
        var rubrics = TwoCriteriaRubric();
        var scores = new Dictionary<string, decimal>
        {
            ["grammar"] = 4m,
            ["VOCABULARY"] = 4m
        };

        var result = WritingScoringEngine.CalculateOverallScore(rubrics, scores, targetMaxScore: 100m);

        Assert.Equal(100m, result.OverallScore);
    }

    [Fact]
    public void CalculateOverallScore_MissingCriterionScore_ThrowsWithClearMessage()
    {
        var rubrics = TwoCriteriaRubric();
        var scores = new Dictionary<string, decimal> { ["Grammar"] = 4m }; // Vocabulary missing

        var ex = Assert.Throws<RubricScoringException>(
            () => WritingScoringEngine.CalculateOverallScore(rubrics, scores));

        Assert.Contains(ex.Errors, e => e.Contains("Missing criterion score") && e.Contains("Vocabulary"));
    }

    [Fact]
    public void CalculateOverallScore_UnexpectedCriterionScore_Throws()
    {
        var rubrics = TwoCriteriaRubric();
        var scores = new Dictionary<string, decimal>
        {
            ["Grammar"] = 4m,
            ["Vocabulary"] = 4m,
            ["ImageRelevance"] = 4m // not in this rubric
        };

        var ex = Assert.Throws<RubricScoringException>(
            () => WritingScoringEngine.CalculateOverallScore(rubrics, scores));

        Assert.Contains(ex.Errors, e => e.Contains("Unexpected criterion score") && e.Contains("ImageRelevance"));
    }

    [Fact]
    public void CalculateOverallScore_RawScoreOutOfRubricRange_Throws()
    {
        var rubrics = TwoCriteriaRubric();
        var scores = new Dictionary<string, decimal>
        {
            ["Grammar"] = 4m,
            ["Vocabulary"] = 10m // rubric max for Vocabulary is 4
        };

        var ex = Assert.Throws<RubricScoringException>(
            () => WritingScoringEngine.CalculateOverallScore(rubrics, scores));

        Assert.Contains(ex.Errors, e => e.Contains("outside the rubric's valid range"));
    }

    [Fact]
    public void CalculateOverallScore_InvalidRubric_ThrowsBeforeEvenReadingScores()
    {
        // Weights don't sum to 1.0 -> RubricSetValidation should catch this.
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 0, 4, 0.3m),
            R("Vocabulary", 1, 0, 4, 0.3m),
        };
        var scores = new Dictionary<string, decimal>
        {
            ["Grammar"] = 4m,
            ["Vocabulary"] = 4m
        };

        var ex = Assert.Throws<RubricScoringException>(
            () => WritingScoringEngine.CalculateOverallScore(rubrics, scores));

        Assert.Contains(ex.Errors, e => e.Contains("Sum of criterion weights"));
    }

    [Fact]
    public void CalculateOverallScore_ZeroOrNegativeTargetMaxScore_ThrowsArgumentOutOfRange()
    {
        var rubrics = TwoCriteriaRubric();
        var scores = new Dictionary<string, decimal> { ["Grammar"] = 4m, ["Vocabulary"] = 4m };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => WritingScoringEngine.CalculateOverallScore(rubrics, scores, targetMaxScore: 0m));
    }

    [Fact]
    public void CalculateOverallScore_ThreeCriteriaIncludingImageRelevance_ForPictureDescriptionQuestions()
    {
        // Mirrors Q1-5 (PICTURE_SENTENCE) rubric shape from Phase 0 ISSUE-09/ISSUE-03:
        // Grammar + RequiredWords + ImageRelevance, weights summing to 1.0.
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 0, 4, 0.4m),
            R("RequiredWords", 1, 0, 4, 0.3m),
            R("ImageRelevance", 1, 0, 4, 0.3m),
        };
        var scores = new Dictionary<string, decimal>
        {
            ["Grammar"] = 4m,
            ["RequiredWords"] = 2m,
            ["ImageRelevance"] = 0m
        };

        // Expected = 1.0*0.4 + 0.5*0.3 + 0.0*0.3 = 0.4 + 0.15 + 0 = 0.55 -> 55 on 100 scale
        var result = WritingScoringEngine.CalculateOverallScore(rubrics, scores, targetMaxScore: 100m);

        Assert.Equal(55m, result.OverallScore);
        Assert.Equal(3, result.CriterionBreakdown.Count);
    }
}
