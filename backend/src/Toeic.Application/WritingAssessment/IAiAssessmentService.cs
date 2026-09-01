using Toeic.Application.WritingAssessment.Dtos;

namespace Toeic.Application.WritingAssessment;

/// <summary>
/// Orchestrates: fetch ACTIVE rubric -> build prompt -> call LLM -> parse +
/// validate output -> compute OverallScore via WritingScoringEngine.
///
/// Does NOT persist anything (no WritingAssessment/AssessmentCriterionScores
/// row is written here) — that wiring is Phase 3 Part 4, which will call
/// this service then save the returned WritingAssessmentResult. Keeping
/// this Part free of persistence lets it be demoed/tested standalone via
/// the `/writing-assessments/preview` endpoint before Part 4 exists.
/// </summary>
public interface IAiAssessmentService
{
    Task<WritingAssessmentResult> AssessAsync(WritingAssessmentRequest request, CancellationToken ct);
}
