using Toeic.Domain.Entities;

namespace Toeic.Domain.Scoring;

/// <summary>
/// Kết quả chấm 1 câu trắc nghiệm (Listening/Reading).
/// </summary>
public sealed record ObjectiveScoringResult(bool IsCorrect, decimal Score);

/// <summary>
/// Domain logic tất định cho việc chấm trắc nghiệm (Listening/Reading).
/// KHÔNG phụ thuộc EF Core / DbContext / AI — chỉ nhận dữ liệu đã load sẵn và trả về kết quả.
/// Đặt trong Domain layer theo đúng quyết định kiến trúc ở PHASE0 (mục G):
/// "Scoring Engine là domain logic tất định, không phụ thuộc AI".
/// </summary>
public static class ObjectiveScoringEngine
{
    /// <summary>
    /// Điểm mặc định cho 1 câu trắc nghiệm khi <see cref="Question.Points"/> chưa được thiết lập (NULL).
    /// Quyết định thiết kế (Phase 2): Questions.Points là "điểm cơ sở/tham chiếu" tuỳ chọn theo schema gốc;
    /// khi Admin chưa nhập, mặc định 1 điểm/câu thay vì coi câu đó là 0 điểm nếu trả lời đúng —
    /// tránh vô tình loại một câu ra khỏi tổng điểm chỉ vì thiếu cấu hình.
    /// </summary>
    public const decimal DefaultQuestionPoints = 1m;

    /// <summary>
    /// Chấm 1 câu trắc nghiệm: so khớp <paramref name="selectedOptionId"/> của thí sinh với
    /// Answer Key (QuestionOptions.IsCorrect) của câu hỏi.
    /// </summary>
    /// <param name="selectedOptionId">Option thí sinh đã chọn (NULL nếu bỏ trống/không trả lời).</param>
    /// <param name="options">Toàn bộ QuestionOption của câu hỏi (dùng để tra Answer Key).</param>
    /// <param name="questionPoints">Question.Points — điểm cơ sở của câu hỏi (NULL → dùng mặc định).</param>
    public static ObjectiveScoringResult EvaluateMultipleChoice(
        Guid? selectedOptionId,
        IReadOnlyCollection<QuestionOption> options,
        decimal? questionPoints)
    {
        if (options is null || options.Count == 0)
        {
            // Không có Answer Key để đối chiếu — coi như sai, điểm 0, không throw để không chặn
            // việc chấm toàn bộ session chỉ vì 1 câu hỏi cấu hình thiếu (đã có validation ở
            // Test.Publish() Phase 1 ngăn việc này xảy ra trong luồng bình thường).
            return new ObjectiveScoringResult(false, 0m);
        }

        if (selectedOptionId is null)
        {
            // Thí sinh không trả lời (bỏ trống) — sai, 0 điểm.
            return new ObjectiveScoringResult(false, 0m);
        }

        var selected = options.FirstOrDefault(o => o.Id == selectedOptionId.Value);
        if (selected is null)
        {
            // Dữ liệu bất thường: SelectedOptionId không thuộc về câu hỏi này.
            // Không throw ở Domain layer (đây là dữ liệu, không phải lỗi lập trình) — coi là sai.
            return new ObjectiveScoringResult(false, 0m);
        }

        var isCorrect = selected.IsCorrect;
        var points = questionPoints ?? DefaultQuestionPoints;
        return new ObjectiveScoringResult(isCorrect, isCorrect ? points : 0m);
    }
}
