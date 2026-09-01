using Toeic.Domain.Entities;
using Toeic.Domain.Rubrics;

namespace Toeic.UnitTests.RubricManagement;

public class RubricSetValidationTests
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

    [Fact]
    public void Validate_EmptyRubricList_ReturnsSingleError()
    {
        var errors = RubricSetValidation.Validate(new List<Rubric>());

        Assert.Single(errors);
        Assert.Contains("at least one Rubric row", errors[0]);
    }

    [Fact]
    public void Validate_WeightsDoNotSumToOne_ReturnsError()
    {
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 0, 2, 0.5m),
            R("Grammar", 2, 2, 4, 0.5m),
            R("Vocabulary", 1, 0, 4, 0.3m) // total = 0.8, not 1.0
        };

        var errors = RubricSetValidation.Validate(rubrics);

        Assert.Contains(errors, e => e.Contains("Sum of criterion weights"));
    }

    [Fact]
    public void Validate_InconsistentWeightAcrossLevelsOfSameCriterion_ReturnsError()
    {
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 0, 2, 0.5m),
            R("Grammar", 2, 2, 4, 0.6m), // inconsistent with level 1
            R("Vocabulary", 1, 0, 4, 0.5m)
        };

        var errors = RubricSetValidation.Validate(rubrics);

        Assert.Contains(errors, e => e.Contains("inconsistent Weight"));
    }

    [Fact]
    public void Validate_OverlappingScoreRanges_ReturnsError()
    {
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 0, 3, 0.5m),
            R("Grammar", 2, 2, 4, 0.5m), // overlaps level 1 (2 < 3)
            R("Vocabulary", 1, 0, 4, 0.5m)
        };

        var errors = RubricSetValidation.Validate(rubrics);

        Assert.Contains(errors, e => e.Contains("overlaps"));
    }

    [Fact]
    public void Validate_DuplicateLevelNumbers_ReturnsError()
    {
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 0, 2, 0.5m),
            R("Grammar", 1, 2, 4, 0.5m), // duplicate level 1
            R("Vocabulary", 1, 0, 4, 0.5m)
        };

        var errors = RubricSetValidation.Validate(rubrics);

        Assert.Contains(errors, e => e.Contains("duplicate Level"));
    }

    [Fact]
    public void Validate_MinGreaterThanMax_ReturnsError()
    {
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 5, 2, 1.0m) // Min > Max
        };

        var errors = RubricSetValidation.Validate(rubrics);

        Assert.Contains(errors, e => e.Contains("must not be greater than MaxScore"));
    }

    [Fact]
    public void Validate_ValidRubricSet_ReturnsNoErrors()
    {
        var rubrics = new List<Rubric>
        {
            R("Grammar", 1, 0, 2, 0.5m),
            R("Grammar", 2, 2, 4, 0.5m),
            R("Vocabulary", 1, 0, 2, 0.25m),
            R("Vocabulary", 2, 2, 4, 0.25m),
            R("ImageRelevance", 1, 0, 4, 0.25m)
        };

        var errors = RubricSetValidation.Validate(rubrics);

        Assert.Empty(errors);
    }
}
