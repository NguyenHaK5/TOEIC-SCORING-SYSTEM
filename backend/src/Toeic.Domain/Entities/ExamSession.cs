using Toeic.Domain.Common;
using Toeic.Domain.Enums;

namespace Toeic.Domain.Entities;

/// <summary>
/// Phiên dự thi của 1 Candidate cho 1 Exam/Test cụ thể.
/// ExpiresAt được chốt MỘT LẦN tại thời điểm StartedAt (ISSUE-07) — mọi auto-submit/khoá
/// câu hỏi phải dựa vào cột này, KHÔNG tính lại từ Test.*DurationSeconds.
/// RowVersion map cột hệ thống xmin của PostgreSQL cho optimistic concurrency (auto-save).
/// </summary>
public class ExamSession : BaseEntity
{
    public Guid ExamId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid TestId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public ExamSessionStatus Status { get; set; } = ExamSessionStatus.NOT_STARTED;

    // Cache/denormalized — luôn được tính lại từ CandidateAnswers/WritingAssessments/HumanAssessments,
    // không bao giờ sửa tay trực tiếp (Phase 0, ghi chú bổ sung).
    public decimal? ListeningScore { get; set; }
    public decimal? ReadingScore { get; set; }
    public decimal? WritingScore { get; set; }
    public decimal? TotalScore { get; set; }

    /// <summary>EF Core: .IsRowVersion() trỏ vào xmin native của PostgreSQL — không phải cột vật lý riêng.</summary>
    public uint RowVersion { get; set; }

    // Navigation
    public Exam Exam { get; set; } = default!;
    public Candidate Candidate { get; set; } = default!;
    public Test Test { get; set; } = default!;
    public ICollection<CandidateAnswer> CandidateAnswers { get; set; } = new List<CandidateAnswer>();
}
