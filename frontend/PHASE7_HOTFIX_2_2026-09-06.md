# PHASE 7 — HOTFIX #2 (06/09/2026): Sửa chính xác 5 shape DTO bằng JSON thật + đề xuất backend cho PHASE7-ISSUE-10

> Dragon gửi JSON response thật của 5 endpoint. File này ghi lại các sửa
> đổi chính xác (thay cho fallback phòng thủ ở Hotfix #1) + đề xuất code
> backend cụ thể để giải quyết PHASE7-ISSUE-10.

## Phần 1 — 5 shape DTO đã sửa CHÍNH XÁC (không còn là giả định/fallback)

Tất cả đã được tự verify lại bằng Playwright với **đúng nguyên văn JSON**
Dragon gửi (không phải JSON tự bịa) — cả 5 trang render đúng, 0 lỗi runtime.

### 1. `GET /admin/dashboard/writing-errors`
| Trước (giả định) | Sau (xác nhận thật) |
|---|---|
| `items` | `topErrors` |
| `errorType`, `occurrenceCount` | `errorType`, `count` (+ `questionType`, `criterion` đi kèm mỗi lỗi) |
| `unparseableCount` | `unparseableErrorsJsonCount` |
| — | `totalAssessmentsAnalyzed` (mới, đã thêm hiển thị) |

File sửa: `types/admin-dashboard.ts`, `app/admin/dashboard/page.tsx`.

### 2. `GET /api/v1/evaluations/metrics` (qua proxy `/api/evaluation/metrics`)
| Trước (giả định) | Sau (xác nhận thật) |
|---|---|
| `pearson`, `spearman` | `pearsonR`, `spearmanR` (+ `pearsonPValue`/`spearmanPValue` mới) |
| `notes: string` | `notes: string[]` (mảng, render từng dòng) |
| Dataset: `{totalPairs, byQuestionType[]}` | `{totalPairs, totalCriterionRows}` (suy từ `datasetInfo` lồng trong Metrics — bản thân `/dataset` độc lập vẫn CHƯA có ví dụ riêng, coi là cùng shape) |

File sửa: `types/evaluation.ts`, `app/admin/evaluation/page.tsx`.

**Còn mở:** endpoint `/report` và `/report/export` vẫn CHƯA có ví dụ JSON
— giữ nguyên giả định cũ, chưa xác nhận.

### 3. `GET /exam-sessions/{id}/result`
Đây là thay đổi lớn nhất — cấu trúc thật khác hẳn thiết kế ban đầu:

| Trước (giả định) | Sau (xác nhận thật) |
|---|---|
| `result.listening.{score,correctCount,totalQuestions,unansweredCount}` (object lồng) | Phẳng ở top-level: `listeningScore`, `listeningCorrectCount`, `listeningTotalCount` (KHÔNG có unanswered riêng — chỉ suy ra "chưa đúng" = total − correct) |
| `writing.overallScore`, `writing.questions[]` | Giữ cấu trúc lồng nhưng thêm `questionsTotal`/`questionsScored` |
| Status `"SCORED"`/`"PENDING"`/`"NOT_ANSWERED"` (SCREAMING_CASE) | `"Scored"`/`"Pending"`/`"NotAnswered"` (PascalCase — đúng như tracker đã ghi, tôi từng gõ sai case) |
| Mỗi câu có `questionNumber` | KHÔNG có — chỉ có `questionId`. Frontend tạm dùng số thứ tự trong mảng làm nhãn hiển thị. |
| `criterionScores[].criterionCode` | `criterionScores[].criterion` |
| — | `weaknesses[]`/`strengths[]`/`corrections[]` (mới, đã thêm hiển thị `weaknesses`) |
| `examName` | KHÔNG tồn tại trong response — đã bỏ khỏi UI |
| — | `totalScore` có tồn tại nhưng **cố ý không hiển thị** (đúng quyết định PHASE3-ISSUE-07/PHASE6-ISSUE-02 — không gộp thang điểm) |

File sửa: `types/candidate-result.ts` (viết lại hoàn toàn),
`components/candidate/{writing-status-badge,section-score-card}.tsx`,
`app/candidate/results/[sessionId]/page.tsx` (viết lại hoàn toàn).

### 4. `GET /candidates/me/analytics`
| Trước (giả định) | Sau (xác nhận thật) |
|---|---|
| `listeningReadingAccuracy` | `objectiveWeakness` |
| `writingCriteriaAverages` | `writingWeakness` |
| `sampleCount` | `sampleSize` |
| `scoreTrend` | Khớp đúng ngay từ đầu, không đổi |

**Còn mở:** `objectiveWeakness` trong ví dụ thật đang rỗng (`[]`) — tên
field top-level đã chắc chắn đúng, nhưng shape từng phần tử bên trong
(khi Listening/Reading có dữ liệu) vẫn là giả định, cần thêm 1 ví dụ khi
Dragon có dữ liệu Listening/Reading thật.

File sửa: `types/candidate-analytics.ts`, `app/candidate/analytics/page.tsx`.

### 5. `GET /rater/writing-answers/{candidateAnswerId}`
| Trước (giả định) | Sau (xác nhận thật) |
|---|---|
| `humanHistory` (đây chính là field gây crash) | `humanAssessments` |
| `criterionScores[].criterionCode` | `criterionScores[].criterion` (cả AI lẫn Rubric criteria) |
| `candidateCode` | KHÔNG tồn tại — chỉ có `candidateId` (GUID) + `candidateName`. Đã bỏ khỏi UI. |
| `questionNumber` | KHÔNG tồn tại — heading đổi thành hiển thị tên dạng câu (vd "Bài luận ý kiến (Q8)") thay vì "Câu N". |

**Còn mở:** `humanAssessments` trong ví dụ thật đang rỗng (`[]`) — tên
field đã chắc chắn đúng, nhưng shape từng phần tử bên trong (khi có Rater
đã chấm) vẫn là giả định.

File sửa: `types/rater.ts`, `app/rater/writing-answers/[candidateAnswerId]/page.tsx`.

### Đã tự verify lại toàn bộ
`npm run build` — 0 lỗi (27 route). `npx eslint .` — 0 lỗi/0 warning. Dùng
Playwright nạp **đúng nguyên văn 5 JSON Dragon gửi** (copy-paste, không
sửa) cho cả 5 trang — xác nhận `pageerror: 0` cho tất cả, và chụp ảnh xác
nhận hiển thị đúng số liệu thật (điểm 74, 3 tiêu chí Grammar/Vocabulary/
Opinion & Idea Development, "Không đủ dữ liệu" đúng chỗ n=1 cho Pearson/
Spearman...).

---

## Phần 2 — Đề xuất backend giải quyết PHASE7-ISSUE-10

**Không thể tự giải quyết từ phía Frontend** — đây là giới hạn quyền
(`Authorize`) ở backend .NET, Frontend không có cách nào "vượt qua" một
endpoint yêu cầu Role=ADMIN. Việc tôi có thể làm là viết sẵn patch backend
theo đúng kiến trúc/convention dự án đã dùng xuyên suốt (Controller mỏng →
Application Service interface → Infrastructure Service dùng thẳng
`AppDbContext`, không `Repository<T>`) để Dragon áp dụng trực tiếp — hoặc
đưa cho phiên làm việc backend .NET để ráp vào đúng file thật.

**Vị trí:** `ExamsController` hiện có (không cần tạo Controller mới) —
thêm 1 action mới bên cạnh các action ADMIN hiện có, method riêng cho
Candidate:

```csharp
// Toeic.Application/ExamManagement/Dtos/CandidateExamSummaryDto.cs
namespace Toeic.Application.ExamManagement.Dtos;

public record CandidateExamSummaryDto(
    Guid ExamId,
    string ExamCode,
    string ExamName,
    string? Description,
    DateOnly ExamDate,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status // "SCHEDULED" | "ONGOING"
);
```

```csharp
// Toeic.Application/ExamManagement/IExamService.cs — thêm 1 method vào interface hiện có
Task<List<CandidateExamSummaryDto>> GetAvailableForCandidateAsync(CancellationToken ct = default);
```

```csharp
// Toeic.Infrastructure/ExamManagement/ExamService.cs — thêm implementation
public async Task<List<CandidateExamSummaryDto>> GetAvailableForCandidateAsync(CancellationToken ct = default)
{
    return await _db.Exams
        .Where(e => e.Status == ExamStatus.Scheduled || e.Status == ExamStatus.Ongoing)
        .OrderBy(e => e.StartTime)
        .Select(e => new CandidateExamSummaryDto(
            e.ExamId,
            e.ExamCode,
            e.ExamName,
            e.Description,
            e.ExamDate,
            e.StartTime,
            e.EndTime,
            e.Status.ToString().ToUpperInvariant()))
        .ToListAsync(ct);
}
```

```csharp
// Toeic.Api/Controllers/ExamsController.cs — thêm action mới, KHÔNG đổi
// action ADMIN hiện có. Đặt route "available" TRƯỚC route "{id}" nếu
// ASP.NET routing bị nhầm "available" thành route param id (thường không
// vấn đề với [HttpGet("available")] tường minh, nhưng nêu ra để lưu ý).
[HttpGet("available")]
[Authorize(Roles = "CANDIDATE")]
public async Task<IActionResult> GetAvailableForCandidate(CancellationToken ct)
{
    var result = await _examService.GetAvailableForCandidateAsync(ct);
    return Ok(result);
}
```

**Field JSON output khớp CHÍNH XÁC** với `types/candidate-exam.ts` đã có
sẵn ở Frontend (`examId`, `examCode`, `examName`, `description`,
`examDate`, `startTime`, `endTime`, `status`) — nếu backend áp dụng đúng
patch trên, Frontend **không cần sửa gì thêm**, chỉ cần Dragon xác nhận
route `GET /api/v1/exams/available` trả về 200 với Candidate token.

**Lưu ý:** đây là code minh hoạ theo đúng convention đã thấy ở các
Controller/Service khác của dự án (Clean Architecture, EF Core trực tiếp
qua `AppDbContext`, không `Repository<T>` — theo quyết định đã chốt ở
Phase 0 mục G) — **không phải patch đã test biên dịch được**, vì tôi
không có quyền truy cập source code backend .NET thật trong phiên làm
việc này (chỉ làm Frontend). Dragon cần đưa cho phiên backend .NET để ráp
đúng tên field/namespace thật (vd `_db`/`_examService` có thể tên khác,
`ExamStatus` enum values cần khớp đúng backend thật).
