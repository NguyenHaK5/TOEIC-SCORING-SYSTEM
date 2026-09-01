using System;

namespace Toeic.Application.Dashboard.Dtos;

/// <summary>
/// 1 dòng trong danh sách lịch sử thi của Candidate.
/// Cố ý KHÔNG có "TotalScore" gộp — Listening/Reading/Writing khác thang điểm
/// (xem PHASE3-ISSUE-07, tái khẳng định ở PHASE6-ISSUE-02).
/// </summary>
public sealed class ExamHistoryItemDto
{
    public Guid ExamSessionId { get; set; }
    public Guid ExamId { get; set; }
    public string ExamCode { get; set; } = default!;
    public string ExamName { get; set; } = default!;
    public string TestCode { get; set; } = default!;

    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    /// <summary>Chuỗi tên của ExamSessionStatus (NOT_STARTED/IN_PROGRESS/SUBMITTED/ABSENT/CANCELLED).</summary>
    public string Status { get; set; } = default!;

    public decimal? ListeningScore { get; set; }
    public decimal? ReadingScore { get; set; }

    /// <summary>
    /// Điểm Writing trung bình của phiên thi (theo IWritingSessionScoreAggregator).
    /// NULL khi WritingStatus != "SCORED".
    /// </summary>
    public decimal? WritingScore { get; set; }

    /// <summary>
    /// "NO_WRITING_QUESTIONS" | "PENDING" (đã nộp nhưng AI/Human chưa chấm xong) | "SCORED"
    /// </summary>
    public string WritingStatus { get; set; } = default!;
}

/// <summary>Tham số truy vấn cho GET /candidates/me/history.</summary>
public sealed class HistoryQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Lọc theo ExamSessions.Status. Nếu để trống, mặc định CHỈ trả về "SUBMITTED"
    /// (ẩn IN_PROGRESS/CANCELLED khỏi màn hình lịch sử — đây không phải "lịch sử" theo nghĩa
    /// người dùng, mà là phiên đang/chưa hoàn tất).
    /// </summary>
    public string? Status { get; set; }
}
