using Toeic.Domain.Enums;
using Toeic.Domain.Rubrics;

namespace Toeic.Application.WritingAssessment.Dtos;

/// <summary>
/// Input to AI Assessment. One shape covers all 3 Writing question types —
/// only the fields relevant to <see cref="QuestionType"/> need to be set:
///   PICTURE_SENTENCE (Q1-5): StudentAnswer, RequiredWords, ImageDescription (optional)
///   WRITTEN_RESPONSE (Q6-7): StudentAnswer, PromptOrQuestionText (the email/request prompt)
///   OPINION_ESSAY   (Q8)  : StudentAnswer, PromptOrQuestionText (the essay question)
/// </summary>
public class WritingAssessmentRequest
{
    public QuestionType QuestionType { get; set; }
    public string StudentAnswer { get; set; } = string.Empty;
    public string? PromptOrQuestionText { get; set; }
    public string? ImageDescription { get; set; }
    public List<string>? RequiredWords { get; set; }
}

/// <summary>
/// Structured JSON the LLM must return. CriterionScores keys are dynamic —
/// they come from the ACTIVE RubricSet's Criterion names at prompt-build
/// time (see PHASE3-ISSUE-02 in the integration guide), not a fixed schema
/// per question type. This is what lets Admin add/rename criteria in Part 1
/// without any code change here.
/// </summary>
public class RawAiAssessmentOutput
{
    public Dictionary<string, decimal> CriterionScores { get; set; } = new();
    public string Feedback { get; set; } = string.Empty;
    public List<string>? Strengths { get; set; }
    public List<string>? Weaknesses { get; set; }
    public List<string>? Corrections { get; set; }
}

/// <summary>
/// Final result of one AI assessment run: the deterministic score (from the
/// Phase 3 Part 2 WritingScoringEngine — the AI never sets this itself) plus
/// everything Phase 3 Part 4 needs to persist into WritingAssessment /
/// AssessmentCriterionScores for audit.
/// </summary>
public class WritingAssessmentResult
{
    public WritingScoringResult Scoring { get; set; } = default!;
    public string RawOutputJson { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public IReadOnlyList<string> Strengths { get; set; } = new List<string>();
    public IReadOnlyList<string> Weaknesses { get; set; } = new List<string>();
    public IReadOnlyList<string> Corrections { get; set; } = new List<string>();
    public Guid RubricSetId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}
