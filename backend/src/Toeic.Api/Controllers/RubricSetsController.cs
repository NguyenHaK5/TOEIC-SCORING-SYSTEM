using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.Common.Dtos;
using Toeic.Application.RubricManagement;
using Toeic.Application.RubricManagement.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Api.Controllers;

[ApiController]
[Route("api/v1/rubric-sets")]
public class RubricSetsController : ApiControllerBase // ASSUMPTION: ApiControllerBase already exists
                                                        // (created in Phase 1 Part 3) and exposes a
                                                        // `CurrentUserId` property read from the JWT
                                                        // `sub` claim. Replace with the real accessor
                                                        // if the name differs.
{
    private readonly IRubricService _rubricService;

    public RubricSetsController(IRubricService rubricService)
    {
        _rubricService = rubricService;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<RubricSetDto>> Create(
        [FromBody] CreateRubricSetRequest request,
        CancellationToken ct)
    {
        var result = await _rubricService.CreateRubricSetAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.RubricSetId }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMIN,RATER")]
    public async Task<ActionResult<RubricSetDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _rubricService.GetRubricSetAsync(id, ct));

    [HttpGet]
    [Authorize(Roles = "ADMIN,RATER")]
    public async Task<ActionResult<PagedResult<RubricSetDto>>> GetAll(
        [FromQuery] QuestionType? questionType,
        [FromQuery] RubricSetStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _rubricService.GetRubricSetsAsync(questionType, status, page, pageSize, ct));

    [HttpGet("active")]
    [Authorize(Roles = "ADMIN,RATER")]
    public async Task<ActionResult<RubricSetDto>> GetActiveForQuestionType(
        [FromQuery] QuestionType questionType,
        CancellationToken ct)
    {
        var result = await _rubricService.GetActiveRubricSetForQuestionTypeAsync(questionType, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<RubricSetDto>> Update(
        Guid id,
        [FromBody] UpdateRubricSetRequest request,
        CancellationToken ct)
        => Ok(await _rubricService.UpdateRubricSetAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _rubricService.DeleteRubricSetAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<RubricSetDto>> Activate(Guid id, CancellationToken ct)
        => Ok(await _rubricService.ActivateRubricSetAsync(id, ct));

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<RubricSetDto>> Archive(Guid id, CancellationToken ct)
        => Ok(await _rubricService.ArchiveRubricSetAsync(id, ct));

    [HttpPost("{id:guid}/rubrics")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<RubricDto>> AddRubric(
        Guid id,
        [FromBody] CreateRubricRequest request,
        CancellationToken ct)
        => Ok(await _rubricService.AddRubricAsync(id, CurrentUserId, request, ct));
}
