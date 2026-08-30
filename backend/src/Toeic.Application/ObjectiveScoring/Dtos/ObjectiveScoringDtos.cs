using System;

namespace Toeic.Application.ObjectiveScoring.Dtos;

/// <summary>
/// Tóm tắt kết quả 1 lần chấm Objective Scoring (dùng nội bộ + trả về ngay sau khi chấm).
/// </summary>
public sealed record ObjectiveScoringSummaryDto(
    Guid ExamSessionId,
    decimal ListeningScore,
    int ListeningCorrectCount,
    int ListeningTotalCount,
    decimal ReadingScore,
    int ReadingCorrectCount,
    int ReadingTotalCount,
    decimal? TotalScore);

/// <summary>
/// Kết quả đầy đủ trả về cho Candidate qua API (GET /exam-sessions/{id}/result).
/// WritingScore/TotalScore có thể NULL nếu Writing chưa được AI/Human chấm (trước Phase 3/4) —
/// Frontend cần xử lý hiển thị "đang chấm" cho phần Writing khi giá trị là NULL.
/// </summary>
public sealed record ExamSessionResultDto(
    Guid ExamSessionId,
    string Status,
    DateTime? SubmittedAt,
    decimal? ListeningScore,
    int ListeningCorrectCount,
    int ListeningTotalCount,
    decimal? ReadingScore,
    int ReadingCorrectCount,
    int ReadingTotalCount,
    decimal? WritingScore,
    decimal? TotalScore);
