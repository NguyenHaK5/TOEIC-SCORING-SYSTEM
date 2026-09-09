# PHASE 7 — HOTFIX (06/09/2026): Lỗi phát hiện khi chạy thật lần đầu với backend .NET

> Dragon đã chạy `frontend` + `Toeic.Api` thật lần đầu tiên và gửi lại toàn
> bộ log + 10 ảnh chụp lỗi. File này ghi lại nguyên nhân + cách sửa theo
> đúng format issue-first đã dùng xuyên suốt dự án.

## PHASE7-ISSUE-18 — Sai tên field khóa chính cho Exam/Test/RubricSet/Rubric/Question/QuestionOption (Critical, ĐÃ SỬA)

**Problem:** Toàn bộ type Admin (`Exam`, `Test`, `RubricSet`, `Rubric`,
`Question`, `QuestionOption`) được định nghĩa với khóa chính là field `id`
chung chung. Khi chạy với backend thật, `exam.id`/`test.id`/`rubricSet.id`
đều là `undefined` → gây:
- Warning "Each child in a list should have a unique key prop" (nhiều
  dòng cùng key `undefined`).
- Điều hướng sai: `/admin/exams/undefined`, `/admin/tests/undefined`,
  `/admin/rubrics/undefined` (bấm "Xem chi tiết" từ danh sách).
- Trang chi tiết kẹt ở "Đang tải…" vĩnh viễn (gọi API với id="undefined").

**Root Cause:** Sandbox chưa từng thấy response JSON thật của các entity
này (README.md chỉ có ví dụ **request** body, không có response). Trong
khi đó, các entity **đã có bằng chứng trực tiếp** (`ExamSession`,
`CandidateAnswer`, `HumanAssessment`...) đều dùng convention nhất quán
`{Entity}Id` (`examSessionId`, `candidateAnswerId`, `humanAssessmentId`).
Tôi đã áp dụng nhầm giả định "id chung chung" cho nhóm Admin thay vì tiếp
tục đúng convention đã thấy — đây là lỗi giả định của tôi, không phải lỗi
ngẫu nhiên của backend.

**Impact:** Chặn hoàn toàn thao tác "xem chi tiết" ở cả 3 module Admin
chính (Exam/Test/Rubric) — nghiêm trọng nhất trong toàn bộ Phase 7 tính
đến nay.

**Fix:** Đổi khóa chính đúng theo convention `{Entity}Id` đã được xác nhận
qua chính lần chạy thật này (`exam.id` từng ra `undefined` → khẳng định
chắc chắn field đó không tồn tại, tên đúng suy ra từ pattern nhất quán):

| Type | Trước | Sau |
|---|---|---|
| `Exam` | `id` | `examId` |
| `Test` | `id` | `testId` |
| `RubricSet` | `id` | `rubricSetId` |
| `Rubric` | `id` | `rubricId` |
| `Question` | `id` | `questionId` |
| `QuestionOption` | `id` | `questionOptionId` |
| `CandidateExamSummary` | `id` | `examId` |
| `CandidateVisibleOption` (Exam Room) | `id` | `questionOptionId` |

Đã sửa toàn bộ nơi sử dụng (list page `key`/`href`, detail page params,
mutation calls, Select option value, radio input trong Exam Room) — rà
soát bằng `grep` toàn repo, xác nhận không còn field `id` chung chung nào
sót lại cho các entity này. `npm run build` + `npx eslint .` đều sạch sau
khi sửa.

**Còn lại cần Dragon xác nhận:** đây là sửa dựa trên **suy luận có bằng
chứng mạnh** (convention nhất quán + bằng chứng `undefined` thật), không
phải đã thấy JSON response thật. Nếu vẫn còn `/undefined` sau khi cập nhật
code này, gửi lại y nguyên 1 dòng JSON response thật của `GET /exams` (hoặc
mở Network tab, copy response) để tôi đối chiếu chính xác.

---

## Các lỗi shape DTO khác (Medium — ĐÃ THÊM PHÒNG THỦ, chưa sửa được chính xác)

5 lỗi còn lại đều cùng một nguyên nhân: **response thật thiếu field mà
Frontend giả định luôn có** (đúng những rủi ro đã cảnh báo trước ở
PHASE7-ISSUE-14/15/17 — các Part 4/5/6 chưa từng thấy JSON thật). Tôi
**không thể sửa chính xác nếu không thấy JSON thật**, nên đã làm 2 việc:
(1) thêm fallback để trang không crash nữa, hiển thị trạng thái rỗng/cảnh
báo thay vì màn hình lỗi đỏ; (2) đánh dấu rõ trong code + liệt kê dưới đây
để Dragon gửi JSON thật, tôi sẽ sửa đúng field ngay khi có.

| # | Trang | Field bị thiếu (theo log) | Đã phòng thủ tại |
|---|---|---|---|
| 1 | `/admin/dashboard` | `errors.items` (danh sách lỗi Writing) | `app/admin/dashboard/page.tsx` — fallback `?? []` |
| 2 | `/admin/evaluation` | `metrics.overall.mae` (và có thể các field số khác) | `components/admin/metric-value.tsx` — nhận cả `undefined` lẫn `null` |
| 3 | `/candidate/results/{id}` | `result.listening`/`reading`/`writing` | `app/candidate/results/[sessionId]/page.tsx` — fallback rỗng + cảnh báo đỏ |
| 4 | `/candidate/analytics` | `data.listeningReadingAccuracy` | `app/candidate/analytics/page.tsx` — fallback `?? []` |
| 5 | `/rater/writing-answers/{id}` | `detail.humanHistory` | `app/rater/writing-answers/[candidateAnswerId]/page.tsx` — fallback `?? []` (và `ai.criterionScores`, `activeRubric.criteria` phòng ngừa thêm) |

**Đã tự verify:** dùng Playwright giả lập ĐÚNG các response thiếu field y
hệt log thật của Dragon (vd `writing-errors` chỉ trả `{unparseableCount:0}`
không có `items`) — xác nhận cả 6 trang không còn crash, hiển thị trạng
thái rỗng/cảnh báo hợp lý thay vì màn hình lỗi đỏ.

### Việc Dragon cần làm để tôi sửa đúng (không chỉ phòng thủ)

Gửi lại **nguyên văn JSON response thật** (mở DevTools → Network → click
vào request → tab Response → copy) cho 5 endpoint sau, mỗi cái chỉ cần 1
ví dụ thật:
1. `GET /admin/dashboard/writing-errors`
2. `GET /api/v1/evaluations/metrics` (endpoint evaluation-service thật, hoặc xem qua proxy `/api/evaluation/metrics`)
3. `GET /exam-sessions/{id}/result`
4. `GET /candidates/me/analytics`
5. `GET /rater/writing-answers/{candidateAnswerId}`

Có JSON thật, tôi sửa đúng tên field trong `types/*.ts` — mỗi endpoint chỉ
cần sửa đúng 1 file type + có thể 1 dòng ở component hiển thị, không phải
viết lại trang.

## Ghi chú khác từ log (không phải lỗi code)

- **`evaluation-service` báo `ECONNREFUSED` nhiều lần rồi sau đó `200 OK`**:
  đúng như dự kiến — Dragon khởi động `evaluation-service` sau khi đã mở
  trang `/admin/evaluation`. Không phải lỗi, proxy xử lý đúng (502 có kiểm
  soát trong lúc chưa chạy).
- **`/candidate/exams` báo "Không tải được danh sách kỳ thi"**: đây chính
  là **PHASE7-ISSUE-10** đã cảnh báo từ Part 3 — endpoint
  `GET /exams/available` nhiều khả năng chưa tồn tại ở backend. Cần Dragon
  xác nhận/bổ sung endpoint này — xem `PHASE7_PART3_INTEGRATION_GUIDE.md`
  mục 6. Đây là điểm chặn quan trọng nhất còn lại của toàn bộ Phase 7.
