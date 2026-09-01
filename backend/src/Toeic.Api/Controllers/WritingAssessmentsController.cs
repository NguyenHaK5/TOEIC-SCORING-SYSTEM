using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.WritingAssessment;
using Toeic.Application.WritingAssessment.Dtos;

namespace Toeic.Api.Controllers;

/// <summary>
/// "Preview" endpoint only — lets Admin manually test AI Assessment via
/// Swagger for any of the 3 Writing question types, without needing a real
/// ExamSession/CandidateAnswer. Nothing here is persisted. The real,
/// candidate-facing flow (triggered automatically on submit, results
/// persisted to WritingAssessment/AssessmentCriterionScores) is Phase 3
/// Part 4 — this endpoint exists purely so Part 3 can be demoed on its own,
/// per the project's "mỗi phase phải demo được" principle (mục 24).
/// </summary>
[ApiController]
[Route("api/v1/writing-assessments")]
public class WritingAssessmentsController : ApiControllerBase
{
    private readonly IAiAssessmentService _aiAssessmentService;

    public WritingAssessmentsController(IAiAssessmentService aiAssessmentService)
    {
        _aiAssessmentService = aiAssessmentService;
    }

    [HttpPost("preview")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<WritingAssessmentResult>> Preview(
        [FromBody] WritingAssessmentRequest request,
        CancellationToken ct)
        => Ok(await _aiAssessmentService.AssessAsync(request, ct));
}
