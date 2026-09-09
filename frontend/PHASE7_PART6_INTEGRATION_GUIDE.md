# PHASE 7 – PART 6: ADMIN — DASHBOARD & EVALUATION — Integration Guide

## 1. Mục tiêu Part này

**Part cuối cùng của Phase 7 (Frontend).** Admin Dashboard overview (số
liệu tổng quan hệ thống + thống kê điểm từng phần) + Writing Error
Analytics (Phase 6 Part 3); trang Evaluation: dataset summary, Metrics
(MAE/RMSE/Pearson/Spearman/Agreement Rate, filter theo `questionType`/
`agreementThreshold`), Report (top case lệch điểm), export CSV/JSON
(Phase 5) — lần đầu tiên Frontend gọi `evaluation-service` (Python) thay vì
chỉ backend .NET.

**Demo cuối Part:** Admin xem tổng quan hệ thống + chạy báo cáo AI vs Human
ngay trên UI, tải CSV.

## 2. Rà soát trước khi code

### PHASE7-ISSUE-16 — Kiến trúc gọi evaluation-service: Route Handler proxy + xác thực tối thiểu
**Problem:** `evaluation-service` (Python/FastAPI, Phase 5) là service độc
lập, base URL khác backend .NET — quyết định kiến trúc PHASE7-ISSUE-03 (đã
chốt từ Phase 0/tracker) yêu cầu Frontend KHÔNG gọi thẳng từ browser, phải
qua Route Handler proxy. Nhưng tài liệu Phase 5 chỉ mô tả
`evaluation-service` tự đăng nhập vào Toeic.Api bằng tài khoản RATER
`eval-service` (để **lấy** dữ liệu) — không nói rõ các endpoint HTTP của
chính `evaluation-service` có đòi hỏi Authorization từ phía gọi (ở đây là
Next.js) hay không.
**Severity:** Medium — không rõ liệu cần thêm cơ chế xác thực giữa Next.js
và evaluation-service hay không.
**Decision:** `app/api/evaluation/[...path]/route.ts` (catch-all) forward
GET request tới `evaluation-service`, yêu cầu tối thiểu request phải có
header `Authorization: Bearer <token>` (chính accessToken ADMIN đang đăng
nhập, dùng lại — không verify chữ ký JWT ở đây, đó là việc của Toeic.Api)
mới cho qua — chặn được request nặc danh trực tiếp vào proxy, dù không
đảm bảo tuyệt đối. Đã test: thiếu header → 401; evaluation-service không
chạy được → 502 JSON có kiểm soát (không crash).
**Alternative:** Không cần proxy, gọi thẳng từ browser tới
`NEXT_PUBLIC_EVALUATION_SERVICE_URL`. Đơn giản hơn nhưng vi phạm quyết
định kiến trúc đã chốt (PHASE7-ISSUE-03) — lộ base URL nội bộ + cần
evaluation-service tự cấu hình CORS cho origin Frontend.
**Reason:** Giữ đúng ranh giới 2 service đã chốt ở Phase 0 mục F/J; nếu
sau này `evaluation-service` thêm auth riêng, chỉ cần sửa hàm `forward()`
trong 1 file.

### PHASE7-ISSUE-17 — Shape DTO của Dashboard/Evaluation là giả định
**Problem:** Cả `GET /admin/dashboard/overview`, `GET /admin/dashboard/
writing-errors`, và 4 endpoint `evaluation-service` chỉ được mô tả bằng
lời trong `phase-tracker.md` (Phase 6 Part 3, Phase 5 Part 1-3), không có
ví dụ JSON. Riêng Phase 5 có mô tả khá chi tiết về Ý NGHĨA field (đặc biệt
việc `null` nghĩa là "không đủ dữ liệu" chứ không phải "tương quan = 0" —
PHASE5 Part2 đã ghi rõ điều này trong tracker), nên độ tin cậy của giả
định cao hơn Part 4 một chút.
**Severity:** Medium.
**Decision:** Field name suy ra từ mô tả + schema đã chốt. Cô lập trong
`types/{admin-dashboard,evaluation}.ts` + `lib/admin/{dashboard,evaluation}-api.ts`.
**Quan trọng nhất đã tuân thủ đúng từ mô tả gốc:** `MetricValue` component
hiển thị **"Không đủ dữ liệu"** khi field là `null`, KHÔNG hiển thị số `0`
— tránh hiểu nhầm "tương quan bằng 0" thành "không có tương quan" khi thực
ra là "chưa đủ mẫu để tính".
**Reason:** Cùng nguyên tắc cô lập giả định đã áp dụng nhất quán từ Part 2.

**Không có issue kiến trúc mới nào khác** — toàn bộ pattern UI (filter
dropdown, card thống kê, bảng dữ liệu) tái sử dụng nguyên vẹn từ Part 2-5.

## 3. Kiến trúc

```
app/admin/dashboard/       Overview (3 count card + 3 score-stats card) + Writing Error Analytics
app/admin/evaluation/      Dataset summary + Metrics (filter) + Report (filter + export)
app/api/evaluation/[...path]/route.ts   Proxy tới evaluation-service (PHASE7-ISSUE-16)

lib/admin/
├── dashboard-api.ts     Gọi THẲNG Toeic.Api (giống mọi module Admin khác)
└── evaluation-api.ts    Gọi qua proxy /api/evaluation/* (KHÁC — service riêng biệt)

types/
├── admin-dashboard.ts   AdminDashboardOverview, WritingErrorAnalytics
└── evaluation.ts        DatasetSummary, MetricsResult, ReportResult
```

### Export CSV/JSON — vì sao không dùng `<a href>` thuần
Route export cần đính kèm `Authorization` header (đúng cơ chế xác thực đã
chốt ở PHASE7-ISSUE-16), mà thẻ `<a>` không thể tự thêm header khi điều
hướng trình duyệt. `evaluationApi.downloadExport()` tự `fetch()` kèm
header, nhận `blob`, đọc `Content-Disposition` để lấy tên file, rồi tạo
`<a>` ẩn với `URL.createObjectURL()` để kích hoạt tải xuống — pattern
chuẩn cho tải file có xác thực.

## 4. Đã tự verify trong sandbox

| Kiểm tra | Kết quả |
|---|---|
| `npm run build` (production) | ✅ Thành công, 0 lỗi (33 route, gồm cả `/api/evaluation/[...path]`) |
| `npx eslint .` | ✅ 0 lỗi, 0 warning ngay lần đầu |
| RBAC: `/admin/dashboard`, `/admin/evaluation` yêu cầu đúng ADMIN | ✅ curl test |
| Proxy `/api/evaluation/*`: thiếu `Authorization` → 401 | ✅ curl test |
| Proxy `/api/evaluation/*`: có header nhưng evaluation-service không chạy → 502 JSON có kiểm soát | ✅ curl test |
| Dashboard: card số liệu + thống kê điểm 3 phần + cảnh báo `unparseableCount` | ✅ Chụp ảnh qua Playwright + mock response |
| Evaluation: dataset + metrics (kể cả hiển thị "Không đủ dữ liệu" khi `n` nhỏ) + report + export | ✅ |
| Export lỗi (evaluation-service không phản hồi) → toast lỗi, không crash trang | ✅ Verify bằng Playwright, click nút thật |

**Lưu ý:** dùng Playwright chặn response (`page.route`) trả JSON giả lập
đúng theo shape đã định nghĩa — xác nhận ĐÚNG hành vi Frontend, KHÔNG xác
nhận shape đó khớp backend/evaluation-service thật (xem PHASE7-ISSUE-17).

## 5. Chưa làm ở Part này (đúng phạm vi)

- Filter `byQuestionType` cho Metrics hiển thị dạng bảng riêng (hiện chỉ
  hiển thị `byCriterion` — `byQuestionType` có trong response nhưng UI
  chưa render nếu rỗng ở dataset mẫu; code đã sẵn sàng lặp qua nếu backend
  trả dữ liệu, không cần sửa thêm).
- Export `level=criterion` (hiện UI chỉ có nút export `level=overall` —
  API đã hỗ trợ tham số, có thể thêm nút thứ 2 nhanh nếu Dragon cần).
- Biểu đồ hoá Metrics (hiện hiển thị dạng số/card, không vẽ chart) — đủ
  dùng cho MVP, tránh over-engineering khi số liệu chủ yếu là để đối
  chiếu chính xác từng con số hơn là xu hướng trực quan.

## 6. Việc BẮT BUỘC Dragon cần làm trước khi coi Part 6 (và toàn bộ Phase 7) là "Done"

1. **Chạy `evaluation-service` (Phase 5) thật** cùng lúc với backend .NET
   và frontend, set `EVALUATION_SERVICE_URL` trong `.env.local` trỏ đúng
   cổng — đây là lần đầu tiên cả 3 tầng (Next.js + .NET + Python) chạy
   cùng lúc.
2. **Xác nhận evaluation-service có yêu cầu Authorization riêng hay
   không** (PHASE7-ISSUE-16) — nếu có, bổ sung header tương ứng trong
   `forward()` (`app/api/evaluation/[...path]/route.ts`).
3. **Đối chiếu shape thật của cả 4 endpoint evaluation-service** — ưu tiên
   `/metrics` (đặc biệt cách phân biệt `null` vs `0`) và `/report/export`
   (đặc biệt tên header `Content-Disposition` để lấy đúng filename).
4. **Đối chiếu shape `GET /admin/dashboard/overview` và `/writing-errors`**
   — đặc biệt đối chiếu field name thật trong `ErrorsJson` (PHASE6-ISSUE-06
   đã ghi trong tracker — quan trọng nhất phía backend, ảnh hưởng trực
   tiếp tới độ chính xác của `errorType` hiển thị ở đây).
5. **Test luồng đầy đủ:** có ít nhất vài cặp AI/Human đã chấm (từ Part 5)
   → vào `/admin/evaluation` → xác nhận dataset/metrics/report khớp số
   liệu thật → xuất CSV → mở bằng Excel xác nhận encoding UTF-8 đúng (dấu
   tiếng Việt trong `candidateName`) — đã lưu ý trong
   `PHASE5_PART3_INTEGRATION_GUIDE.md` gốc.

---

## Phase 7 (Frontend) — đã hoàn tất cả 6/6 Part

Với Part 6, toàn bộ Frontend Next.js đã đủ UI cho cả 3 vai trò
(Admin/Rater/Candidate) theo đúng yêu cầu "hoàn chỉnh, không phải bản tối
thiểu" của Dragon. Việc còn lại trước khi coi Phase 7 là hoàn tất thật sự:

- Xử lý toàn bộ mục "Việc BẮT BUỘC" của cả 6 Part (danh sách đầy đủ nằm
  rải trong 6 file `PHASE7_PARTn_INTEGRATION_GUIDE.md`) — quan trọng nhất
  vẫn là **PHASE7-ISSUE-10** (endpoint danh sách Exam cho Candidate, Part
  3) vì đây là điểm chặn duy nhất ảnh hưởng khả năng demo end-to-end thật
  của luồng Candidate.
- Chạy toàn bộ hệ thống cùng lúc lần đầu: `frontend` + `Toeic.Api` +
  `evaluation-service`, đi qua đúng kịch bản demo end-to-end mục 30 prompt
  gốc (Admin tạo đề → Candidate thi → Rater chấm → Admin xem Evaluation).
- Sau khi xác nhận ổn định, chuyển sang **Phase 8 — DevOps** (Docker
  Compose đầy đủ cả 4 service, CI/CD) — xem `PHASE0_Project_Review_Final_
  Architecture.md` mục M/N.
