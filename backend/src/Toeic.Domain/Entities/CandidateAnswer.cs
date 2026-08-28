using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>
/// Câu trả lời của thí sinh cho 1 câu hỏi trong 1 ExamSession.
/// UNIQUE(ExamSessionId, QuestionId) (ISSUE-05) — auto-save phải dùng UPSERT, không insert nhiều dòng.
/// RowVersion map xmin cho optimistic concurrency, cùng cơ chế với ExamSession.
/// Không kế thừa BaseEntity vì không cần UpdatedAt riêng (dùng AnsweredAt thay thế ngữ nghĩa "lần trả lời gần nhất").
/// </summary>
public class CandidateAnswer
{
    public Guid Id { get; set; }
    public Guid ExamSessionId { get; set; }
    public Guid QuestionId { get; set; }

    /// <summary>Chỉ dùng cho câu hỏi Multiple Choice (Listening/Reading).</summary>
    public Guid? SelectedOptionId { get; set; }

    /// <summary>Nội dung trả lời tự luận (Writing).</summary>
    public string? AnswerText { get; set; }

    public DateTime? AnsweredAt { get; set; }

    /// <summary>Đúng/sai cho trắc nghiệm; NULL cho tự luận (chấm bởi AI/Human riêng).</summary>
    public bool? IsCorrect { get; set; }

    public decimal? Score { get; set; }
    public AnswerStatus Status { get; set; } = AnswerStatus.NOT_ANSWERED;

    public uint RowVersion { get; set; }

    // Navigation
    public ExamSession ExamSession { get; set; } = default!;
    public Question Question { get; set; } = default!;
    public QuestionOption? SelectedOption { get; set; }
    public ICollection<WritingAssessment> WritingAssessments { get; set; } = new List<WritingAssessment>();
    public ICollection<HumanAssessment> HumanAssessments { get; set; } = new List<HumanAssessment>();
}
