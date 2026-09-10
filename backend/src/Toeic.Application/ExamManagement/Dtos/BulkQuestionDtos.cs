// File: backend/src/Toeic.Application/ExamManagement/Dtos/BulkQuestionDtos.cs
namespace Toeic.Application.ExamManagement.Dtos;

public record BulkQuestionOptionDto(
    char OptionLabel,   // "A" | "B" | "C" | "D"
    string OptionText,
    bool IsCorrect
);

public record BulkQuestionRowDto(
    int RowNumber,               // số dòng trong file Excel (để trả lỗi đúng dòng, KHÔNG phải QuestionNumber)
    int QuestionNumber,
    string Section,              // "LISTENING" | "READING" | "WRITING"
    string QuestionType,         // "MULTIPLE_CHOICE" | "PICTURE_SENTENCE" | "WRITTEN_RESPONSE" | "OPINION_ESSAY"
    string QuestionText,
    string? ImageUrl,
    string? AudioUrl,
    string? RequiredWords,
    decimal? Points,
    List<BulkQuestionOptionDto> Options
);

public record BulkCreateQuestionsRequestDto(List<BulkQuestionRowDto> Rows);

public record BulkRowErrorDto(int RowNumber, List<string> Errors);

public record BulkCreateQuestionsResultDto(
    bool Success,                      // false nếu có bất kỳ dòng nào lỗi -> KHÔNG ghi gì cả (all-or-nothing)
    int TotalRows,
    int CreatedCount,
    List<BulkRowErrorDto> Errors
);


// File: backend/src/Toeic.Application/ExamManagement/Dtos/BulkQuestionDtos.cs — thêm vào file đã có ở ISSUE-19
public record BulkUpdateQuestionRowDto(
    int RowNumber,
    Guid QuestionId,             // BẮT BUỘC — định danh câu hỏi cần sửa, lấy từ cột ẩn khi export
    int QuestionNumber,
    string Section,
    string QuestionType,
    string QuestionText,
    string? ImageUrl,
    string? AudioUrl,
    string? RequiredWords,
    decimal? Points,
    List<BulkQuestionOptionDto> Options   // tái dùng nguyên type đã có ở ISSUE-19
);

public record BulkUpdateQuestionsRequestDto(List<BulkUpdateQuestionRowDto> Rows);

public record BulkUpdateQuestionsResultDto(
    bool Success,
    int TotalRows,
    int UpdatedCount,
    List<BulkRowErrorDto> Errors   // tái dùng type đã có ở ISSUE-19
);

// File: backend/src/Toeic.Application/ExamManagement/Dtos/QuestionExportRowDto.cs — MỚI, dùng cho export
public record QuestionExportRowDto(
    Guid QuestionId,
    int QuestionNumber,
    string Section,
    string QuestionType,
    string QuestionText,
    string? ImageUrl,
    string? AudioUrl,
    string? RequiredWords,
    decimal? Points,
    List<BulkQuestionOptionDto> Options
);