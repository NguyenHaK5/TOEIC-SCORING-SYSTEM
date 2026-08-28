using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toeic.Application.ExamManagement;
using Toeic.Application.ExamManagement.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Api.Controllers;

/// <summary>
/// Quản lý Question + QuestionOption. Question luôn thuộc 1 Test (nested route), QuestionOption
/// thuộc 1 Question — dùng route riêng "question-options/{id}" cho thao tác sửa/xoá trực tiếp
/// theo Id (không cần lồng qua QuestionId trong URL) để đơn giản hoá.
/// Toàn bộ endpoint chỉ ADMIN được gọi.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize(Roles = nameof(UserRole.ADMIN))]
public class QuestionsController : ApiControllerBase
{
    private readonly IQuestionService _questionService;

    public QuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    [HttpGet("tests/{testId:guid}/questions")]
    [ProducesResponseType(typeof(IReadOnlyList<QuestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByTest(Guid testId, [FromQuery] Section? section, CancellationToken ct)
    {
        var questions = await _questionService.ListByTestAsync(testId, section, ct);
        return Ok(questions);
    }

    [HttpPost("tests/{testId:guid}/questions")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid testId, [FromBody] CreateQuestionRequest request, CancellationToken ct)
    {
        var question = await _questionService.CreateAsync(testId, request, CurrentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { questionId = question.QuestionId }, question);
    }

    [HttpGet("questions/{questionId:guid}")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid questionId, CancellationToken ct)
    {
        var question = await _questionService.GetByIdAsync(questionId, ct);
        return Ok(question);
    }

    [HttpPut("questions/{questionId:guid}")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid questionId, [FromBody] UpdateQuestionRequest request, CancellationToken ct)
    {
        var question = await _questionService.UpdateAsync(questionId, request, ct);
        return Ok(question);
    }

    [HttpDelete("questions/{questionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid questionId, CancellationToken ct)
    {
        await _questionService.DeleteAsync(questionId, ct);
        return NoContent();
    }

    [HttpPost("questions/{questionId:guid}/options")]
    [ProducesResponseType(typeof(QuestionOptionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddOption(Guid questionId, [FromBody] CreateQuestionOptionRequest request, CancellationToken ct)
    {
        var option = await _questionService.AddOptionAsync(questionId, request, ct);
        return CreatedAtAction(nameof(GetById), new { questionId }, option);
    }

    [HttpPut("question-options/{optionId:guid}")]
    [ProducesResponseType(typeof(QuestionOptionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOption(Guid optionId, [FromBody] UpdateQuestionOptionRequest request, CancellationToken ct)
    {
        var option = await _questionService.UpdateOptionAsync(optionId, request, ct);
        return Ok(option);
    }

    [HttpDelete("question-options/{optionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOption(Guid optionId, CancellationToken ct)
    {
        await _questionService.DeleteOptionAsync(optionId, ct);
        return NoContent();
    }
}
