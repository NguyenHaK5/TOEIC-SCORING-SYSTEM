# PHASE 7 – PART 2: ADMIN — CONTENT MANAGEMENT — Integration Guide

## 1. Mục tiêu Part này

CRUD đầy đủ cho Exam/Test/ExamTest (gán/gỡ)/Question/QuestionOption (mục 15
prompt gốc — "Admin"), CRUD RubricSet/Rubric (Phase 3 Part 1), state
machine UI cho Exam (DRAFT→SCHEDULED→ONGOING→COMPLETED/CANCELLED) và Test
(DRAFT→PUBLISHED→ARCHIVED), form nhập URL ảnh/audio (PHASE7-ISSUE-04).

**Demo cuối Part:** Admin tạo Test → thêm Question/Option → Publish → tạo
Exam → gán Test → chuyển SCHEDULED, hoàn toàn qua UI (không cần Swagger).

## 2. Rà soát trước khi code

### PHASE7-ISSUE-07 — Rubric Management API shape là giả định lớn nhất Part này
**Problem:** Khác với Exam/Test (README.md gốc có ví dụ `curl` thật với
field name cụ thể — `examCode`, `testCode`, `listeningDurationSeconds`...),
Rubric Management (Phase 3 Part 1) chỉ được nhắc tên trong `phase-tracker.md`
("Endpoint: `/rubric-sets/**`, `/rubrics/{id}`") mà không có nội dung
`PHASE3_PART1_INTEGRATION_GUIDE.md` thật để đối chiếu — sandbox này chưa
từng thấy file đó.
**Severity:** Medium — không chặn code (schema DB đã chốt ở PHASE0 mục
E.2.10/E.2.11 nên field name Rubric/RubricSet tương đối chắc chắn), nhưng
**endpoint path và action state-machine** (`/activate`, `/archive`, path
thêm Rubric con) là suy luận theo REST convention thông thường, chưa có
bằng chứng trực tiếp.
**Recommended Solution:** Code toàn bộ `lib/admin/rubrics-api.ts` theo suy
luận hợp lý nhất (liệt kê ở mục 4), đánh dấu rõ trong code comment. Nếu
path thật khác, chỉ cần sửa 1 file này.
**Decision:** Tiến hành với giả định, KHÔNG chặn tiến độ Part 2 — đúng tinh
thần Demo First, sửa 1 file khi có xác nhận từ Dragon rẻ hơn nhiều so với
việc dừng lại chờ.
**Reason:** Toàn bộ UI (list/detail/form) được viết độc lập với đường dẫn
API cụ thể — tách qua `lib/admin/rubrics-api.ts` — nên rủi ro sửa sai lan
rộng sang nhiều file gần như bằng 0.

### PHASE7-ISSUE-08 — `PagedResult<T>` shape vẫn là giả định (kế thừa từ Phase 6)
**Problem:** `phase-tracker.md` (mục Phase 6 "Việc tiếp theo") đã ghi nhận
đây là giả định mở từ trước, chưa được Dragon xác nhận. Part 2 là Part đầu
tiên của Frontend thực sự cần phân trang (danh sách Exam/Test/RubricSet).
**Severity:** Medium.
**Decision:** Định nghĩa 1 type `PagedResult<T>` DUY NHẤT tại
`types/pagination.ts` (`items`/`totalCount`/`page`/`pageSize`/`totalPages`
— convention .NET phổ biến nhất), toàn bộ `lib/admin/*-api.ts` dùng chung
type này. Nếu sai, sửa đúng 1 chỗ.
**Reason:** Tập trung điểm sửa, tránh rải rác giả định ở nhiều file.

### PHASE7-ISSUE-09 — React 19 lint rule `set-state-in-effect`
**Problem:** Pattern quen thuộc "fetch qua React Query rồi `useEffect` đồng
bộ vào `useState` để có form editable" bị ESLint (React Compiler rules,
bật sẵn trong `eslint-config-next` ở Next.js 16) coi là lỗi — không chỉ
warning.
**Severity:** Low (thuần vấn đề code quality, không phải bug chức năng).
**Decision:** Tách phần form chỉnh sửa (Exam/Test/RubricSet/Question) ra
component con nhận entity đã load làm prop, khởi tạo `useState` trực tiếp
từ prop đó (không qua effect) — component con chỉ mount sau khi component
cha đã gate `isLoading`, nên dữ liệu luôn có sẵn tại thời điểm khởi tạo
state. Thêm `key={entity.id}` ở nơi gọi để đảm bảo remount đúng nếu id đổi
mà không unmount/mount tự nhiên.
**Reason:** Đây là pattern React chuẩn được chính tài liệu React khuyến
nghị (tránh "adjusting state when a prop changes" bằng effect), đồng thời
giữ `npx eslint .` sạch 100% xuyên suốt dự án như các Part trước.

## 3. Kiến trúc

```
app/admin/
├── exams/            list, new, [examId] (edit + state machine + assign/unassign Test)
├── tests/            list, new, [testId] (edit + publish/archive + question list)
│   └── [testId]/questions/   new, [questionId] (edit + QuestionOption CRUD)
└── rubrics/          list, new, [rubricSetId] (edit + activate/archive + Rubric criteria)

lib/admin/
├── exams-api.ts       CRUD + changeStatus + assignTest/unassignTest
├── tests-api.ts       CRUD + publish + archive
├── questions-api.ts   CRUD Question + CRUD QuestionOption
├── rubrics-api.ts     CRUD RubricSet + activate/archive + CRUD Rubric  (PHASE7-ISSUE-07)
└── query.ts           buildQuery() — querystring helper dùng chung

types/
├── pagination.ts   PagedResult<T>                              (PHASE7-ISSUE-08)
├── exam.ts         Exam, ExamStatus + state machine map
├── test.ts         Test, TestStatus, PublishValidationError
├── question.ts     Question, QuestionOption, Section/Type enum + label tiếng Việt
└── rubric.ts        RubricSet, Rubric                            (PHASE7-ISSUE-07)
```

Mọi gọi API nghiệp vụ đều qua `apiClient` đã dựng ở Part 1 (Bearer token từ
bộ nhớ, tự refresh-và-retry khi 401) — không có Route Handler BFF mới nào
ở Part này (chỉ Auth mới cần BFF, theo PHASE7-ISSUE-01).

State server (danh sách, chi tiết) dùng TanStack Query — thêm mới ở Part
này (`components/providers/query-provider.tsx`, bọc trong
`app/layout.tsx`). Toast feedback dùng `sonner` (thêm `<Toaster/>` trong
root layout).

## 4. Endpoint đã dùng (✅ = có bằng chứng trực tiếp từ README.md gốc, ⚠️ = giả định)

| Method | Path | Ghi chú |
|---|---|---|
| ✅ GET/POST | `/exams`, `/exams/{id}` | |
| ✅ PUT/DELETE | `/exams/{id}` | chỉ khi DRAFT |
| ✅ POST | `/exams/{id}/status` | body `{status}` |
| ✅ POST/DELETE | `/exams/{id}/tests`, `/exams/{id}/tests/{testId}` | |
| ✅ GET/POST | `/tests`, `/tests/{id}` | |
| ✅ POST | `/tests/{id}/publish`, `/tests/{id}/archive` | |
| ⚠️ GET/POST | `/tests/{id}/questions` | field name suy từ PHASE0 mục E.2.6 |
| ⚠️ GET/PUT/DELETE | `/questions/{id}` | |
| ⚠️ POST | `/questions/{id}/options` | |
| ⚠️ PUT/DELETE | `/question-options/{id}` | |
| ⚠️ GET/POST | `/rubric-sets`, `/rubric-sets/{id}` | PHASE7-ISSUE-07 |
| ⚠️ PUT/DELETE | `/rubric-sets/{id}` | |
| ⚠️ POST | `/rubric-sets/{id}/activate`, `/archive` | |
| ⚠️ POST | `/rubric-sets/{id}/rubrics` | |
| ⚠️ PUT/DELETE | `/rubrics/{id}` | |

Danh sách ✅ khớp `curl` thật trong README.md gốc — không cần đối chiếu
lại. Danh sách ⚠️ cần Dragon xác nhận khi chạy với backend thật; sai ở đâu
chỉ sửa đúng file `lib/admin/*-api.ts` tương ứng.

## 5. Đã tự verify trong sandbox

| Kiểm tra | Kết quả |
|---|---|
| `npm run build` (production) | ✅ Thành công, 0 lỗi |
| `npx eslint .` | ✅ 0 lỗi, 0 warning (sau khi xử lý PHASE7-ISSUE-09) |
| RBAC vẫn đúng sau khi thêm ~15 route mới | ✅ curl test lại toàn bộ kịch bản Part 1, không hồi quy |
| Toàn bộ route mới render 200, không SSR crash khi backend .NET không chạy | ✅ (test bằng cookie giả `session_role=ADMIN`) |
| Giao diện: Admin home, Exams list, Exam new, Test new, Rubric new | ✅ Đã chụp ảnh |
| Chọn dropdown (Select), checkbox (QuestionOption) | ✅ Render đúng qua Playwright |

**Lưu ý khi tự test bằng cookie giả:** `AuthProvider` tự gọi
`/api/auth/refresh` khi mount; nếu `refresh_token` là giá trị giả (không
phải token thật) và không có backend .NET đang chạy, route refresh sẽ xoá
cookie ngay lập tức (đúng hành vi bảo mật đã thiết kế ở Part 1 — token
không hợp lệ/không refresh được thì đăng xuất). Điều này KHÔNG phải lỗi —
chỉ ảnh hưởng khi test thủ công không có backend thật; với backend thật
chạy đúng, luồng refresh sẽ thành công bình thường.

## 6. Việc BẮT BUỘC Dragon cần làm trước khi coi Part 2 là "Done"

1. **Chạy `npm run dev` với backend .NET Phase 1 Part 3 (Exam CRUD) +
   Phase 3 Part 1 (Rubric) thật đang chạy song song.** Đây là Part đầu
   tiên phụ thuộc nhiều endpoint chưa từng được test qua Frontend.
2. **Đối chiếu toàn bộ endpoint đánh dấu ⚠️ ở mục 4** — ưu tiên Rubric
   Management trước (rủi ro cao nhất, PHASE7-ISSUE-07).
3. **Xác nhận shape `PagedResult<T>`** (`types/pagination.ts`) khớp response
   thật của `GET /exams`, `/tests`, `/rubric-sets`.
4. **Xác nhận response chi tiết `GET /exams/{id}`** có kèm danh sách Test
   đã gán hay không (field `assignedTests` trong `types/exam.ts` là giả
   định) — nếu không, cần thêm cách khác để hiển thị (vd tự lọc từ danh
   sách ExamTests nếu có endpoint riêng).
5. **Xác nhận shape lỗi khi Publish Test thất bại** (`PublishTestResult` /
   `PublishValidationError` trong `types/test.ts`) — hiện code xử lý cả 2
   khả năng (200 kèm `success:false` HOẶC 400 kèm `{errors:[...]}`), nhưng
   chưa thấy ví dụ thật.
6. **Test luồng đầy đủ:** tạo Test → thêm đủ Question (kể cả Listening cần
   AudioUrl, Picture Description cần ImageUrl) → Publish (xem lỗi validation
   nếu thiếu) → tạo Exam → gán Test → chuyển SCHEDULED.

## 7. Chưa làm ở Part này (đúng phạm vi, để Part sau)

- Upload file ảnh/audio thật — hiện dùng input URL (PHASE7-ISSUE-04, quyết
  định tạm thời đã chốt ở Phase 0).
- Kéo-thả sắp xếp lại `QuestionNumber`/Option — dùng nhập số thủ công,
  đủ dùng cho MVP, tránh over-engineering.
- Bulk import câu hỏi (vd từ file Excel/CSV) — không có trong yêu cầu gốc.
- Candidate Exam Room, Rater review, Dashboard/Evaluation UI — Part 3/5/6.
