# PHASE 7 – PART 4: CANDIDATE — RESULT / HISTORY / ANALYTICS — Integration Guide

## 1. Mục tiêu Part này

Result page sau khi nộp bài (Listening/Reading/Writing hiển thị **riêng
biệt**, không gộp Total Score — đúng PHASE3-ISSUE-07/PHASE6-ISSUE-02),
trạng thái Writing Pending/Scored/NotAnswered tự đồng bộ khi background job
chấm xong; History list nhiều phiên thi; Performance Analytics (điểm mạnh/
yếu theo QuestionType/Criterion + biểu đồ xu hướng điểm qua thời gian).

**Demo cuối Part:** Sau khi Writing được chấm xong (background job Phase 3
Part 4) → xem Result đầy đủ 3 điểm + feedback; xem History nhiều phiên +
biểu đồ xu hướng.

## 2. Rà soát trước khi code

### PHASE7-ISSUE-14 — Toàn bộ shape DTO của Part này là giả định (Phase 2/3 Part 4/6 chưa từng lộ source thật cho sandbox)
**Problem:** Khác với Exam/Test (Phase 1 Part 3, có ví dụ `curl` thật) và
kể cả Rubric (Phase 3 Part 1, ít nhất có mô tả trong tracker), 3 nguồn dữ
liệu của Part này —
`GET /exam-sessions/{id}/result` (Phase 2 + Phase 3 Part 4),
`GET /candidates/me/history` (Phase 6 Part 1),
`GET /candidates/me/analytics` (Phase 6 Part 2) —
chỉ được mô tả **bằng lời** trong `phase-tracker.md`, không có ví dụ JSON
hay source code thật nào được giao cho sandbox này ở bất kỳ thời điểm nào.
**Severity:** Medium-High — không chặn việc code (mô tả bằng lời + schema
DB đã chốt ở PHASE0 đủ để suy luận hợp lý), nhưng đây là Part có **mật độ
giả định cao nhất** từ trước đến giờ.
**Recommended Solution:** Thiết kế 3 type (`types/candidate-result.ts`,
`types/candidate-history.ts`, `types/candidate-analytics.ts`) theo suy luận
hợp lý nhất từ mô tả + schema, ghi rõ từng giả định bằng comment tại chỗ.
Cô lập hoàn toàn qua 3 file API tương ứng (`lib/candidate/{result,history,
analytics}-api.ts`) — sai ở đâu chỉ sửa đúng 1 file.
**Decision:** Tiến hành với giả định, KHÔNG chặn tiến độ — đúng tinh thần
Demo First đã áp dụng xuyên suốt dự án.
**Reason:** UI (biểu đồ, bảng, card điểm) được viết độc lập với response
shape cụ thể qua lớp `types/`+`lib/candidate/*-api.ts`, rủi ro sửa sai lan
rộng gần như bằng 0.

**Giả định cụ thể quan trọng nhất (đã ghi trong code comment):**
- Trạng thái Writing (`Pending`/`Scored`/`NotAnswered` — tên có bằng chứng
  trực tiếp từ tracker) được thiết kế **per-question** thay vì tổng hợp cả
  section — an toàn hơn vì Frontend tự suy ra trạng thái tổng hợp từ danh
  sách per-question (`writingOverallStatus()` trong `types/candidate-result.ts`),
  không mất thông tin nếu giả định ngược lại đúng.
- History/Analytics dùng `PagedResult<T>` (History) đã định nghĩa từ Part 2
  (PHASE7-ISSUE-08) — không tạo giả định mới.
- Analytics nhóm theo `(Section, QuestionType)` nhưng UI ưu tiên hiển thị
  theo Section (đúng lưu ý PHASE6-ISSUE-05 đã ghi trong tracker: hiện tại
  toàn bộ câu Listening/Reading đều `MULTIPLE_CHOICE` nên 2 cách nhóm tương
  đương).

## 3. Kiến trúc

```
app/candidate/results/[sessionId]/   Result — poll lại khi Writing còn PENDING
app/candidate/history/               History — bảng phân trang, link sang Result
app/candidate/analytics/             Analytics — biểu đồ xu hướng (recharts) + accuracy bar + criteria card

lib/candidate/
├── result-api.ts       GET /exam-sessions/{id}/result
├── history-api.ts       GET /candidates/me/history
└── analytics-api.ts     GET /candidates/me/analytics

components/candidate/
├── section-score-card.tsx    Card điểm Listening/Reading (KHÔNG có Total)
├── writing-status-badge.tsx  Badge Pending (nhấp nháy)/Scored/NotAnswered
├── accuracy-bar.tsx           Thanh % đúng theo Section/QuestionType
└── score-trend-chart.tsx      Line chart 3 đường (recharts) — thêm mới dependency
```

### Result page tự đồng bộ khi Writing đang chấm
`useQuery` với `refetchInterval` **động**: nếu `writingOverallStatus(data.writing)
=== "PENDING"` thì poll lại mỗi 5s, ngược lại dừng hẳn (`false`) — tránh
poll vô hạn sau khi đã có điểm. Đã verify bằng Playwright: gọi API lần 1
thấy `PENDING`, đợi qua 1 chu kỳ poll thấy tự chuyển `SCORED` **không cần
F5** (xem mục 5).

### Biểu đồ xu hướng điểm — thêm dependency mới
Cài `recharts` (không có trong Part 1-3) — dùng cho `ScoreTrendChart` duy
nhất; các biểu đồ khác (accuracy) cố ý dùng thanh progress tự vẽ bằng
token màu riêng của dự án thay vì thêm chart cho việc đơn giản, tránh
over-engineering.

## 4. Đã tự verify trong sandbox

| Kiểm tra | Kết quả |
|---|---|
| `npm run build` (production) | ✅ Thành công, 0 lỗi (23 route) |
| `npx eslint .` | ✅ 0 lỗi, 0 warning (ngay lần đầu, không phát sinh issue lint mới) |
| RBAC: `/candidate/history`, `/candidate/analytics`, `/candidate/results/*` | ✅ curl test, không hồi quy |
| Result page tự chuyển Pending → Scored qua polling, không cần F5 | ✅ Verify bằng route interception, đếm số lần gọi API (≥2) |
| History: hiển thị "Đang chấm" khi `writingScore = null` | ✅ |
| Analytics: biểu đồ xu hướng vẽ đúng 3 đường, đứt đoạn đúng chỗ `null` (`connectNulls` tắt có chủ đích cho đoạn thiếu dữ liệu Writing) | ✅ |
| Analytics: thanh accuracy + card tiêu chí Writing | ✅ |

**Lưu ý:** toàn bộ kiểm tra trên dùng Playwright chặn response
(`page.route`) trả JSON giả lập đúng theo shape đã định nghĩa — xác nhận
ĐÚNG hành vi Frontend (bao gồm cả polling), nhưng KHÔNG xác nhận shape đó
khớp backend thật (xem PHASE7-ISSUE-14).

## 5. Chưa làm ở Part này (đúng phạm vi, để Part sau)

- Export/in kết quả (PDF) — không có trong yêu cầu gốc.
- Filter/search trong History (theo khoảng ngày, theo Exam) — bảng đơn
  giản với phân trang là đủ cho MVU, tránh over-engineering.
- Rater/Admin Dashboard, Evaluation UI — Part 5/6.

## 6. Việc BẮT BUỘC Dragon cần làm trước khi coi Part 4 là "Done"

1. **Chạy `npm run dev` với backend .NET Phase 2, Phase 3 Part 4, Phase 6
   Part 1-2 thật đang chạy song song** — Part đầu tiên phụ thuộc cả 3 nguồn
   dữ liệu đó cùng lúc.
2. **Đối chiếu shape thật của `GET /exam-sessions/{id}/result`** — quan
   trọng nhất: xác nhận trạng thái Writing là per-question hay tổng hợp
   section, và tên field điểm/feedback/criterionScores thật.
3. **Đối chiếu shape `GET /candidates/me/history`** — đặc biệt field
   `writingScore` có trả `null` khi chưa chấm xong hay không.
4. **Đối chiếu shape `GET /candidates/me/analytics`** — đặc biệt cấu trúc
   `scoreTrend` (mỗi điểm có đủ 3 field điểm hay tách endpoint riêng cho
   Writing).
5. **Test luồng đầy đủ:** nộp 1 bài có câu Writing → vào Result ngay sau
   khi nộp → thấy Pending → đợi background job chấm xong → xác nhận UI tự
   cập nhật không cần F5 → vào History thấy điểm mới → vào Analytics thấy
   xu hướng điểm cập nhật.
