using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.RubricManagement;
using Toeic.Application.RubricManagement.Dtos;

namespace Toeic.Api.Controllers;

[ApiController]
[Route("api/v1/rubrics")]
[Authorize(Roles = "ADMIN")]
public class RubricsController : ApiControllerBase
{
    private readonly IRubricService _rubricService;

    public RubricsController(IRubricService rubricService)
    {
        _rubricService = rubricService;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RubricDto>> Update(
        Guid id,
        [FromBody] UpdateRubricRequest request,
        CancellationToken ct)
        => Ok(await _rubricService.UpdateRubricAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _rubricService.DeleteRubricAsync(id, ct);
        return NoContent();
    }




    [HttpPost("~/api/v1/rubric-sets/{rubricSetId:guid}/rubrics/bulk")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> BulkCreate(
    Guid rubricSetId, [FromBody] BulkCreateRubricsRequestDto request, CancellationToken ct)
    {
        var result = await _rubricService.BulkCreateAsync(rubricSetId, request, ct);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    [HttpGet("~/api/v1/rubric-sets/{rubricSetId:guid}/rubrics/export")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Export(Guid rubricSetId, CancellationToken ct)
    {
        var rows = await _rubricService.ExportForRubricSetAsync(rubricSetId, ct);
        return Ok(rows);
    }

    [HttpPut("~/api/v1/rubric-sets/{rubricSetId:guid}/rubrics/bulk")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> BulkUpdate(
        Guid rubricSetId, [FromBody] BulkUpdateRubricsRequestDto request, CancellationToken ct)
    {
        var result = await _rubricService.BulkUpdateAsync(rubricSetId, request, ct);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }
}
