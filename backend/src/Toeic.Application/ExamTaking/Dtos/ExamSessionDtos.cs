using System.ComponentModel.DataAnnotations;
using Toeic.Domain.Enums;

namespace Toeic.Application.ExamTaking.Dtos;

public class StartExamSessionRequest
{
    [Required]
    public Guid ExamId { get; set; }
}

public class ExamSessionDto
{
    public Guid ExamSessionId { get; set; }
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = default!;
    public Guid TestId { get; set; }
    public string TestCode { get; set; } = default!;

    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public ExamSessionStatus Status { get; set; }

    /// <summary>Tính tại thời điểm trả response — server luôn là nguồn xác thực thời gian (mục 16 prompt gốc).</summary>
    public int RemainingSeconds { get; set; }

    public int ListeningDurationSeconds { get; set; }
    public int ReadingDurationSeconds { get; set; }
    public int WritingDurationSeconds { get; set; }
}

/// <summary>
/// Câu hỏi hiển thị cho Candidate — KHÔNG bao giờ chứa QuestionOption.IsCorrect (đó là Answer Key,
/// chỉ ADMIN/RATER được thấy qua ExamManagement API). Kèm câu trả lời đã lưu (nếu có) để hỗ trợ
/// resume phiên thi khi candidate load lại trang.
/// </summary>
public class ExamSessionQuestionDto
{
    public Guid QuestionId { get; set; }
    public Section Section { get; set; }
    public QuestionType QuestionType { get; set; }
    public int QuestionNumber { get; set; }
    public string QuestionText { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? RequiredWords { get; set; }
    public IReadOnlyList<CandidateVisibleOptionDto> Options { get; set; } = Array.Empty<CandidateVisibleOptionDto>();

    public Guid? MySelectedOptionId { get; set; }
    public string? MyAnswerText { get; set; }
    public AnswerStatus MyAnswerStatus { get; set; } = AnswerStatus.NOT_ANSWERED;
}

/// <summary>Option nhìn từ phía Candidate — cố ý KHÔNG có field IsCorrect.</summary>
public class CandidateVisibleOptionDto
{
    public Guid QuestionOptionId { get; set; }
    public char OptionLabel { get; set; }
    public string OptionText { get; set; } = default!;
}

public class SaveAnswerRequest
{
    [Required]
    public Guid QuestionId { get; set; }

    /// <summary>Chỉ dùng cho câu hỏi MULTIPLE_CHOICE.</summary>
    public Guid? SelectedOptionId { get; set; }

    /// <summary>Chỉ dùng cho câu hỏi Writing.</summary>
    public string? AnswerText { get; set; }
}

public class CandidateAnswerDto
{
    public Guid CandidateAnswerId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string? AnswerText { get; set; }
    public AnswerStatus Status { get; set; }
    public DateTime? AnsweredAt { get; set; }
}
