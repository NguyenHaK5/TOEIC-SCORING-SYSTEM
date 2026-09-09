using Toeic.Application.Common.Dtos;
using Toeic.Application.ExamManagement.Dtos;
using Toeic.Domain.Enums;

namespace Toeic.Application.ExamManagement;

public interface IExamService
{
    Task<ExamDto> CreateAsync(CreateExamRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<ExamDto> UpdateAsync(Guid examId, UpdateExamRequest request, CancellationToken ct = default);
    Task<ExamDto> GetByIdAsync(Guid examId, CancellationToken ct = default);
    Task<PagedResult<ExamListItemDto>> ListAsync(ExamListQuery query, CancellationToken ct = default);

    /// <summary>Chỉ xoá được Exam ở trạng thái DRAFT và chưa có ExamSession nào tham chiếu.</summary>
    Task DeleteAsync(Guid examId, CancellationToken ct = default);

    /// <summary>
    /// Chuyển trạng thái theo state machine: DRAFT→SCHEDULED→ONGOING→COMPLETED,
    /// hoặc →CANCELLED từ bất kỳ trạng thái nào chưa COMPLETED. Publish (SCHEDULED)
    /// yêu cầu Exam phải có ít nhất 1 Test đang IsActive trong ExamTests.
    /// </summary>
    Task<ExamDto> ChangeStatusAsync(Guid examId, ExamStatus newStatus, CancellationToken ct = default);

    /// <summary>
    /// Gán 1 Test (PUBLISHED) vào Exam (ISSUE-01, Phase 0). Idempotent: nếu đã tồn tại nhưng
    /// IsActive=false thì set lại true; nếu chưa tồn tại thì tạo mới.
    /// </summary>
    Task<ExamTestSummaryDto> AssignTestAsync(Guid examId, Guid testId, CancellationToken ct = default);

    /// <summary>
    /// Gỡ Test khỏi Exam — set IsActive=false, KHÔNG xoá dòng ExamTests (giữ lịch sử,
    /// vì ExamSessions cũ vẫn tham chiếu TestId này qua Exam đó).
    /// </summary>
    Task UnassignTestAsync(Guid examId, Guid testId, CancellationToken ct = default);



    Task<List<CandidateExamSummaryDto>> GetAvailableForCandidateAsync(CancellationToken ct = default);
}
