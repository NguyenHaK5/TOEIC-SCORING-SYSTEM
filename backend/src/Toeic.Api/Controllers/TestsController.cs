using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.Common.Dtos;
using Toeic.Application.ExamManagement;
using Toeic.Application.ExamManagement.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Api.Controllers;

/// <summary>Quản lý Test/bộ đề — toàn bộ endpoint chỉ ADMIN được gọi.</summary>
[ApiController]
[Route("api/v1/tests")]
[Authorize(Roles = nameof(UserRole.ADMIN))]
public class TestsController : ApiControllerBase
{
    private readonly ITestService _testService;

    public TestsController(ITestService testService)
    {
        _testService = testService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TestDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTestRequest request, CancellationToken ct)
    {
        var test = await _testService.CreateAsync(request, CurrentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { testId = test.TestId }, test);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TestListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] TestListQuery query, CancellationToken ct)
    {
        var result = await _testService.ListAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{testId:guid}")]
    [ProducesResponseType(typeof(TestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid testId, CancellationToken ct)
    {
        var test = await _testService.GetByIdAsync(testId, ct);
        return Ok(test);
    }

    [HttpPut("{testId:guid}")]
    [ProducesResponseType(typeof(TestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid testId, [FromBody] UpdateTestRequest request, CancellationToken ct)
    {
        var test = await _testService.UpdateAsync(testId, request, ct);
        return Ok(test);
    }

    [HttpDelete("{testId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid testId, CancellationToken ct)
    {
        await _testService.DeleteAsync(testId, ct);
        return NoContent();
    }

    /// <summary>DRAFT → PUBLISHED. Validate toàn bộ câu hỏi trước khi cho phép publish.</summary>
    [HttpPost("{testId:guid}/publish")]
    [ProducesResponseType(typeof(TestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(Guid testId, CancellationToken ct)
    {
        var test = await _testService.PublishAsync(testId, ct);
        return Ok(test);
    }

    /// <summary>PUBLISHED → ARCHIVED.</summary>
    [HttpPost("{testId:guid}/archive")]
    [ProducesResponseType(typeof(TestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Archive(Guid testId, CancellationToken ct)
    {
        var test = await _testService.ArchiveAsync(testId, ct);
        return Ok(test);
    }
}
