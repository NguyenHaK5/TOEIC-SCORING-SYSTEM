using Toeic.Application.Common.Dtos;
using Toeic.Application.ExamManagement.Dtos;

namespace Toeic.Application.ExamManagement;

public interface ITestService
{
    Task<TestDto> CreateAsync(CreateTestRequest request, Guid createdByUserId, CancellationToken ct = default);

    /// <summary>Chỉ sửa được khi Test đang ở trạng thái DRAFT (bảo toàn nội dung đề đã publish).</summary>
    Task<TestDto> UpdateAsync(Guid testId, UpdateTestRequest request, CancellationToken ct = default);

    Task<TestDto> GetByIdAsync(Guid testId, CancellationToken ct = default);
    Task<PagedResult<TestListItemDto>> ListAsync(TestListQuery query, CancellationToken ct = default);

    /// <summary>Chỉ xoá được Test ở trạng thái DRAFT và chưa được gán vào Exam nào.</summary>
    Task DeleteAsync(Guid testId, CancellationToken ct = default);

    /// <summary>
    /// DRAFT → PUBLISHED. Validate toàn bộ câu hỏi trước khi publish: mỗi câu Listening/Reading
    /// (MULTIPLE_CHOICE) phải có ≥2 option và đúng 1 option IsCorrect=true; câu Listening phải có
    /// AudioUrl; câu Picture Description (Q1–5) phải có ImageUrl. Ném ValidationAppException liệt
    /// kê đầy đủ lỗi nếu có, không publish "nửa vời".
    /// </summary>
    Task<TestDto> PublishAsync(Guid testId, CancellationToken ct = default);

    /// <summary>PUBLISHED → ARCHIVED. Test đã archive không còn được gán mới vào Exam.</summary>
    Task<TestDto> ArchiveAsync(Guid testId, CancellationToken ct = default);
}
