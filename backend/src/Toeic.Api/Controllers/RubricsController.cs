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
}
