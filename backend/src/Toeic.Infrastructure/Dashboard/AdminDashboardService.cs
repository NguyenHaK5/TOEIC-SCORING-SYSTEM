using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Toeic.Application.Dashboard;
using Toeic.Application.Dashboard.Dtos;
using Toeic.Domain.Enums;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.Dashboard;

/// <summary>
/// ⚠️ Xem PHASE6-ISSUE-06 (integration guide) trước khi tin tưởng
/// <see cref="GetWritingErrorAnalyticsAsync"/> — <c>CriterionKeys</c>/<c>ErrorTypeKeys</c>
/// bên dưới là GIẢ ĐỊNH về field name trong ErrorsJson, chưa được xác nhận với AI
/// Assessment Service thật (Phase 3 Part 3).
/// </summary>
public sealed class AdminDashboardService : IAdminDashboardService
{
    // Thứ tự ưu tiên field name khi parse — thử lần lượt, dùng field đầu tiên khớp.
    private static readonly string[] CriterionKeys = { "criterion", "category", "criterionCode" };
    private static readonly string[] ErrorTypeKeys = { "errorType", "type", "code", "issue" };

    private const string UnspecifiedCriterion = "UNSPECIFIED";
    private const string UnknownErrorType = "UNKNOWN";

    private readonly AppDbContext _db;
    private readonly IWritingSessionScoreAggregator _writingAggregator;

    public AdminDashboardService(AppDbContext db, IWritingSessionScoreAggregator writingAggregator)
    {
        _db = db;
        _writingAggregator = writingAggregator;
    }

    public async Task<AdminDashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var totalCandidates = await _db.Candidates.CountAsync(cancellationToken);
        var totalExams = await _db.Exams.CountAsync(cancellationToken);
        var totalSessions = await _db.ExamSessions.CountAsync(cancellationToken);
        var submittedCount = await _db.ExamSessions
            .CountAsync(s => s.Status == ExamSessionStatus.SUBMITTED, cancellationToken);
        var inProgressCount = await _db.ExamSessions
            .CountAsync(s => s.Status == ExamSessionStatus.IN_PROGRESS, cancellationToken);

        // ⚠️ Tải toàn bộ ListeningScore/ReadingScore của các phiên SUBMITTED vào bộ nhớ để
        // tính Median (không tính được Median trực tiếp trong SQL qua LINQ portable). Chấp
        // nhận được ở quy mô đồ án; nếu dữ liệu lớn (nhiều chục nghìn phiên), cần chuyển
        // sang tính median bằng SQL (PERCENTILE_CONT) hoặc cache định kỳ — không làm ở MVP
        // này để tránh over-engineering sớm.
        var submittedScores = await _db.ExamSessions
            .Where(s => s.Status == ExamSessionStatus.SUBMITTED)
            .Select(s => new { s.Id, s.ListeningScore, s.ReadingScore })
            .ToListAsync(cancellationToken);

        var listeningSummary = BuildScoreSummary(submittedScores.Select(s => s.ListeningScore));
        var readingSummary = BuildScoreSummary(submittedScores.Select(s => s.ReadingScore));

        var sessionIds = submittedScores.Select(s => s.Id).ToList();
        var writingScores = await _writingAggregator.GetWritingScoresForSessionsAsync(sessionIds, cancellationToken);
        var writingSummary = BuildScoreSummary(writingScores.Values);

        return new AdminDashboardOverviewDto
        {
            TotalCandidates = totalCandidates,
            TotalExams = totalExams,
            TotalExamSessions = totalSessions,
            SubmittedSessionsCount = submittedCount,
            InProgressSessionsCount = inProgressCount,
            ListeningScoreSummary = listeningSummary,
            ReadingScoreSummary = readingSummary,
            WritingScoreSummary = writingSummary
        };
    }

    public async Task<WritingErrorAnalyticsResponseDto> GetWritingErrorAnalyticsAsync(
        int topN, CancellationToken cancellationToken)
    {
        var limit = topN is < 1 or > 100 ? 20 : topN;

        // Chỉ lấy bản chấm AI hiện hành (IsCurrent=true), thuộc phiên đã SUBMITTED —
        // tránh đếm lặp lỗi từ các bản chấm lại lịch sử (cùng rule "chỉ 1 bản chính thức"
        // đã dùng xuyên suốt Phase 6).
        var rows = await (
            from w in _db.WritingAssessments
            join a in _db.CandidateAnswers on w.CandidateAnswerId equals a.Id
            join s in _db.ExamSessions on a.ExamSessionId equals s.Id
            join q in _db.Questions on a.QuestionId equals q.Id
            where w.IsCurrent && s.Status == ExamSessionStatus.SUBMITTED
            select new { w.ErrorsJson, q.QuestionType }
        ).ToListAsync(cancellationToken);

        var tally = new Dictionary<(string QuestionType, string Criterion, string ErrorType), int>();
        var unparseableCount = 0;

        foreach (var row in rows)
        {
            var (errors, parsedOk) = ParseErrorsJson(row.ErrorsJson);
            if (!parsedOk)
            {
                unparseableCount++;
                continue;
            }

            foreach (var (criterion, errorType) in errors)
            {
                var key = (row.QuestionType.ToString(), criterion, errorType);
                tally[key] = tally.TryGetValue(key, out var existing) ? existing + 1 : 1;
            }
        }

        var topErrors = tally
            .Select(kv => new WritingErrorAnalyticsItemDto
            {
                QuestionType = kv.Key.QuestionType,
                Criterion = kv.Key.Criterion,
                ErrorType = kv.Key.ErrorType,
                Count = kv.Value
            })
            .OrderByDescending(i => i.Count)
            .Take(limit)
            .ToList();

        return new WritingErrorAnalyticsResponseDto
        {
            TopErrors = topErrors,
            TotalAssessmentsAnalyzed = rows.Count,
            UnparseableErrorsJsonCount = unparseableCount
        };
    }

    private static ScoreSummaryDto BuildScoreSummary(IEnumerable<decimal?> values)
    {
        var nonNull = values.Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();

        if (nonNull.Count == 0)
        {
            return new ScoreSummaryDto { SampleSize = 0 };
        }

        var mid = nonNull.Count / 2;
        var median = nonNull.Count % 2 == 0
            ? (nonNull[mid - 1] + nonNull[mid]) / 2m
            : nonNull[mid];

        return new ScoreSummaryDto
        {
            SampleSize = nonNull.Count,
            Average = Math.Round(nonNull.Average(), 2),
            Min = nonNull.First(),
            Max = nonNull.Last(),
            Median = Math.Round(median, 2)
        };
    }

    /// <summary>
    /// Parser khoan dung cho ErrorsJson — xem PHASE6-ISSUE-06.
    /// Trả về (danh sách lỗi đã trích xuất, có parse được hay không).
    /// ErrorsJson NULL/rỗng = "không có lỗi", KHÔNG tính là parse thất bại.
    /// </summary>

    private static (List<(string Criterion, string ErrorType)> Errors, bool ParsedOk) ParseErrorsJson(string? errorsJson)
    {
        if (string.IsNullOrWhiteSpace(errorsJson))
            return (new List<(string, string)>(), true);

        try
        {
            using var doc = JsonDocument.Parse(errorsJson);
            var root = doc.RootElement;
            var list = new List<(string, string)>();

            // Chỉ lấy danh sách từ mảng Weaknesses (Không lấy Corrections vì Corrections là gợi ý sửa lỗi)
            if (root.TryGetProperty("Weaknesses", out var weaknessesElement) && weaknessesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in weaknessesElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        string errorStr = item.GetString() ?? UnknownErrorType;

                        // Thuật toán AI thường trả về Weaknesses dạng "[Tiêu chí] Mô tả lỗi" (Ví dụ: "[Grammar] Lỗi chia thì")
                        // Ta tách chuỗi này ra để lấy được Criterion và ErrorType
                        var criterion = UnspecifiedCriterion;
                        var errorType = errorStr;

                        if (errorStr.StartsWith("[") && errorStr.Contains("]"))
                        {
                            int endBracket = errorStr.IndexOf("]");
                            criterion = errorStr.Substring(1, endBracket - 1).Trim();
                            errorType = errorStr.Substring(endBracket + 1).Trim();
                        }

                        list.Add((criterion, errorType));
                    }
                }
            }

            return (list, true);
        }
        catch (JsonException)
        {
            return (new List<(string, string)>(), false);
        }
    }




    //private static (List<(string Criterion, string ErrorType)> Errors, bool ParsedOk) ParseErrorsJson(
    //    string? errorsJson)
    //{
    //    if (string.IsNullOrWhiteSpace(errorsJson))
    //    {
    //        return (new List<(string, string)>(), true);
    //    }

    //    try
    //    {
    //        using var doc = JsonDocument.Parse(errorsJson);
    //        var root = doc.RootElement;
    //        var list = new List<(string, string)>();

    //        switch (root.ValueKind)
    //        {
    //            case JsonValueKind.Array:
    //                foreach (var item in root.EnumerateArray())
    //                {
    //                    list.Add(ExtractErrorTuple(item));
    //                }
    //                break;

    //            case JsonValueKind.Object:
    //                // Fallback: có thể ErrorsJson là 1 object duy nhất, không phải mảng.
    //                list.Add(ExtractErrorTuple(root));
    //                break;

    //            default:
    //                // Không phải array/object -> không đúng dạng mong đợi, coi là không parse được.
    //                return (new List<(string, string)>(), false);
    //        }

    //        return (list, true);
    //    }
    //    catch (JsonException)
    //    {
    //        return (new List<(string, string)>(), false);
    //    }
    //}

    //private static (string Criterion, string ErrorType) ExtractErrorTuple(JsonElement item)
    //{
    //    if (item.ValueKind == JsonValueKind.String)
    //    {
    //        // Trường hợp mảng chỉ chứa chuỗi mô tả lỗi thuần (không có field criterion/type).
    //        return (UnspecifiedCriterion, item.GetString() ?? UnknownErrorType);
    //    }

    //    var criterion = UnspecifiedCriterion;
    //    var errorType = UnknownErrorType;

    //    if (item.ValueKind == JsonValueKind.Object)
    //    {
    //        foreach (var key in CriterionKeys)
    //        {
    //            if (item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
    //            {
    //                criterion = value.GetString()!;
    //                break;
    //            }
    //        }

    //        foreach (var key in ErrorTypeKeys)
    //        {
    //            if (item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
    //            {
    //                errorType = value.GetString()!;
    //                break;
    //            }
    //        }
    //    }

    //    return (criterion, errorType);
    //}
}
