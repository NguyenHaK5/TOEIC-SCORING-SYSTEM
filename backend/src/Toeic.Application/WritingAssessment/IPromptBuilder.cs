using Toeic.Application.WritingAssessment.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;

namespace Toeic.Application.WritingAssessment;

/// <summary>
/// The exact prompt sent to the LLM, plus metadata needed for audit
/// (PromptVersion is persisted alongside the score in Part 4 so a later
/// rubric/prompt change never silently reinterprets an old assessment).
/// </summary>
public record AiPromptRequest(
    string SystemPrompt,
    string UserPrompt,
    string PromptVersion,
    IReadOnlyList<string> ExpectedCriteria);

/// <summary>
/// Builds the prompt for one Writing question type from the currently
/// ACTIVE RubricSet's Rubric rows (see PHASE3-ISSUE-02: the JSON schema
/// requested from the LLM is generated dynamically from the rubric's
/// Criterion/MinScore/MaxScore, not hard-coded per question type).
/// </summary>
public interface IPromptBuilder
{
    AiPromptRequest Build(QuestionType questionType, WritingAssessmentRequest input, IReadOnlyCollection<Rubric> rubrics);
}
