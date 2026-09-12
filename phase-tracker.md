# TOEIC Scoring Support System — Phase Tracker

> File này dùng để theo dõi tiến độ dự án qua nhiều đoạn chat.
> Mỗi khi hoàn thành 1 phần, cập nhật lại file này rồi upload đè lên
> file cũ trong Project Context (Context → xóa file cũ → + thêm file mới).

Tài liệu tham chiếu (đã có trong Context):
- `Prompt xây dựng toàn bộ dự án TOEIC Scoring Support System.md` — quy tắc & yêu cầu gốc
- `PHASE0_Project_Review_Final_Architecture.md` — kiến trúc cuối cùng đã chốt
- `Project.docx` — requirement nghiệp vụ

---

## ✅ XÁC NHẬN TỔNG THỂ (11/09/2026)

Dragon xác nhận: **"ĐÃ BUILD, RUN, TEST ĐƯỢC MỌI THỨ TỪ PHASE 1 ĐẾN 8 VÀ ĐÃ
CHẠY BÌNH THƯỜNG."** Toàn bộ roadmap 9 phase (0→8) đã chốt ở
`PHASE0_Project_Review_Final_Architecture.md` mục M/N nay đã **hoàn tất và
chạy thật end-to-end**, bao gồm cả tầng DevOps (Docker Compose 4 service +
CI/CD + kiến trúc Cloud minh hoạ) mới hoàn thành ở phiên làm việc này.

**Không còn phase nào trong roadmap gốc còn dang dở.** Các mục A/B/C từng
liệt kê ở phần "Việc tiếp theo" (double-check giả định nghiệp vụ chi tiết kế
thừa từ Phase 3/4/5/6 — tên enum, tên field JSON, công thức gộp điểm...) được
coi là **đã khép lại** theo xác nhận tổng thể này; xem mục "Việc tiếp theo"
bên dưới để biết chi tiết còn lại (không chặn, chỉ là cải tiến ngoài phạm vi
đồ án nếu Dragon muốn đi tiếp).

---

## Trạng thái tổng quan

| Phase | Tên | Trạng thái | Ghi chú |
|---|---|---|---|
| 0 | Project Review & Final Architecture | ✅ Xong | Xem PHASE0_Project_Review_Final_Architecture.md |
| 1 | MVP Core | ✅ Xong (4/4 part) — **đã build/run/test thật** | Part 1: DB+EF Core. Part 2: Auth/RBAC. Part 3: Exam CRUD. Part 4: ExamSession+CandidateAnswer. |
| 2 | Objective Scoring (Listening/Reading) | ✅ Xong — **đã build/run/test thật** | Xem README.md Phase 2. |
| 3 | Writing AI Scoring | ✅ Xong (4/4 part) — **đã build/run/test thật** | Rubric Management, Writing Scoring Engine, AI Assessment Service, Wiring (async background job). |
| 4 | Human Rater | ✅ Xong (2/2 part) — **đã build/run/test thật** | Part 1: Rater Dashboard + Human Scoring. Part 2: Wiring `IsFinal` (ISSUE-04) + AI-vs-Human comparison endpoint. |
| 5 | AI vs Human Evaluation | ✅ Xong (3/3 part) — **đã build/run/test thật** | Part 1: Data ingestion. Part 2: Metrics (MAE/RMSE/Pearson/Spearman/Agreement Rate). Part 3: Report top-lệch + export CSV/JSON. |
| 6 | Dashboard | ✅ Xong (3/3 part) — **đã build/run/test thật** | Part 1: Candidate History. Part 2: Performance Analytics. Part 3: Admin Dashboard + Writing Error Analytics. |
| 7 | Frontend (Next.js + TypeScript) — đầy đủ Admin/Rater/Candidate | ✅ Xong (6/6 Part) — **đã chạy end-to-end thật** | PHASE7-ISSUE-10 đã đóng. Bulk Import/Update Excel (ISSUE-19/20) đã test đúng. |
| **8** | **DevOps** | ✅ **Xong (3/3 Part) — đã build/run/test thật (11/09/2026)** | Part 1: Dockerize (3 Dockerfile + compose, 3 hotfix thật). Part 2: CI/CD GitHub Actions (5 workflow). Part 3: Cloud Deployment architecture (AWS, minh hoạ). |

**✅ Cập nhật trạng thái build (02/09/2026):** Dragon xác nhận đã **build, run, test thành
công thật** toàn bộ Phase 1→6 (.NET + Python) trên môi trường của Dragon.

**✅ Cập nhật 09/09/2026:** Phase 7 (Frontend) hoàn tất 6/6 Part, đã chạy end-to-end thật
với backend .NET + evaluation-service (2 vòng hotfix, PHASE7-ISSUE-10 đã đóng).

**✅ Cập nhật 11/09/2026 — Phase 8 (DevOps) hoàn tất, dự án hoàn thành toàn bộ roadmap
0→8:** xem mục "Phase 8 — DevOps" trong "Lịch sử các Phase đã hoàn thành" bên dưới cho chi
tiết đầy đủ (3 Part + 3 hotfix thật phát sinh khi Dragon chạy `docker compose up` lần đầu).

---

## Việc tiếp theo (không chặn — cải tiến tuỳ chọn, ngoài phạm vi roadmap gốc)

**Toàn bộ roadmap 9 phase (0→8) đã hoàn tất theo xác nhận tổng thể 11/09/2026 ở đầu file.**
Không còn "việc bắt buộc" nào theo đúng phạm vi đã chốt ở `PHASE0_Project_Review_Final_
Architecture.md` mục M/N. Các mục dưới đây là gợi ý nếu Dragon muốn tiếp tục phát triển dự án
xa hơn phạm vi đồ án ban đầu — hoàn toàn tuỳ chọn.

**Kỹ thuật / vận hành (từ Phase 8):**
1. Bổ sung Playwright test suite tự động thật cho `frontend/` (hiện chỉ verify thủ công
   trong phiên làm việc — PHASE8-ISSUE-09, job `e2e-placeholder` đang `if: false` trong
   `frontend-ci.yml`).
2. Bật `docker-publish.yml` (push GHCR) hoặc nối CI/CD sang deploy AWS thật (mục 6-7 của
   `PHASE8_PART3_CLOUD_DEPLOYMENT.md`) nếu Dragon muốn có 1 bản demo online thay vì chỉ chạy
   local qua `docker compose up`.
3. Chạy `dotnet format`/`ruff check --fix` cục bộ 1 lần rồi gỡ `continue-on-error: true` ở
   2 bước lint trong `backend-ci.yml`/`evaluation-ci.yml` để lint thật sự chặn PR sau này.
4. Quy đổi thang điểm TOEIC scaled 5–495 (để ngỏ từ Phase 2/3 — chưa từng là yêu cầu bắt
   buộc của đề tài, chỉ là điểm mở rộng khả dĩ).
5. Cân nhắc thêm bảng `RaterAssignments` nếu số lượng Rater thật lớn (PHASE4-ISSUE-01, MVP
   hiện tại mọi RATER xem/chấm được mọi bài — chấp nhận được cho quy mô đồ án).

**Không cần làm lại các mục double-check A/B/C từng liệt kê ở các phiên trước** (đối chiếu
tên enum/field JSON kế thừa Phase 3-6) — xác nhận tổng thể 11/09/2026 ("mọi thứ chạy bình
thường") coi các điểm đó là đã khép lại trong thực tế vận hành. Nếu về sau phát hiện số liệu
hiển thị sai ở Dashboard/Evaluation, quay lại đối chiếu cụ thể `AssessmentCriterionScores`,
`ErrorsJson`, enum `AssessmentSourceType` như đã ghi chi tiết ở lịch sử Phase 6 bên dưới.

---

## Phase 7 — Frontend (Kế hoạch, chưa bắt đầu code)

> Chèn vào roadmap theo yêu cầu của Dragon (02/09/2026) — xem `PHASE0_Project_Review_Final_
> Architecture.md` mục M/N (bảng roadmap cập nhật) và ISSUE-10 (lý do đổi thứ tự Phase 7/8).
> **Yêu cầu rõ từ Dragon: Frontend "hoàn chỉnh", không phải bản tối thiểu** — đủ toàn bộ màn
> hình liệt kê ở mục 15 prompt gốc cho cả 3 role, cộng các trang tương ứng Phase 2-6 backend
> đã hoàn tất (Objective Scoring result, Writing AI result, Human Rater, Evaluation, Dashboard).

### Rà soát trước khi code (theo đúng quy trình Issue-first đã áp dụng xuyên suốt dự án)

**PHASE7-ISSUE-01 — Cơ chế lưu JWT phía Frontend** (đã Decision — xem chi tiết đầy đủ
Problem/Severity/Root Cause/Alternative/Reason trong `PHASE0_Project_Review_Final_
Architecture.md` mục H): dùng **Next.js Route Handler làm BFF** cho `/auth/login`,
`/auth/refresh`, `/auth/logout` — Route Handler gọi backend .NET thật, tự set `httpOnly`
cookie cho `refreshToken`; `accessToken` giữ trong bộ nhớ phía client (không `localStorage`).
Không sửa `AuthController` backend (đã build/test, khoá lại).

**PHASE7-ISSUE-02 — Thư viện UI component.** Severity: Low. Số lượng màn hình lớn (Admin CRUD
nhiều bảng, Exam Room, Rater review, 3 loại Dashboard) nên nếu tự viết toàn bộ component từ
đầu (form, table, modal, toast...) sẽ tốn thời gian không cần thiết cho một đồ án. **Decision:**
dùng **shadcn/ui** (Radix UI + Tailwind, copy code vào repo — không phải black-box dependency,
đúng tinh thần "không over-engineering" vì vẫn toàn quyền chỉnh sửa) làm nền component, kết
hợp Tailwind CSS đã chốt sẵn ở Phase 0 mục H.

**PHASE7-ISSUE-03 — Nguồn dữ liệu cho trang Evaluation (Admin).** `evaluation-service`
(Python, Phase 5) là **service độc lập, base URL khác** với backend .NET. Severity: Low.
**Decision:** Frontend gọi thẳng `evaluation-service` từ trang Admin Evaluation qua một
API client riêng (`lib/evaluationApiClient.ts`), tách biệt với client gọi backend .NET
chính (`lib/apiClient.ts`) — đúng ranh giới 2 service đã chốt ở Phase 0 mục F/J, không gộp
chung 1 client để tránh nhầm lẫn base URL/auth (evaluation-service dùng tài khoản RATER
`eval-service` riêng ở server-side, KHÔNG lộ ra trình duyệt — Frontend gọi qua Route Handler
proxy tương tự PHASE7-ISSUE-01, không gọi thẳng từ client browser).

**PHASE7-ISSUE-04 — Audio/Image upload (Admin tạo câu hỏi).** Backend Phase 1 Part 3 hiện
lưu `ImageUrl`/`AudioUrl` dạng string — **chưa có endpoint upload file thật** (Dragon cần xác
nhận: có endpoint upload trong Toeic.Api chưa, hay Admin nhập URL trỏ tới file đã có sẵn ở
nơi khác?). Severity: Medium — chặn trực tiếp 1 phần UI Admin (mục 15: "Upload image",
"Upload audio"). **Decision tạm thời:** Part 2 (Admin) code UI dạng **nhập URL** trước (không
block tiến độ); nếu Dragon xác nhận có/cần endpoint upload file thật, bổ sung ở 1 bản vá nhỏ
sau — không đợi vì đây không phải phần lõi nghiệp vụ.

### Chia Part (theo đúng nguyên tắc "mỗi lần một module hoàn chỉnh" — mục 25 prompt gốc,
cùng cách chia đã áp dụng cho Phase 1/3/4/6)

| Part | Nội dung | Phụ thuộc | Demo cuối Part |
|---|---|---|---|
| **1. Foundation** ✅ | Scaffold Next.js App Router + TypeScript + Tailwind + shadcn/ui; `apiClient` (fetch wrapper, tự đính `Authorization` header, tự refresh khi 401); Route Handler BFF cho Auth (PHASE7-ISSUE-01); trang Login/Register; middleware RBAC theo route group `/admin`, `/rater`, `/candidate` (PHASE7-ISSUE-05); layout + nav riêng từng role | Backend Phase 1 Part 2 | ✅ Login đúng role → vào đúng layout tương ứng; sai role bị chặn — xem "Lịch sử" bên dưới |
| **2. Admin — Content Management** ✅ | CRUD Exam/Test/ExamTest (gán/gỡ)/Question/QuestionOption (mục 15 "Admin"); CRUD RubricSet/Rubric (Phase 3 Part 1); state machine UI (DRAFT/PUBLISHED/ARCHIVED, SCHEDULED/ONGOING...); form nhập URL ảnh/audio (PHASE7-ISSUE-04) | Part 1; Backend Phase 1 Part 3, Phase 3 Part 1 | ✅ Admin tạo Test → thêm Question/Option → Publish → tạo Exam → gán Test → chuyển SCHEDULED, hoàn toàn qua UI — xem "Lịch sử" bên dưới |
| **3. Candidate — Exam Room** ✅ | Exam list/detail (chỉ Exam đã publish/scheduled/ongoing); Start/Resume session; Exam Room: Listening UI (audio player), Reading UI, Writing UI (Q1-5/Q6-7/Q8, hiển thị `RequiredWords`/ảnh); Timer đồng bộ `ExpiresAt` server (không tự tính ở client — mục 16 prompt gốc); auto-save (debounce, optimistic update); Submit; xử lý refresh/reconnect | Part 1; Backend Phase 1 Part 4 | ✅ Candidate login → chọn Exam → làm đủ 3 phần → submit, đúng qua UI thật — xem "Lịch sử" bên dưới |
| **4. Candidate — Result/History/Analytics** ✅ | Result page (Listening/Reading/Writing tách riêng — không TotalScore, đúng PHASE3-ISSUE-07/PHASE6-ISSUE-02); trạng thái Writing Pending/Scored (Phase 3 Part 4); History list (Phase 6 Part 1); Performance Analytics — biểu đồ Strength/Weakness + Score Trend (Phase 6 Part 2) | Part 1, 3; Backend Phase 2, 3 Part 4, 6 Part 1-2 | ✅ Sau khi Writing được chấm xong (background job) → xem Result đầy đủ 3 điểm + feedback; xem History nhiều phiên + biểu đồ xu hướng — xem "Lịch sử" bên dưới |
| **5. Rater** ✅ | Danh sách bài Writing (Pending/Rated, filter — Phase 4 Part 1); trang chi tiết 1 bài (câu trả lời + AI assessment hiện hành + Rubric ACTIVE + lịch sử Human); form nhập điểm theo Criterion → gọi `WritingScoringEngine` qua API; trang AI-vs-Human comparison (Phase 4 Part 2) | Part 1; Backend Phase 4 Part 1-2 | ✅ Rater chấm 1 bài qua UI → thấy điểm AI cạnh điểm vừa chấm, sắp theo mức lệch — xem "Lịch sử" bên dưới |
| **6. Admin — Dashboard & Evaluation** ✅ | Admin Dashboard overview + Writing Error Analytics (Phase 6 Part 3); trang Evaluation: dataset summary, Metrics (MAE/RMSE/Correlation/Agreement, filter theo `questionType`/`agreementThreshold`), Report (top lệch), nút Export CSV/JSON (Phase 5, qua `evaluationApiClient` — PHASE7-ISSUE-03) | Part 1; Backend Phase 5, Phase 6 Part 3 | ✅ Admin xem tổng quan hệ thống + chạy báo cáo AI vs Human ngay trên UI, tải CSV — xem "Lịch sử" bên dưới |

**Ghi chú:**
- Part 1-3 là đường găng (critical path) để có demo "Candidate thi thật qua UI" sớm nhất —
  ưu tiên làm trước, đúng tinh thần Demo First.
- Part 4-6 phụ thuộc dữ liệu nghiệp vụ đã đối chiếu đúng ở mục A (Kế thừa Phase 3/4) — nếu
  các giả định đó sai, UI vẫn build/chạy được nhưng số liệu hiển thị có thể sai; không chặn
  code nhưng nên double-check trước khi coi Part 4-6 là "Definition of Done" đầy đủ (mục 29
  prompt gốc).
- Responsive design (mục "Frontend" — yêu cầu công nghệ) áp dụng xuyên suốt tất cả Part, không
  tách thành Part riêng.
- Mỗi Part kết thúc đều phải chạy được thật với backend .NET (+ evaluation-service ở Part 6)
  đang chạy cục bộ, không dùng mock data — đúng Definition of Done mục 29 prompt gốc.

---

## Lịch sử các Phase đã hoàn thành

### Phase 0 — Project Review & Final Architecture
**Trạng thái:** xong — xem toàn bộ nội dung trong `PHASE0_Project_Review_Final_Architecture.md` (9 ISSUE đã xử lý, kiến trúc/DB/roadmap đã chốt).

### Phase 1 — Part 1: Database + EF Core
**Trạng thái:** xong — 14 entity + Fluent API configuration đầy đủ theo Phase 0 (xem chi tiết trong README.md phần "Part 1"). 14 entity gồm 12 bảng gốc (Users, Candidates, Exams, Tests, Questions, QuestionOptions, ExamSessions, CandidateAnswers, RubricSets, Rubrics, WritingAssessments, HumanAssessments) + 2 bảng mới từ Phase 0 review (ExamTest, AssessmentCriterionScore).

### Phase 1 — Part 2: Auth/RBAC module
**Trạng thái:** xong — Register/Login/JWT access+refresh token/RBAC 3 role (xem chi tiết trong README.md phần "Part 2").

### Phase 1 — Part 3: Exam Management CRUD
**Trạng thái:** xong — Exam/Test/ExamTest/Question/QuestionOption CRUD, state machine, publish validation, DbSeeder (xem chi tiết trong README.md phần "Part 3").

### Phase 1 — Part 4: ExamSession + CandidateAnswer
**Trạng thái:** xong — bắt đầu/resume phiên thi, chọn Test form ngẫu nhiên, `ExpiresAt` chốt 1 lần,
lazy auto-submit, auto-save UPSERT, ẩn Answer Key với Candidate (xem chi tiết trong README.md phần
"Part 4"). Backend Phase 1 đủ để demo end-to-end "Candidate login → chọn Exam → làm bài → submit".

### Phase 2 — Objective Scoring Engine (Listening/Reading)
**Trạng thái:** xong — `ObjectiveScoringEngine` (domain, thuần logic), chấm đồng bộ khi
SUBMITTED, idempotent, câu không trả lời vẫn được ghi nhận, `GET /exam-sessions/{id}/result`.
Chưa quy đổi thang điểm TOEIC scaled 5–495 (để ngỏ — không phải yêu cầu bắt buộc).

---

### Phase 3 — Writing AI Scoring (✅ 4/4 Part xong)

Chia làm 4 Part để tuân thủ "mỗi lần một module hoàn chỉnh" (mục 25 prompt gốc).

#### Part 1 — Rubric Management
CRUD `RubricSet`/`Rubric` cho Admin, state machine DRAFT→ACTIVE→ARCHIVED.
`RubricSetValidation` (Domain, thuần logic, unit-test được — cùng pattern
`ObjectiveScoringEngine`): đủ ≥1 Rubric row, weight nhất quán theo Criterion,
không trùng Level, không chồng lấn khoảng điểm, tổng weight = 1.0.
**PHASE3-ISSUE-01**: đúng 1 RubricSet ACTIVE/QuestionType — auto-archive bản
cũ khi activate bản mới; enforce ở Application layer, **chưa** có partial
unique index DB (để ngỏ). RubricSet/Rubric chỉ sửa được khi DRAFT.
File: `PHASE3_PART1_INTEGRATION_GUIDE.md`. Endpoint: `/rubric-sets/**`,
`/rubrics/{id}`.

#### Part 2 — Writing Scoring Engine
`WritingScoringEngine.CalculateOverallScore(rubrics, criterionScores,
targetMaxScore)` (Domain, thuần logic, không AI/EF Core) — module DUY NHẤT
được tính Final Score cho Writing (AI/Human chỉ cung cấp criterion scores).
Chuẩn hoá mỗi tiêu chí về `[0,1]` theo range của rubric, nhân Weight, cộng
lại, nhân `targetMaxScore` (mặc định 100). **Reject** (không clamp) điểm
ngoài range; bắt buộc khớp chính xác tập Criterion. File: `PHASE3_PART2_NOTES.md`.

#### Part 3 — AI Assessment Service
`Controller -> IAiAssessmentService -> IPromptBuilder + ILlmProvider ->
AiOutputParser -> WritingScoringEngine`. **PHASE3-ISSUE-02**: JSON schema
yêu cầu AI trả về **sinh động** từ Criterion/MinScore/MaxScore của RubricSet
ACTIVE (không hard-code 3 DTO cố định theo Q1-5/Q6-7/Q8). **PHASE3-ISSUE-03**:
Q1-5 ImageRelevance chưa tích hợp Vision AI thật (dùng ImageDescription text
tuỳ chọn). **PHASE3-ISSUE-04**: retry tối đa `MaxAttempts` khi AI output sai
định dạng/tiêu chí, tái dùng validation từ Part 2. **PHASE3-ISSUE-05**:
`ILlmProvider` chỉ có 1 implementation tham khảo (`AnthropicLlmProvider`).
Chống prompt injection: `<student_answer>` tag + system prompt + `Sanitize()`.
File: `PHASE3_PART3_INTEGRATION_GUIDE.md`. Endpoint:
`POST /writing-assessments/preview` (ADMIN, không persist).

#### Part 4 — Wiring vào luồng thi thật
Nối Listening/Reading (Phase 2, không đổi) + Writing (Phase 3 Part 1-3) vào
submit/result thật. **PHASE3-ISSUE-06**: chấm Writing **bất đồng bộ** qua
background job — `System.Threading.Channels` + `BackgroundService`.
`SubmitAsync` enqueue job rồi trả response ngay. `WritingAssessmentBackgroundWorker`
dequeue, gọi `WritingResultService.ProcessExamSessionAsync`, persist
`WritingAssessment` + `AssessmentCriterionScores`. `GET .../result` đọc
trạng thái hiện có (`Scored`/`Pending`/`NotAnswered`), tự enqueue lại nếu
phát hiện câu chưa chấm (self-healing). Trade-off đã biết: queue in-memory,
không persistent qua restart (bù bằng self-healing). **PHASE3-ISSUE-07**:
không gộp Writing score vào 1 "Overall Score" chung với Listening/Reading vì
khác thang điểm — trả riêng biệt. File: `PHASE3_PART4_INTEGRATION_GUIDE.md`.

---

### Phase 4 — Human Rater (✅ 2/2 Part xong)

**PHASE4-ISSUE-01**: không có bảng "phân công Rater" — mọi RATER xem/chấm
được mọi Writing answer đã SUBMITTED (YAGNI, MVP).

**PHASE4-ISSUE-02**: xác nhận `AssessmentCriterionScores` theo đúng shape
Phase 0 (E.2.14 — dùng chung AI/Human qua `AssessmentType` + `HumanAssessmentId`
nullable) — đã đối chiếu và chạy đúng.

#### Part 1 — Rater Dashboard + Human Scoring
`Controller -> IHumanRatingService -> AppDbContext` + `WritingScoringEngine`
(tái dùng nguyên vẹn từ Phase 3 Part 2). Rater xem danh sách bài Writing đã
SUBMITTED → xem chi tiết (câu trả lời, AI assessment hiện hành, Rubric
ACTIVE, lịch sử Human) → nhập điểm theo Criterion → lưu `HumanAssessment` +
`AssessmentCriterionScores(AssessmentType=HUMAN)` trong 1 transaction.
Endpoint: `/api/v1/rater/writing-answers/**`. File:
`PHASE4_PART1_INTEGRATION_GUIDE.md`.

#### Part 2 — Wiring `IsFinal` + AI-vs-Human comparison
`IWritingScoreResolver` — DUY NHẤT quyết định "điểm Writing chính thức"
(ưu tiên Human `IsFinal`, fallback AI `IsCurrent`). `GetAiHumanComparisonAsync`
— dataset cặp (AI, Human) theo Criterion, endpoint
`GET /rater/writing-answers/comparison`. File: `PHASE4_PART2_INTEGRATION_GUIDE.md`.

---

### Phase 5 — AI vs Human Evaluation (✅ 3/3 Part)

Python/FastAPI, tách biệt hoàn toàn khỏi `backend/` .NET (Phase 0 mục F/J).

#### Part 1 — Evaluation Service skeleton + Data Ingestion
**PHASE5-ISSUE-01**: Evaluation Service lấy dữ liệu bằng cách gọi
`GET /rater/writing-answers/comparison` qua HTTP, **không** tự kết nối
PostgreSQL. Auth: tài khoản RATER dedicated `eval-service`, tự re-login khi
token hết hạn/401. `BackendClient`, `dataset_loader` (2 pandas DataFrame:
`overall_df`, `criterion_df`), `GET /api/v1/evaluations/dataset`. File:
`PHASE5_PART1_INTEGRATION_GUIDE.md`.

#### Part 2 — Metrics
`metrics.py` (thuần logic): MAE/RMSE (sklearn), Pearson/Spearman (scipy),
Agreement Rate (`|Human - AI| <= agreementThreshold`, mặc định 5.0, cấu hình
qua query param). Không throw khi thiếu dữ liệu (n=0/1, phương sai=0) — trả
`null` kèm `notes`. `GET /api/v1/evaluations/metrics`. 10/10 unit test PASS.
File: `PHASE5_PART2_INTEGRATION_GUIDE.md`.

#### Part 3 — Report & Export
`report.py`: `build_deviation_cases()` (top N lệch điểm nhiều nhất, kèm
breakdown theo Criterion), `dataframe_to_csv_bytes()` (UTF-8 BOM cho Excel
Windows). `GET /evaluations/report`, `GET /evaluations/report/export`
(CSV/JSON). 9/9 test mới PASS (19/19 tổng cả Part 2+3) + smoke test qua
TestClient. File: `PHASE5_PART3_INTEGRATION_GUIDE.md`.

---

### Phase 6 — Dashboard (✅ 3/3 Part)

**PHASE6-ISSUE-01**: thêm `IWritingSessionScoreAggregator` (batch-query, áp
dụng đúng rule Human `IsFinal` > AI `IsCurrent`).
**PHASE6-ISSUE-02**: không gộp Total Score — hiển thị 3 điểm riêng biệt.
**PHASE6-ISSUE-03**: Strength/Weakness — Listening/Reading theo `QuestionType`,
Writing theo `CriterionCode`.

#### Part 1 — Candidate History
`GET /candidates/me/history` — danh sách phiên thi đã nộp, phân trang, 3
điểm số. File: `PHASE6_PART1_INTEGRATION_GUIDE.md`.

#### Part 2 — Candidate Performance Analytics
**PHASE6-ISSUE-04**: chỉ gộp đúng 1 bản "chính thức" (Human `IsFinal` ưu
tiên, else AI `IsCurrent`) khi tính trung bình `AssessmentCriterionScores` —
không cộng dồn cả 2 nguồn. `GET /candidates/me/analytics?trendLimit=20`.
File: `PHASE6_PART2_INTEGRATION_GUIDE.md`.

#### Part 3 — Admin Dashboard + Writing Error Analytics
**PHASE6-ISSUE-06**: parser khoan dung nhiều tên field khả dĩ cho
`ErrorsJson`, đếm riêng bản ghi không parse được. **PHASE6-ISSUE-07**:
Overview trả summary stats (Min/Max/Average/Median) thay vì histogram cố
định (chưa có TOEIC scaled score). `GET /admin/dashboard/overview`,
`GET /admin/dashboard/writing-errors`. File: `PHASE6_PART3_INTEGRATION_GUIDE.md`.

---

### Phase 7 — Frontend (✅ 6/6 Part + Bulk Import/Export bổ sung)

Next.js 16 (App Router) + TypeScript + Tailwind CSS v4 + shadcn/ui + TanStack
Query + Zustand + recharts.

- **Part 1 — Foundation**: Auth BFF qua Route Handler (PHASE7-ISSUE-01), RBAC
  middleware (`proxy.ts`), Zustand giữ accessToken trong bộ nhớ.
- **Part 2 — Admin Content Management**: CRUD Exam/Test/Question/Option,
  Rubric Management, state machine UI.
- **Part 3 — Candidate Exam Room**: đường găng quan trọng nhất — Listening/
  Reading/Writing UI, timer bám `expiresAt` server, auto-save debounce,
  auto-submit khi hết giờ. **PHASE7-ISSUE-10** (endpoint `GET /exams/available`
  cho Candidate) — đã đóng 09/09/2026 sau khi Dragon áp dụng patch backend.
- **Part 4 — Result/History/Analytics**: Result tách 3 điểm riêng biệt,
  polling tự cập nhật khi Writing chuyển Pending→Scored.
- **Part 5 — Rater**: danh sách bài Writing, form chấm điểm sinh động theo
  Rubric ACTIVE, trang so sánh AI-vs-Human.
- **Part 6 — Admin Dashboard & Evaluation**: gọi `evaluation-service` lần đầu
  qua Route Handler proxy (`app/api/evaluation/[...path]/route.ts`).
- **Bổ sung sau Part 6 — Bulk Import/Update Excel** (PHASE7-ISSUE-19/20):
  `POST/PUT /tests/{testId}/questions/bulk`, `GET .../export`, parse Excel
  client-side bằng SheetJS. Đã test và chạy đúng (09/09/2026).

**2 vòng hotfix thật** (06/09/2026, 09/09/2026): sửa key chính `id`→
`{Entity}Id` (PHASE7-ISSUE-18, Critical), sửa 5 shape DTO theo JSON thật từ
Dragon, viết patch backend cho PHASE7-ISSUE-10. Toàn bộ chi tiết xem
`PHASE7_HOTFIX_2026-09-06.md`, `PHASE7_HOTFIX_2_2026-09-06.md`.

**✅ Xác nhận 09/09/2026:** toàn bộ hệ thống (Next.js + .NET + evaluation-service)
chạy cùng lúc, đi qua đúng kịch bản demo end-to-end mục 30 prompt gốc.

---

### Phase 8 — DevOps (✅ 3/3 Part — đã build/run/test thật, 11/09/2026)

> Container hoá + CI/CD + kiến trúc Cloud minh hoạ, đúng phạm vi chốt ở
> `PHASE0_Project_Review_Final_Architecture.md` mục F/K/M-N (ISSUE-10: Phase
> 8 = DevOps, sau khi Frontend Phase 7 đã xong, container hoá 1 lần duy nhất
> cho đủ cả 4 service).

#### Rà soát trước khi code
**PHASE8-ISSUE-01 → 04** (Part 1): auto-migrate `Database.Migrate()` tại
startup thay vì `dotnet ef` trong container (không có EF CLI ở runtime
image); healthcheck + `depends_on: condition: service_healthy` đúng thứ tự
Postgres → Backend → Evaluation → Frontend; secret qua `.env` + .NET
double-underscore convention; thêm route `/api/health` (frontend) và
`/api/v1/health` (evaluation-service, đổi từ đề xuất ban đầu `/health` cho
nhất quán với convention `/api/v1/*` sẵn có).

**PHASE8-ISSUE-05 → 12** (Part 2): CI/CD path-based trigger theo từng tầng;
Postgres service container cho Integration Test; build Docker image (không
push) tách khỏi push GHCR (chỉ tag `v*`/thủ công); dummy env cho `next
build`; `dotnet format`/`ruff` làm lint (không chặn ngay lần đầu, gỡ
`continue-on-error` sau khi chạy 1 lần cục bộ).

**PHASE8-ISSUE-13 → 17** (Part 3): chọn AWS làm phương án chính (ECS Fargate
+ RDS + ALB + Secrets Manager), có phụ lục Azure tương đương; Next.js BFF
buộc chạy như service (không static hosting thuần); cố ý KHÔNG làm Multi-AZ/
autoscaling/WAF/CDN (out of scope đồ án).

#### Part 1 — Dockerize
3 Dockerfile multi-stage (`backend.Dockerfile`, `frontend.Dockerfile`,
`evaluation.Dockerfile` — non-root user, `HEALTHCHECK` riêng từng service) +
`docker-compose.yml` đầy đủ 4 service (network chung, volume Postgres,
`depends_on: condition: service_healthy`) + `.env.example` + `.dockerignore`.

**🔧 Hotfix #1 (Critical, đã sửa)** — `ERR_CONNECTION_REFUSED :5001` ở toàn
bộ trang Admin (`/admin/tests`, `/admin/exams`): `NEXT_PUBLIC_BACKEND_URL`
bị Next.js inline vào bundle JS **lúc `next build`**, không đọc lại lúc
container chạy — `frontend.Dockerfile` gốc chỉ set biến này ở
`docker-compose.yml` `environment:` (runtime), quá muộn. **Fix:** thêm
`ARG`/`ENV NEXT_PUBLIC_BACKEND_URL` trước `RUN npm run build` trong
Dockerfile, thêm `build.args` tương ứng trong compose; đổi scheme
`https://`→`http://` (backend container không có TLS cert). **Lưu ý vận
hành:** đổi giá trị này bắt buộc phải `docker compose build frontend` lại,
không chỉ restart.

**🔧 Hotfix #2** — `/admin/evaluation` toàn bộ 502 Bad Gateway: tài khoản
RATER `eval-service` (evaluation-service dùng để tự login vào backend,
PHASE5-ISSUE-01) chưa tồn tại trong Postgres mới của Docker Compose — không
phải lỗi hạ tầng, chỉ thiếu bước setup dữ liệu ban đầu. Xử lý tạm thời: tạo
tay qua `POST /auth/register-staff`.

**🔧 Hotfix #3 (PHASE8-ISSUE-18, cải tiến lâu dài cho Hotfix #2)** — mở rộng
`DbSeeder` (Phase 1 Part 3, vốn chỉ seed ADMIN) thêm method
`SeedEvalServiceAsync` seed luôn tài khoản RATER `eval-service` khi backend
khởi động nếu `SeedEvalService:Username/Email/Password` được cấu hình —
dùng lại đúng giá trị `EVAL_RATER_USERNAME`/`EVAL_RATER_PASSWORD` (single
source of truth với evaluation-service, tránh lệch credential). Thêm đúng 1
dòng gọi trong `Program.cs`, cùng scope với `SeedAdminAsync` đã có. Idempotent
(check theo Username cụ thể, không theo Role — vì có thể đã có RATER khác
do người thật tạo).

**File:** `docker/{backend,frontend,evaluation}.Dockerfile`,
`docker/docker-compose.yml`, `docker/.env.example`, `docker/dockerignore-root`
(đổi tên thành `.dockerignore` ở root), `Toeic.Infrastructure/Persistence/
DbSeeder.cs` (sửa), `Toeic.Api/Program.cs` (sửa 1 dòng).

#### Part 2 — CI/CD (GitHub Actions)
5 workflow: `backend-ci.yml` (dotnet restore/format/build/test + Postgres
service container), `frontend-ci.yml` (npm ci/eslint/tsc/build, dummy env
cho build), `evaluation-ci.yml` (pip/ruff/pytest), `docker-build.yml` (build
cả 3 image, không push, trigger mỗi push main), `docker-publish.yml` (push
GHCR, chỉ tag `v*` hoặc thủ công). Đây là lần đầu tiên code .NET Phase 1→6
được compile/test tự động ngoài máy Dragon (sandbox Claude không có quyền
`nuget.org`).

**File:** `.github/workflows/{backend-ci,frontend-ci,evaluation-ci,
docker-build,docker-publish}.yml`.

#### Part 3 — Cloud Deployment (tài liệu kiến trúc minh hoạ)
AWS làm phương án chính: ALB path-based routing → 3 ECS Fargate service
(private subnet) → RDS PostgreSQL single-AZ; Secrets Manager cho toàn bộ
secret; NAT Gateway cho outbound LLM API call; ECR cho image; CloudWatch
Logs. Bảng ánh xạ trực tiếp từ `docker-compose.yml` sang từng AWS resource.
Phụ lục ánh xạ tương đương Azure. Cố ý loại trừ Multi-AZ/autoscaling/WAF/CDN/
Blue-Green (out of scope đồ án, mục 22 prompt gốc). Chi phí ước tính tối
thiểu + khuyến nghị tắt tài nguyên khi không demo. **Chưa deploy thật** —
đúng phạm vi "minh hoạ" đã chốt ở Phase 0 mục M/N.

**File:** `PHASE8_PART3_CLOUD_DEPLOYMENT.md`.

#### ✅ Xác nhận hoàn tất (11/09/2026)
Dragon xác nhận: **"ĐÃ BUILD, RUN, TEST ĐƯỢC MỌI THỨ TỪ PHASE 1 ĐẾN 8 VÀ ĐÃ
CHẠY BÌNH THƯỜNG."** `docker compose up -d --build` chạy đúng cả 4 service
sau khi áp dụng đủ 3 hotfix ở Part 1. Không còn vấn đề mở nào từ Phase 8.

**Toàn bộ roadmap 0→8 của dự án TOEIC Scoring Support System đã hoàn tất.**

---

## Phụ lục — Danh sách toàn bộ file `PHASE*_INTEGRATION_GUIDE.md` / hotfix đã phát sinh

Để tiện tra cứu khi cần đối chiếu lại (không lặp nội dung, chỉ liệt kê tên):

```
PHASE3_PART1_INTEGRATION_GUIDE.md   PHASE3_PART2_NOTES.md
PHASE3_PART3_INTEGRATION_GUIDE.md   PHASE3_PART4_INTEGRATION_GUIDE.md
PHASE4_PART1_INTEGRATION_GUIDE.md   PHASE4_PART2_INTEGRATION_GUIDE.md
PHASE5_PART1_INTEGRATION_GUIDE.md   PHASE5_PART2_INTEGRATION_GUIDE.md
PHASE5_PART3_INTEGRATION_GUIDE.md
PHASE6_PART1_INTEGRATION_GUIDE.md   PHASE6_PART2_INTEGRATION_GUIDE.md
PHASE6_PART3_INTEGRATION_GUIDE.md
PHASE7_PART1_INTEGRATION_GUIDE.md   PHASE7_PART2_INTEGRATION_GUIDE.md
PHASE7_PART3_INTEGRATION_GUIDE.md   PHASE7_PART4_INTEGRATION_GUIDE.md
PHASE7_PART5_INTEGRATION_GUIDE.md   PHASE7_PART6_INTEGRATION_GUIDE.md
PHASE7_HOTFIX_2026-09-06.md         PHASE7_HOTFIX_2_2026-09-06.md
PHASE8_PART1_INTEGRATION_GUIDE.md   PHASE8_PART1_HOTFIX.md
PHASE8_PART1_AUTOSEED_FINAL.md      PHASE8_PART2_INTEGRATION_GUIDE.md
PHASE8_PART3_CLOUD_DEPLOYMENT.md
```
