# TOEIC Scoring Support System — Phase 1 (Part 1 + Part 2)

Roadmap theo `PHASE0_Project_Review_Final_Architecture.md` (mục M/N) và
nguyên tắc "mỗi lần chỉ triển khai một module hoàn chỉnh" (mục 25–26 prompt gốc).

## Part 1 — Database + EF Core

- `backend/src/Toeic.Domain` — 14 entity đúng theo bản schema **đã sửa** ở
  Phase 0 (2 bảng mới `ExamTest`, `AssessmentCriterionScore`, toàn bộ
  ISSUE-01 → ISSUE-09) + entity mới `RefreshToken` (Part 2).
- `backend/src/Toeic.Infrastructure/Persistence` — `AppDbContext` + Fluent API
  configuration cho từng bảng: partial unique index, UNIQUE constraint,
  CHECK constraint, optimistic concurrency qua `xmin`.
- `docker/docker-compose.yml` — PostgreSQL 16 cho local dev.

## Part 2 — Auth/RBAC module

**Kiến trúc:** thêm project `Toeic.Application` (Clean Architecture — mục G,
Phase 0) chứa DTO + interface (`IAuthService`, `IPasswordHasher`,
`IJwtTokenService`) + exception nghiệp vụ (`AppException` và các subclass).
`Toeic.Infrastructure` implement các interface đó, dùng thẳng `AppDbContext`
(không thêm `Repository<T>` — đúng quyết định đã ghi ở Part 1).

**Chức năng:**
- **Password hashing**: PBKDF2-HMAC-SHA256, 100.000 vòng lặp, salt 128-bit
  ngẫu nhiên/mật khẩu — dùng API sẵn có của .NET (`Rfc2898DeriveBytes`),
  không cần thêm NuGet package ngoài cho phần này.
- **Access token**: JWT (HMAC-SHA256), claim `sub`/`role`/`email`/`username`/`jti`,
  hết hạn sau `Jwt:AccessTokenMinutes` (mặc định 30 phút).
- **Refresh token**: chuỗi ngẫu nhiên 256-bit opaque (KHÔNG phải JWT), lưu
  **hash SHA-256** trong bảng `RefreshTokens` (không bao giờ lưu raw token).
  Hỗ trợ **rotation**: mỗi lần gọi `/auth/refresh`, token cũ bị revoke và
  token mới được cấp; nếu 1 token đã-revoke bị dùng lại (dấu hiệu bị đánh
  cắp), toàn bộ token đang active của user đó bị revoke ngay.
- **RBAC**: 3 role `ADMIN`, `RATER`, `CANDIDATE`. Endpoint public
  `/auth/register` **luôn** ép `Role = CANDIDATE` bất kể client gửi gì; chỉ
  `/auth/register-staff` (yêu cầu `[Authorize(Roles = "ADMIN")]`) mới tạo
  được tài khoản `ADMIN`/`RATER`.
- **Exception handling**: middleware toàn cục map `AppException` (400/401/
  403/404/409) sang `application/problem+json`; lỗi hệ thống không mong
  muốn trả 500 chung chung, không rò rỉ chi tiết nội bộ.

**Endpoint (`/api/v1/auth`):**

| Method | Path | Quyền | Mô tả |
|---|---|---|---|
| POST | `/register` | Public | Tạo tài khoản CANDIDATE (tự động tạo hồ sơ `Candidate`) |
| POST | `/register-staff` | ADMIN | Tạo tài khoản ADMIN/RATER |
| POST | `/login` | Public | Đăng nhập, trả access + refresh token |
| POST | `/refresh` | Public (cần refresh token hợp lệ) | Cấp lại cặp token mới (rotation) |
| POST | `/logout` | Public (cần refresh token hợp lệ) | Revoke 1 refresh token |
| GET | `/me` | Đã đăng nhập | Trả hồ sơ user hiện tại từ JWT claim |

**File mới:**
- `backend/src/Toeic.Application/**` (project mới)
- `backend/src/Toeic.Domain/Entities/RefreshToken.cs`
- `backend/src/Toeic.Infrastructure/Security/{PasswordHasher,JwtOptions,JwtTokenService}.cs`
- `backend/src/Toeic.Infrastructure/Auth/AuthService.cs`
- `backend/src/Toeic.Infrastructure/DependencyInjection.cs`
- `backend/src/Toeic.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- `backend/src/Toeic.Api/Controllers/AuthController.cs`
- `backend/src/Toeic.Api/Middleware/ExceptionHandlingMiddleware.cs`
- `Program.cs`, `appsettings*.json`, `Toeic.sln` — cập nhật

## ⚠️ Quan trọng: chưa build/test được trong sandbox này

Môi trường mình đang chạy **không có `dotnet` SDK cài sẵn và không có quyền
truy cập `nuget.org`** (network của sandbox chỉ mở whitelist domain, không có
NuGet). Vì vậy mình **không thể tự `dotnet restore`/`build`/`ef migrations add`
để verify code compile được** trước khi gửi cho bạn — khác với cam kết ở mục
26 của prompt gốc ("code phải chạy được"). Code được viết đúng theo API/pattern
chuẩn của .NET 8 + EF Core 8 + Npgsql, nhưng bạn cần tự build ở máy có mạng đầy
đủ và báo lại nếu có lỗi biên dịch để mình sửa ngay.

## Cách chạy (trên máy bạn, có Docker + .NET 8 SDK)

```bash
# 1. Khởi động PostgreSQL
cd docker
docker compose up -d

# 2. Cài EF Core CLI tool (nếu chưa có)
dotnet tool install --global dotnet-ef

# 3. Đặt Jwt:Key qua user-secrets (KHÔNG hard-code, mục 8/18 prompt gốc)
cd ../backend
dotnet restore
dotnet user-secrets init --project src/Toeic.Api
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/Toeic.Api
# (appsettings.Development.json đã có sẵn 1 key dev-only để chạy nhanh nếu bạn
#  không set user-secrets — nhưng KHÔNG dùng key đó ngoài môi trường local.)

# 4. Tạo migration đầu tiên từ Toeic.Infrastructure
dotnet ef migrations add InitialCreate \
  --project src/Toeic.Infrastructure \
  --startup-project src/Toeic.Api

# 5. Áp dụng migration vào DB
dotnet ef database update \
  --project src/Toeic.Infrastructure \
  --startup-project src/Toeic.Api

# 6. Chạy API
dotnet run --project src/Toeic.Api
# Swagger: https://localhost:<port>/swagger  (đã có nút "Authorize" để nhập Bearer token)
# Health check: GET /api/v1/health -> { "status": "ok", "database": "connected" }
```

Connection string dev mặc định (`appsettings.Development.json`) khớp với
`docker-compose.yml` — không phải secret thật, chỉ dùng cho local dev.

### Thử nhanh luồng Auth

```bash
# Đăng ký 1 candidate
curl -X POST https://localhost:<port>/api/v1/auth/register -k \
  -H "Content-Type: application/json" \
  -d '{"username":"candidate1","fullName":"Nguyen Van A","email":"c1@example.com","password":"P@ssw0rd123"}'

# Đăng nhập
curl -X POST https://localhost:<port>/api/v1/auth/login -k \
  -H "Content-Type: application/json" \
  -d '{"usernameOrEmail":"candidate1","password":"P@ssw0rd123"}'
# -> { accessToken, refreshToken, user: {...} }

# Gọi endpoint cần đăng nhập
curl https://localhost:<port>/api/v1/auth/me -k -H "Authorization: Bearer <accessToken>"
```

Tài khoản ADMIN đầu tiên hiện chưa có seed data tự động (sẽ thêm ở Part 3 cùng
Exam CRUD, hoặc có thể tạo thủ công qua `dotnet ef` seed / trực tiếp SQL —
nêu nếu bạn muốn ưu tiên việc này sớm hơn).

## Part 3 — Exam Management CRUD

**Service boundary:** `IExamService` (Exam + gán/gỡ Test qua ExamTests),
`ITestService` (Test + Publish/Archive), `IQuestionService` (Question +
QuestionOption) — theo đúng nhóm bảng đã chốt ở Phase 0, controller không bị
phình to.

**Quy tắc toàn vẹn quan trọng:**
- Question/QuestionOption chỉ sửa được khi `Test.Status = DRAFT`. Test tự nó
  cũng chỉ sửa được khi DRAFT. Sau khi `PUBLISHED`, nội dung bị khoá — bảo vệ
  tính đúng đắn cho các phiên thi đã/đang diễn ra dựa trên nội dung đó.
- `Test.Publish()` validate toàn bộ câu hỏi trước khi cho publish (liệt kê
  đầy đủ lỗi, không dừng ở lỗi đầu tiên): Listening cần `AudioUrl`, Picture
  Description (Q1–5) cần `ImageUrl`, MULTIPLE_CHOICE cần ≥2 option và đúng
  1 option `IsCorrect = true`.
- **Exam state machine**: `DRAFT → SCHEDULED → ONGOING → COMPLETED`, hoặc
  `→ CANCELLED` từ bất kỳ trạng thái nào chưa COMPLETED (`SCHEDULED → DRAFT`
  cũng được phép để rút lại lịch nếu cần sửa). Chuyển sang `SCHEDULED` bắt
  buộc phải có ≥1 Test đang `IsActive` trong `ExamTests`.
- `AssignTest` chỉ nhận Test đã `PUBLISHED`. `UnassignTest` **không xoá**
  dòng `ExamTests` — chỉ tắt `IsActive` (giữ lịch sử, ISSUE-01 Phase 0).

**Bootstrap ADMIN:** thêm `DbSeeder` — nếu cấu hình `SeedAdmin:Username/
Email/Password` đầy đủ và chưa có user `Role=ADMIN` nào, tự tạo 1 tài khoản
ADMIN khi app khởi động (idempotent, không hard-code secret).

**Endpoint chính (`/api/v1`, toàn bộ yêu cầu `Authorize(Roles=ADMIN)`):**

| Method | Path | Mô tả |
|---|---|---|
| POST/GET | `/exams`, `/exams/{id}` | CRUD Exam |
| PUT/DELETE | `/exams/{id}` | Sửa/xoá (chỉ khi DRAFT) |
| POST | `/exams/{id}/status` | Chuyển trạng thái theo state machine |
| POST/DELETE | `/exams/{id}/tests`, `/exams/{id}/tests/{testId}` | Gán/gỡ Test |
| POST/GET | `/tests`, `/tests/{id}` | CRUD Test |
| POST | `/tests/{id}/publish`, `/tests/{id}/archive` | Đổi trạng thái Test |
| GET/POST | `/tests/{id}/questions` | Danh sách/tạo Question theo Test |
| GET/PUT/DELETE | `/questions/{id}` | CRUD Question |
| POST | `/questions/{id}/options` | Thêm QuestionOption |
| PUT/DELETE | `/question-options/{id}` | Sửa/xoá QuestionOption |

**File mới:**
- `backend/src/Toeic.Application/Common/Dtos/PagedResult.cs`
- `backend/src/Toeic.Application/ExamManagement/**` (DTOs + 3 interface)
- `backend/src/Toeic.Infrastructure/ExamManagement/{ExamService,TestService,QuestionService}.cs`
- `backend/src/Toeic.Infrastructure/Persistence/DbSeeder.cs`
- `backend/src/Toeic.Api/Controllers/{ApiControllerBase,ExamsController,TestsController,QuestionsController}.cs`

### Thử nhanh luồng Exam Management (sau khi có access token ADMIN)

```bash
TOKEN="<access token của tài khoản ADMIN>"

# Tạo Test
curl -X POST https://localhost:<port>/api/v1/tests -k \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"testCode":"FORM-A","testName":"TOEIC Form A","version":"1.0","listeningDurationSeconds":2700,"readingDurationSeconds":4500,"writingDurationSeconds":3600}'

# Thêm câu hỏi Listening (ví dụ) rồi thêm option, sau đó publish
curl -X POST https://localhost:<port>/api/v1/tests/<testId>/publish -k -H "Authorization: Bearer $TOKEN"

# Tạo Exam, gán Test đã publish, chuyển sang SCHEDULED
curl -X POST https://localhost:<port>/api/v1/exams -k \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"examCode":"TOEIC-2026-08","examName":"TOEIC Ky thi thang 8","examDate":"2026-08-30","startTime":"2026-08-30T08:00:00Z","endTime":"2026-08-30T11:00:00Z"}'
curl -X POST https://localhost:<port>/api/v1/exams/<examId>/tests -k \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"testId":"<testId>"}'
curl -X POST https://localhost:<port>/api/v1/exams/<examId>/status -k \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"status":"SCHEDULED"}'
```


## Part 4 — ExamSession + CandidateAnswer (hoàn tất Phase 1 backend)

**Quy tắc quan trọng:**
- `POST /exam-sessions` bắt đầu làm bài **ngay lập tức** (`Status=IN_PROGRESS`),
  không dùng trạng thái trung gian `NOT_STARTED`. Gọi lại cho cùng Exam khi đã
  có phiên đang diễn ra sẽ **resume** phiên đó (idempotent) thay vì lỗi.
- Hệ thống **tự chọn ngẫu nhiên 1 Test form** trong số các Test đang `IsActive`
  của Exam — Candidate không tự chọn đề (chống gian lận, ISSUE-01).
- `ExpiresAt` **chốt đúng 1 lần** khi tạo phiên = `StartedAt` + tổng 3 duration
  của Test đã chọn (ISSUE-07) — không bao giờ tính lại từ `Test` sau đó.
- **Auto-submit kiểu lazy**: mọi action chạm vào phiên thi đều kiểm tra
  `UtcNow >= ExpiresAt` trước; nếu hết giờ, tự chuyển `SUBMITTED` với
  `SubmittedAt = ExpiresAt`. Đây là hạn chế đã biết: MVP **chưa có background
  job** chủ động quét phiên hết giờ khi không có request nào chạm tới — có
  thể bổ sung `IHostedService` ở phase sau nếu cần Status real-time.
- **Answer Key luôn bị ẩn với Candidate** — `ExamSessionQuestionDto` dùng
  DTO riêng (`CandidateVisibleOptionDto`), tách biệt hoàn toàn với DTO phía
  ADMIN (`QuestionOptionDto` có field `IsCorrect`).
- `SaveAnswerAsync` **chỉ ghi nhận câu trả lời**, không tính điểm — chấm điểm
  Listening/Reading thuộc **Phase 2 (Objective Scoring Engine)**, giữ đúng
  ranh giới module đã chốt ở Phase 0.

**Endpoint (`/api/v1/exam-sessions`, `[Authorize(Roles=CANDIDATE)]`):**

| Method | Path | Mô tả |
|---|---|---|
| POST | `/exam-sessions` | Bắt đầu/resume phiên thi |
| GET | `/exam-sessions/{id}` | Xem trạng thái phiên |
| GET | `/exam-sessions/{id}/questions` | Câu hỏi (ẩn Answer Key) + câu trả lời đã lưu |
| POST | `/exam-sessions/{id}/answers` | Auto-save 1 câu trả lời (UPSERT) |
| POST | `/exam-sessions/{id}/submit` | Nộp bài (idempotent) |

**File mới:**
- `backend/src/Toeic.Application/ExamTaking/**`
- `backend/src/Toeic.Infrastructure/ExamTaking/ExamSessionService.cs`
- `backend/src/Toeic.Api/Controllers/ExamSessionsController.cs`

### Thử nhanh luồng thi (end-to-end, cần Exam đã SCHEDULED → chuyển ONGOING)

```bash
ADMIN_TOKEN="<access token ADMIN>"
curl -X POST https://localhost:<port>/api/v1/exams/<examId>/status -k \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -d '{"status":"ONGOING"}'

CANDIDATE_TOKEN="<access token candidate1 đã đăng ký ở Part 2>"

# Bắt đầu phiên thi
curl -X POST https://localhost:<port>/api/v1/exam-sessions -k \
  -H "Authorization: Bearer $CANDIDATE_TOKEN" -H "Content-Type: application/json" -d '{"examId":"<examId>"}'
# -> { examSessionId, expiresAt, remainingSeconds, ... }

# Xem câu hỏi
curl https://localhost:<port>/api/v1/exam-sessions/<sessionId>/questions -k -H "Authorization: Bearer $CANDIDATE_TOKEN"

# Auto-save 1 câu trắc nghiệm
curl -X POST https://localhost:<port>/api/v1/exam-sessions/<sessionId>/answers -k \
  -H "Authorization: Bearer $CANDIDATE_TOKEN" -H "Content-Type: application/json" \
  -d '{"questionId":"<questionId>","selectedOptionId":"<optionId>"}'

# Nộp bài
curl -X POST https://localhost:<port>/api/v1/exam-sessions/<sessionId>/submit -k \
  -H "Authorization: Bearer $CANDIDATE_TOKEN"
```

## Phase 1 (backend) đã hoàn tất — bước tiếp theo

Backend hiện đủ để demo end-to-end kịch bản mục L, Phase 0: **Admin tạo đề →
publish → tạo Exam → gán Test → mở ONGOING; Candidate đăng ký/đăng nhập →
bắt đầu phiên → làm bài → submit.** Chưa có Frontend Next.js (chỉ có
Swagger để test thủ công).

Hai hướng có thể đi tiếp — nêu bạn muốn ưu tiên hướng nào:

1. **Phase 2 — Objective Scoring Engine**: chấm tự động Listening/Reading
   sau khi submit (so khớp `CandidateAnswers` với `QuestionOptions.IsCorrect`,
   tính điểm, cache vào `ExamSessions`).
2. **Frontend Next.js tối thiểu** cho Phase 1 để demo trực quan thay vì chỉ
   qua `curl`/Swagger.
