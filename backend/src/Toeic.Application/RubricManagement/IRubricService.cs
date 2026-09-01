using Toeic.Application.Common.Dtos; // ASSUMPTION: PagedResult<T> already exists here (created in Phase 1 Part 3)
using Toeic.Application.RubricManagement.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Application.RubricManagement;

public interface IRubricService
{
    Task<RubricSetDto> CreateRubricSetAsync(Guid createdByUserId, CreateRubricSetRequest request, CancellationToken ct);

    Task<RubricSetDto> GetRubricSetAsync(Guid rubricSetId, CancellationToken ct);

    Task<PagedResult<RubricSetDto>> GetRubricSetsAsync(
        QuestionType? questionType,
        RubricSetStatus? status,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Used by the AI Assessment Service (Phase 3 Part 3) to fetch the rubric
    /// that must be applied when scoring a given question type. Returns null
    /// if no RubricSet is currently ACTIVE for that QuestionType.
    /// </summary>
    Task<RubricSetDto?> GetActiveRubricSetForQuestionTypeAsync(QuestionType questionType, CancellationToken ct);

    Task<RubricSetDto> UpdateRubricSetAsync(Guid rubricSetId, UpdateRubricSetRequest request, CancellationToken ct);

    Task DeleteRubricSetAsync(Guid rubricSetId, CancellationToken ct);

    /// <summary>
    /// DRAFT -> ACTIVE. Validates the rubric via RubricSetValidation first
    /// (throws ValidationAppException listing every problem found, not just
    /// the first). On success, auto-archives any other RubricSet already
    /// ACTIVE for the same QuestionType (PHASE3-ISSUE-01 — exactly one
    /// active rubric per question type).
    /// </summary>
    Task<RubricSetDto> ActivateRubricSetAsync(Guid rubricSetId, CancellationToken ct);

    /// <summary>ACTIVE -> ARCHIVED.</summary>
    Task<RubricSetDto> ArchiveRubricSetAsync(Guid rubricSetId, CancellationToken ct);

    Task<RubricDto> AddRubricAsync(Guid rubricSetId, Guid createdByUserId, CreateRubricRequest request, CancellationToken ct);

    Task<RubricDto> UpdateRubricAsync(Guid rubricId, UpdateRubricRequest request, CancellationToken ct);

    Task DeleteRubricAsync(Guid rubricId, CancellationToken ct);
}
