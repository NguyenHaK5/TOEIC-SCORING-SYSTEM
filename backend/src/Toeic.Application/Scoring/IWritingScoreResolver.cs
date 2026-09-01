using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Toeic.Application.Scoring;

/// <summary>"HUMAN" | "AI" | "NONE" — nguồn của điểm Writing chính thức, theo quy tắc ISSUE-04 Phase 0.</summary>
public static class OfficialScoreSource
{
    public const string Human = "HUMAN";
    public const string Ai = "AI";
    public const string None = "NONE";
}

/// <summary>"Scored" | "Pending" | "NotAnswered" — trạng thái chấm của 1 câu Writing.</summary>
public static class OfficialScoreStatus
{
    public const string Scored = "Scored";
    public const string Pending = "Pending";
    public const string NotAnswered = "NotAnswered";
}

public class OfficialWritingScoreDto
{
    public Guid CandidateAnswerId { get; set; }
    public decimal? Score { get; set; }
    public string Source { get; set; } = OfficialScoreSource.None;
    public string Status { get; set; } = OfficialScoreStatus.NotAnswered;
}

/// <summary>
/// Phase 4 Part 2 — ISSUE-04 Phase 0: "điểm hiển thị chính thức = HumanAssessment.IsFinal=true nếu
/// tồn tại, ngược lại = WritingAssessment.IsCurrent=true". Đây là nơi DUY NHẤT trong hệ thống quyết
/// định điểm Writing "chính thức" cho 1 CandidateAnswer — mọi chỗ hiển thị điểm cho Candidate
/// (`ExamSessionService.GetResultAsync`, dashboard, v.v.) PHẢI gọi qua đây, không tự query
/// WritingAssessments/HumanAssessments trực tiếp nữa, để tránh 2 nơi có 2 quy tắc khác nhau.
/// </summary>
public interface IWritingScoreResolver
{
    /// <summary>Điểm chính thức cho 1 câu trả lời Writing.</summary>
    Task<OfficialWritingScoreDto> ResolveAsync(Guid candidateAnswerId, CancellationToken ct = default);

    /// <summary>
    /// Phiên bản batch — dùng khi 1 ExamSession có nhiều câu Writing (Q1-5/Q6-7/Q8), tránh N+1 query.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, OfficialWritingScoreDto>> ResolveManyAsync(
        IEnumerable<Guid> candidateAnswerIds, CancellationToken ct = default);
}
