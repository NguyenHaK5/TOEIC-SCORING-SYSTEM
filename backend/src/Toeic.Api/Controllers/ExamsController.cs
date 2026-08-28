using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.ExamManagement;
using Toeic.Application.ExamManagement.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Api.Controllers;

/// <summary>Quản lý Exam (kỳ thi) — toàn bộ endpoint chỉ ADMIN được gọi (mục 12, prompt gốc: "Exam Management").</summary>
[ApiController]
[Route("api/v1/exams")]
[Authorize(Roles = nameof(UserRole.ADMIN))]
public class ExamsController : ApiControllerBase
{
    private readonly IExamService _examService;

    public ExamsController(IExamService examService)
    {
        _examService = examService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateExamRequest request, CancellationToken ct)
    {
        var exam = await _examService.CreateAsync(request, CurrentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { examId = exam.ExamId }, exam);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Toeic.Application.Common.Dtos.PagedResult<ExamListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] ExamListQuery query, CancellationToken ct)
    {
        var result = await _examService.ListAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{examId:guid}")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid examId, CancellationToken ct)
    {
        var exam = await _examService.GetByIdAsync(examId, ct);
        return Ok(exam);
    }

    [HttpPut("{examId:guid}")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid examId, [FromBody] UpdateExamRequest request, CancellationToken ct)
    {
        var exam = await _examService.UpdateAsync(examId, request, ct);
        return Ok(exam);
    }

    [HttpDelete("{examId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid examId, CancellationToken ct)
    {
        await _examService.DeleteAsync(examId, ct);
        return NoContent();
    }

    /// <summary>Chuyển trạng thái theo state machine DRAFT→SCHEDULED→ONGOING→COMPLETED / CANCELLED.</summary>
    [HttpPost("{examId:guid}/status")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(Guid examId, [FromBody] ChangeExamStatusRequest request, CancellationToken ct)
    {
        var exam = await _examService.ChangeStatusAsync(examId, request.Status, ct);
        return Ok(exam);
    }

    [HttpPost("{examId:guid}/tests")]
    [ProducesResponseType(typeof(ExamTestSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignTest(Guid examId, [FromBody] AssignTestToExamRequest request, CancellationToken ct)
    {
        var result = await _examService.AssignTestAsync(examId, request.TestId, ct);
        return Ok(result);
    }

    [HttpDelete("{examId:guid}/tests/{testId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnassignTest(Guid examId, Guid testId, CancellationToken ct)
    {
        await _examService.UnassignTestAsync(examId, testId, ct);
        return NoContent();
    }
}
