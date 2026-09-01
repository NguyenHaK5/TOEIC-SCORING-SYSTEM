using Toeic.Application.WritingAssessment;
using Xunit;

namespace Toeic.UnitTests.WritingAssessment;

public class AiOutputParserTests
{
    private const string ValidJson = """
    {
      "criterionScores": { "Grammar": 3, "Vocabulary": 2 },
      "feedback": "Good use of vocabulary, minor grammar issues.",
      "strengths": ["Clear structure"],
      "weaknesses": ["Subject-verb agreement errors"],
      "corrections": ["he go -> he goes"]
    }
    """;

    [Fact]
    public void Parse_ValidJson_ReturnsExpectedValues()
    {
        var result = AiOutputParser.Parse(ValidJson);

        Assert.Equal(2, result.CriterionScores.Count);
        Assert.Equal(3m, result.CriterionScores["Grammar"]);
        Assert.Equal(2m, result.CriterionScores["Vocabulary"]);
        Assert.Equal("Good use of vocabulary, minor grammar issues.", result.Feedback);
        Assert.Single(result.Strengths!);
        Assert.Single(result.Weaknesses!);
        Assert.Single(result.Corrections!);
    }

    [Fact]
    public void Parse_JsonWrappedInMarkdownFence_StripsAndParses()
    {
        var fenced = "```json\n" + ValidJson + "\n```";

        var result = AiOutputParser.Parse(fenced);

        Assert.Equal(3m, result.CriterionScores["Grammar"]);
    }

    [Fact]
    public void Parse_MissingCriterionScores_ThrowsFormatException()
    {
        const string json = """
        { "feedback": "Some feedback." }
        """;

        var ex = Assert.Throws<FormatException>(() => AiOutputParser.Parse(json));
        Assert.Contains("criterionScores", ex.Message);
    }

    [Fact]
    public void Parse_EmptyCriterionScores_ThrowsFormatException()
    {
        const string json = """
        { "criterionScores": {}, "feedback": "Some feedback." }
        """;

        Assert.Throws<FormatException>(() => AiOutputParser.Parse(json));
    }

    [Fact]
    public void Parse_MissingFeedback_ThrowsFormatException()
    {
        const string json = """
        { "criterionScores": { "Grammar": 3 } }
        """;

        var ex = Assert.Throws<FormatException>(() => AiOutputParser.Parse(json));
        Assert.Contains("feedback", ex.Message);
    }

    [Fact]
    public void Parse_NullStrengthsWeaknessesCorrections_DefaultToEmptyLists()
    {
        const string json = """
        { "criterionScores": { "Grammar": 3 }, "feedback": "OK." }
        """;

        var result = AiOutputParser.Parse(json);

        Assert.Empty(result.Strengths!);
        Assert.Empty(result.Weaknesses!);
        Assert.Empty(result.Corrections!);
    }

    [Fact]
    public void Parse_NotJson_ThrowsFormatException()
    {
        const string garbage = "Sorry, I cannot help with that request.";

        Assert.Throws<FormatException>(() => AiOutputParser.Parse(garbage));
    }

    [Fact]
    public void Parse_EmptyString_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => AiOutputParser.Parse(""));
    }
}
