using Toeic.Application.ExamManagement.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Application.ExamManagement;

public interface IQuestionService
{
    /// <summary>Chỉ thêm được câu hỏi khi Test cha đang DRAFT (bảo toàn đề đã publish).</summary>
    Task<QuestionDto> CreateAsync(Guid testId, CreateQuestionRequest request, Guid createdByUserId, CancellationToken ct = default);

    Task<QuestionDto> UpdateAsync(Guid questionId, UpdateQuestionRequest request, CancellationToken ct = default);
    Task<QuestionDto> GetByIdAsync(Guid questionId, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionDto>> ListByTestAsync(Guid testId, Section? section, CancellationToken ct = default);
    Task DeleteAsync(Guid questionId, CancellationToken ct = default);

    /// <summary>Chỉ hợp lệ khi Question.QuestionType = MULTIPLE_CHOICE.</summary>
    Task<QuestionOptionDto> AddOptionAsync(Guid questionId, CreateQuestionOptionRequest request, CancellationToken ct = default);

    Task<QuestionOptionDto> UpdateOptionAsync(Guid optionId, UpdateQuestionOptionRequest request, CancellationToken ct = default);
    Task DeleteOptionAsync(Guid optionId, CancellationToken ct = default);



    Task<BulkCreateQuestionsResultDto> BulkCreateAsync(Guid testId, BulkCreateQuestionsRequestDto request, CancellationToken ct = default);

    Task<List<QuestionExportRowDto>> ExportForTestAsync(Guid testId, CancellationToken ct = default);

    Task<BulkUpdateQuestionsResultDto> BulkUpdateAsync(
        Guid testId, BulkUpdateQuestionsRequestDto request, CancellationToken ct = default);
}
