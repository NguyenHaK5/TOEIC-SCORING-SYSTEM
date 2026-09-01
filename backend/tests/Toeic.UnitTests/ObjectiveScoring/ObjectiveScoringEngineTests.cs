using Toeic.Domain.Entities;
using Toeic.Domain.Scoring;

namespace Toeic.UnitTests.ObjectiveScoring;

public class ObjectiveScoringEngineTests
{
    private static QuestionOption Option(Guid id, bool isCorrect) => new()
    {
        Id = id,
        QuestionId = Guid.NewGuid(),
        OptionLabel = isCorrect ? 'A' : 'B',
        OptionText = "text",
        IsCorrect = isCorrect,
    };

    [Fact]
    public void SelectedCorrectOption_ReturnsCorrect_WithConfiguredPoints()
    {
        var correctId = Guid.NewGuid();
        var options = new List<QuestionOption> { Option(correctId, true), Option(Guid.NewGuid(), false) };

        var result = ObjectiveScoringEngine.EvaluateMultipleChoice(correctId, options, questionPoints: 2m);

        Assert.True(result.IsCorrect);
        Assert.Equal(2m, result.Score);
    }

    [Fact]
    public void SelectedWrongOption_ReturnsIncorrect_ZeroScore()
    {
        var wrongId = Guid.NewGuid();
        var options = new List<QuestionOption> { Option(Guid.NewGuid(), true), Option(wrongId, false) };

        var result = ObjectiveScoringEngine.EvaluateMultipleChoice(wrongId, options, questionPoints: 5m);

        Assert.False(result.IsCorrect);
        Assert.Equal(0m, result.Score);
    }

    [Fact]
    public void NoSelection_ReturnsIncorrect_ZeroScore()
    {
        var options = new List<QuestionOption> { Option(Guid.NewGuid(), true), Option(Guid.NewGuid(), false) };

        var result = ObjectiveScoringEngine.EvaluateMultipleChoice(null, options, questionPoints: 1m);

        Assert.False(result.IsCorrect);
        Assert.Equal(0m, result.Score);
    }

    [Fact]
    public void NullPoints_FallsBackToDefaultOnePointWhenCorrect()
    {
        var correctId = Guid.NewGuid();
        var options = new List<QuestionOption> { Option(correctId, true) };

        var result = ObjectiveScoringEngine.EvaluateMultipleChoice(correctId, options, questionPoints: null);

        Assert.True(result.IsCorrect);
        Assert.Equal(ObjectiveScoringEngine.DefaultQuestionPoints, result.Score);
    }

    [Fact]
    public void SelectedOptionNotBelongingToQuestion_ReturnsIncorrect()
    {
        var options = new List<QuestionOption> { Option(Guid.NewGuid(), true) };
        var unrelatedOptionId = Guid.NewGuid();

        var result = ObjectiveScoringEngine.EvaluateMultipleChoice(unrelatedOptionId, options, questionPoints: 1m);

        Assert.False(result.IsCorrect);
        Assert.Equal(0m, result.Score);
    }
}
