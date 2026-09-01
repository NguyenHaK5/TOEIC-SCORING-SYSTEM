using Toeic.Domain.Enums;

namespace Toeic.Application.WritingAssessment.Dtos;

public enum WritingQuestionStatus
{
    /// <summary>Candidate never submitted an answer for this Writing question.</summary>
    NotAnswered,

    /// <summary>Answered, but AI scoring has not completed yet (queued or in progress).</summary>
    Pending,

    /// <summary>AI scoring completed — OverallScore/Feedback/CriterionScores are populated.</summary>
    Scored
}

public class CriterionScoreDto
{
    public string Criterion { get; set; } = default!;
    public decimal Score { get; set; }
    public decimal Weight { get; set; }
}

public class WritingQuestionResultDto
{
    public Guid QuestionId { get; set; }
    public QuestionType QuestionType { get; set; }
    public WritingQuestionStatus Status { get; set; }
    public decimal? OverallScore { get; set; }
    public string? Feedback { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Corrections { get; set; } = new();
    public List<CriterionScoreDto> CriterionScores { get; set; } = new();

    public static WritingQuestionResultDto NotAnswered(Guid questionId, QuestionType type) => new()
    {
        QuestionId = questionId,
        QuestionType = type,
        Status = WritingQuestionStatus.NotAnswered
    };

    public static WritingQuestionResultDto Pending(Guid questionId, QuestionType type) => new()
    {
        QuestionId = questionId,
        QuestionType = type,
        Status = WritingQuestionStatus.Pending
    };
}

/// <summary>
/// Writing section of GET /exam-sessions/{id}/result. OverallScore is the
/// plain average of already-scored questions' OverallScore (0-100 scale
/// from WritingScoringEngine) — see PHASE3-ISSUE-07 in the integration
/// guide for why this is NOT combined with Listening/Reading into one
/// cross-section number.
/// </summary>
public class WritingSectionResultDto
{
    public int QuestionsTotal { get; set; }
    public int QuestionsScored { get; set; }
    public decimal? OverallScore { get; set; }
    public List<WritingQuestionResultDto> Questions { get; set; } = new();
}
