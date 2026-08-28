# TOEIC Scoring Support System — Phase Tracker

> File này dùng để theo dõi tiến độ dự án qua nhiều đoạn chat.
> Mỗi khi hoàn thành 1 phần, cập nhật lại file này rồi upload đè lên
> file cũ trong Project Context (Context → xóa file cũ → + thêm file mới).

T�i liệu tham chiếu (đã có trong Context):
- `Prompt xây dựng toàn bộ dự án TOEIC Scoring Support System.md` — quy tắc & yêu cầu gốc
- `PHASE0_Project_Review_Final_Architecture.md` — kiến trúc cuối cùng đã chốt
- `Project.docx` — requirement nghiệp vụ

---

## Trạng thái tổng quan

| Phase | Tên | Trạng thái | Ghi chú |
|---|---|---|---|
| 0 | Project Review & Final Architecture | ✅ Xong | Xem PHASE0_Project_Review_Final_Architecture.md |
| 1 | MVP Core | ✅ Xong (4/4 part) | Part 1: DB+EF Core. Part 2: Auth/RBAC. Part 3: Exam CRUD. Part 4: ExamSession+CandidateAnswer. Backend đủ để demo end-to-end "Candidate login → chọn Exam → làm bài → submit" (chưa có Frontend Next.js — xem "Việc tiếp theo"). |
| 2 | Objective Scoring (Listening/Reading) | ⬜ | |
| 3 | Writing AI Scoring | ⬜ | |
| 4 | Human Rater | ⬜ | |
| 5 | AI vs Human Evaluation | ⬜ | |
| 6 | Dashboard | ⬜ | |
| 7 | DevOps | ⬜ | |

---

## Chi tiết Phase đang làm

### Phase: 1 — MVP Core — Part 4: ExamSession + CandidateAnswer
**Trạng thái:** xong — **Phase 1 hoàn tất (backend)**

**Đã quyết định:**
- Bỏ qua trạng thái `NOT_STARTED` trong vận hành thực tế: gọi `POST /exam-sessions` là bắt đầu làm bài
  ngay lập tức (tạo phiên với `Status=IN_PROGRESS`), khớp UX "vào phòng thi là bắt đầu tính giờ".
  `NOT_STARTED` vẫn giữ trong enum theo đúng schema Phase 0 (dự phòng nếu sau này cần bước "vào phòng
  chờ" trước khi bấm bắt đầu).
- **Resume-idempotent**: nếu Candidate gọi lại `POST /exam-sessions` cho cùng Exam trong khi đã có
  phiên `IN_PROGRESS`, trả về phiên đó thay vì lỗi hoặc tạo phiên mới — tự nhiên chống được lỗi double-
  submit từ Frontend (double click, refresh).
- **Chọn Test form ngẫu nhiên**: khi bắt đầu phiên, hệ thống tự chọn 1 Test trong số các Test đang
  `IsActive` của Exam (không cho Candidate tự chọn) — đúng tinh thần chống gian lận nhiều form
  (FORM-A/FORM-B) đã quyết định ở ISSUE-01, Phase 0.
- **`ExpiresAt` chốt đúng 1 lần** tại thời điểm tạo phiên = `StartedAt + (ListeningDurationSeconds +
  ReadingDurationSeconds + WritingDurationSeconds)` của Test đã chọn (ISSUE-07) — không bao giờ tính lại
  từ `Test` sau đó, kể cả khi Admin sửa duration của Test khác.
- **Auto-submit kiểu "lazy"**: mọi action chạm tới phiên thi (`GetById`, `GetQuestions`, `SaveAnswer`,
  `Submit`) đều kiểm tra `DateTime.UtcNow >= ExpiresAt` trước; nếu đã hết giờ và còn `IN_PROGRESS` thì tự
  chuyển `Status=SUBMITTED` với `SubmittedAt = ExpiresAt` (không phải "now", để phản ánh đúng thời điểm
  hết hạn). **Hạn chế đã biết**: đây là kiểm tra lazy (chỉ chạy khi có request), MVP CHƯA có background
  job chủ động quét toàn bộ phiên hết giờ — nếu Candidate đóng tab và không ai gọi lại API, `Status`
  trong DB có thể vẫn hiển thị `IN_PROGRESS` cho tới lần truy cập tiếp theo (kể cả từ Rater/Admin dashboard
  ở phase sau). Có thể bổ sung `IHostedService` định kỳ quét ở Phase 6 (Dashboard) nếu cần Status luôn
  chính xác real-time.
- **Tách "ghi nhận câu trả lời" khỏi "chấm điểm"**: `SaveAnswerAsync` KHÔNG tính `IsCorrect`/`Score` cho
  câu trắc nghiệm dù đã có đủ Answer Key để tính ngay — cố ý để trống, vì việc chấm điểm là trách nhiệm
  của **Objective Scoring Engine (Phase 2)**, giữ đúng ranh giới module đã vạch ở Phase 0 (không trộn 2
  việc "nhận câu trả lời" và "chấm điểm" vào cùng 1 service).
- **Ẩn Answer Key với Candidate**: `ExamSessionQuestionDto`/`CandidateVisibleOptionDto` (namespace
  `ExamTaking`) cố ý KHÔNG có field `IsCorrect` — khác hẳn `QuestionOptionDto` phía ExamManagement (dành
  cho ADMIN) có field này. 2 DTO tách riêng để không thể vô tình rò rỉ đáp án qua serialization.
- Bắt `DbUpdateConcurrencyException` (từ cơ chế `xmin`) ở `SaveAnswerAsync`/`SubmitAsync`, map sang
  `ConflictAppException` (409) thay vì để lộ exception EF Core ra ngoài.

**File đã tạo/sửa (đường dẫn + tên):**
- MỚI: `backend/src/Toeic.Application/ExamTaking/Dtos/ExamSessionDtos.cs`
- MỚI: `backend/src/Toeic.Application/ExamTaking/IExamSessionService.cs`
- MỚI: `backend/src/Toeic.Infrastructure/ExamTaking/ExamSessionService.cs`
- MỚI: `backend/src/Toeic.Api/Controllers/ExamSessionsController.cs`
- SỬA: `backend/src/Toeic.Infrastructure/DependencyInjection.cs` (+ đăng ký `IExamSessionService`)

**Endpoint (`/api/v1/exam-sessions`, toàn bộ `[Authorize(Roles=CANDIDATE)]`, chỉ thao tác trên phiên
của chính mình):**

| Method | Path | Mô tả |
|---|---|---|
| POST | `/exam-sessions` | Bắt đầu/resume phiên thi cho 1 Exam đang ONGOING |
| GET | `/exam-sessions/{id}` | Xem trạng thái phiên (tự auto-submit nếu hết giờ) |
| GET | `/exam-sessions/{id}/questions` | Câu hỏi của đề (ẩn Answer Key) + câu trả lời đã lưu |
| POST | `/exam-sessions/{id}/answers` | Auto-save 1 câu trả lời (UPSERT) |
| POST | `/exam-sessions/{id}/submit` | Nộp bài (idempotent) |

**Việc tiếp theo cần làm:**
- Build/verify thực tế trên máy có `dotnet` SDK + mạng đầy đủ tới NuGet (vẫn chưa build được trong sandbox — xem "Vấn đề còn vướng").
- **Backend Phase 1 đã đủ để demo end-to-end** theo kịch bản mục L (Phase 0): Admin tạo Test → thêm câu hỏi → publish → tạo Exam → gán Test → chuyển ONGOING; Candidate đăng ký/đăng nhập → bắt đầu phiên → xem câu hỏi → auto-save → submit. **Chưa có UI** — cần Frontend Next.js tối thiểu để demo trực quan (chưa làm ở Phase 1, có thể làm ngay hoặc chuyển sang Phase 2 — Objective Scoring — trước rồi làm UI sau, tuỳ ưu tiên của bạn).
- Phase 2 (theo roadmap gốc): **Objective Scoring Engine** cho Listening/Reading — đọc `CandidateAnswers` sau khi `Status=SUBMITTED`, so khớp `SelectedOptionId` với `QuestionOptions.IsCorrect`, tính `IsCorrect`/`Score` cho từng câu và cache `ListeningScore`/`ReadingScore`/`TotalScore` vào `ExamSessions`.

**Vấn đề còn vướng / cần hỏi lại:**
- Sandbox hiện tại (nơi Claude chạy code) không có `dotnet` SDK cài sẵn và mạng bị chặn tới `nuget.org` → **chưa tự build/verify được** cho toàn bộ Phase 1 (4 part). Cần user tự chạy `dotnet restore && dotnet build` ở máy local và báo lỗi nếu có — đây là rủi ro tích luỹ lớn nhất hiện tại vì chưa từng compile thử toàn bộ solution.
- Auto-submit khi hết giờ là "lazy" (không có background job) — nêu ở trên, cần bạn xác nhận có chấp nhận được cho MVP hay muốn ưu tiên làm ngay 1 `IHostedService` quét định kỳ.
- Chưa giới hạn "retake policy" (thí sinh thi lại 1 Exam bao nhiêu lần) — hiện tại sau khi phiên cũ SUBMITTED, gọi lại `POST /exam-sessions` sẽ tạo phiên mới không giới hạn số lần.
- Chưa có endpoint cho Admin/Rater xem danh sách ExamSession của thí sinh (sẽ cần ở Phase 4 — Human Rater — hoặc Phase 6 — Dashboard).

---

## Lịch sử các Phase đã hoàn thành

### Phase 0 — Project Review & Final Architecture
**Trạng thái:** xong — xem toàn bộ nội dung trong `PHASE0_Project_Review_Final_Architecture.md` (9 ISSUE đã xử lý, kiến trúc/DB/roadmap đã chốt).

### Phase 1 — Part 1: Database + EF Core
**Trạng thái:** xong — 14 entity + Fluent API configuration đầy đủ theo Phase 0 (xem chi tiết trong README.md phần "Part 1").

### Phase 1 — Part 2: Auth/RBAC module
**Trạng thái:** xong — Register/Login/JWT access+refresh token/RBAC 3 role (xem chi tiết trong README.md phần "Part 2").

### Phase 1 — Part 3: Exam Management CRUD
**Trạng thái:** xong — Exam/Test/ExamTest/Question/QuestionOption CRUD, state machine, publish validation, DbSeeder (xem chi tiết trong README.md phần "Part 3").
