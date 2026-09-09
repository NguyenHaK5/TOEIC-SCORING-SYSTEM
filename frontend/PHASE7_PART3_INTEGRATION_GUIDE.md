# PHASE 7 – PART 3: CANDIDATE — EXAM ROOM — Integration Guide

## 1. Mục tiêu Part này

Đường găng quan trọng nhất của toàn bộ Frontend: Exam list/detail cho
Candidate, Start/Resume session, và **Phòng thi trực tuyến** (Listening
audio + trắc nghiệm, Reading trắc nghiệm, Writing Q1–5/Q6–7/Q8 với ảnh +
required words), timer đồng bộ tuyệt đối với `expiresAt` do server trả về
(mục 16 prompt gốc), auto-save debounce, submit có xác nhận + tự động nộp
khi hết giờ.

**Demo cuối Part:** Candidate login → chọn Exam → làm đủ 3 phần → submit,
đúng qua UI thật (không cần curl/Swagger).

## 2. Rà soát trước khi code

### PHASE7-ISSUE-10 — Chưa có endpoint xác nhận cho Candidate xem danh sách Exam khả dụng
**Problem:** README.md gốc (mục "Part 3 — Exam Management CRUD") ghi rõ
**toàn bộ** endpoint trong `ExamsController` — bao gồm cả `GET /exams` và
`GET /exams/{id}` — yêu cầu `[Authorize(Roles=ADMIN)]`. Nhưng luồng nghiệp
vụ bắt buộc (mục 30 prompt gốc, PHASE0 mục "Main Business Flows") lại cần
Candidate xem được danh sách kỳ thi để chọn trước khi tạo `ExamSession`.
Không có endpoint nào khác trong toàn bộ tài liệu đã cung cấp phục vụ mục
đích này.
**Severity:** High — đây là **điểm vào** của toàn bộ Part 3; nếu endpoint
không tồn tại, Candidate không có cách nào biết `examId` để gọi
`POST /exam-sessions`.
**Impact:** Chặn hoàn toàn luồng demo end-to-end nếu backend thật chưa có
endpoint tương ứng.
**Root Cause:** Khoảng trống giữa specification (Admin CRUD) và luồng
nghiệp vụ Candidate — không có trong tài liệu backend đã giao ở các Phase
trước, có thể do lúc code Phase 1 Part 3 chưa tính tới góc nhìn Candidate.
**Recommended Solution:** Frontend gọi endpoint **giả định**
`GET /exams/available` (Authorize bất kỳ user đã đăng nhập, hoặc riêng
CANDIDATE), trả về các Exam ở trạng thái `SCHEDULED`/`ONGOING` — đủ thông
tin để hiển thị danh sách + chi tiết (không cần endpoint chi tiết riêng,
Frontend tái sử dụng dữ liệu từ danh sách). Cô lập hoàn toàn trong
`lib/candidate/candidate-exams-api.ts` — nếu backend dùng path khác, hoặc
đã có sẵn từ trước mà tài liệu chưa nhắc tới, chỉ cần sửa 1 dòng.
**Alternative:** Bỏ qua trang danh sách, để Candidate luôn cần được cấp
thẳng link `/candidate/exams/{examId}` (vd qua email/thông báo ngoài hệ
thống). Né được việc phải thêm endpoint mới nhưng trải nghiệm rất kém và
không đúng luồng "chọn Exam" đã mô tả trong đề bài gốc.
**Decision:** Dùng endpoint giả định `GET /exams/available`, đúng tinh
thần Demo First — không chặn tiến độ code Frontend, sửa sau nếu cần.
**Reason:** Đây rõ ràng là một khoảng trống backend cần được Dragon xác
nhận/bổ sung (có thể chỉ cần thêm `[AllowAnonymous]` hoặc
`[Authorize]` không giới hạn Role cho 2 action GET trong `ExamsController`
hiện có, filter theo Status ở tầng Service — không nhất thiết phải là
endpoint hoàn toàn mới, xem mục 6).

### PHASE7-ISSUE-11 — Exam Room cần giao diện tối giản, tách khỏi RoleShell
**Problem:** `RoleShell` (Part 1) bọc toàn bộ `/candidate/**` bằng top bar +
sidebar nav — phù hợp cho trang quản lý thông thường nhưng KHÔNG phù hợp
cho phòng thi có tính thời gian: sidebar có thể khiến Candidate bấm nhầm
điều hướng ra khỏi bài thi, nút "Đăng xuất" luôn hiện sẵn dễ bấm nhầm.
**Severity:** Low (UX, không phải lỗi chức năng).
**Decision:** Route phòng thi đặt tại `/exam-room/{sessionId}` — **NGOÀI**
prefix `/candidate` — với layout riêng, tối giản (chỉ Wordmark + đồng hồ +
nút Nộp bài, không sidebar/logout). `proxy.ts` cập nhật để `ROLE_PREFIXES`
hỗ trợ nhiều prefix/role (`CANDIDATE` giờ gồm cả `/candidate` và
`/exam-room`), đảm bảo vẫn được bảo vệ đúng quyền dù nằm ngoài
`/candidate`.
**Reason:** Giảm rủi ro thao tác nhầm trong lúc làm bài có tính thời gian,
đúng tinh thần mục 16 prompt gốc (bảo vệ tính toàn vẹn phiên thi) mà không
cần thêm cơ chế khoá route phức tạp.

### PHASE7-ISSUE-12 — Shape JSON của `GET .../questions` và body `answers` là giả định
**Problem:** README.md mô tả 2 endpoint này bằng lời + 1 ví dụ `curl` cho
`answers` (chỉ có case trắc nghiệm: `{"questionId":"...","selectedOptionId":"..."}`),
KHÔNG có ví dụ JSON đầy đủ cho response câu hỏi (bao gồm việc câu trả lời
đã lưu được đặt ở đâu) hay case Writing (`answerText`).
**Severity:** Medium.
**Decision:** Giả định câu trả lời đã lưu nằm ngay trên từng object Question
(`selectedOptionId`/`answerText`), và endpoint `answers` dùng chung 1 path
cho cả 2 loại câu, chỉ khác field gửi lên. Toàn bộ giả định cô lập trong
`types/exam-session.ts` (đã comment rõ) — không lan ra UI nếu sai, chỉ cần
map lại đúng field.
**Reason:** Đơn giản nhất, khớp với cách README mô tả UPSERT 1 endpoint
duy nhất cho auto-save.

### PHASE7-ISSUE-13 — React Compiler cấm mutate biến cục bộ trong thân component
**Problem:** Logic gộp audio player cho các câu hỏi dùng chung `audioUrl`
(gán lại biến `lastAudioUrl` trong `.map()`) bị React Compiler
(`react-hooks/immutability`, bật sẵn ở Next.js 16) báo lỗi cứng, không chỉ
warning.
**Severity:** Low.
**Decision:** Tách logic gộp thành 1 hàm thuần (`groupByAudio`) đặt NGOÀI
component — React Compiler chỉ áp quy tắc bất biến cho thân component/hook,
không áp cho hàm module-level thông thường.
**Reason:** Giữ `npx eslint .` sạch 100% xuyên suốt dự án, đúng pattern đã
dùng ở PHASE7-ISSUE-09 (Part 2).

## 3. Kiến trúc

```
app/candidate/exams/            list (gọi GET /exams/available — giả định)
app/candidate/exams/[examId]/   chi tiết + nút "Bắt đầu / Tiếp tục làm bài"
app/exam-room/[sessionId]/      PHÒNG THI — NGOÀI prefix /candidate (PHASE7-ISSUE-11)

lib/candidate/
├── candidate-exams-api.ts   listAvailable()                    (PHASE7-ISSUE-10)
├── exam-session-api.ts      start/get/getQuestions/saveAnswer/submit
├── use-countdown.ts         đếm ngược dựa trên expiresAt server, không tự tính lại
└── use-save-answer.ts       mutation auto-save + optimistic update cache

components/candidate/
├── timer-display.tsx        đồng hồ đếm ngược (đỏ khi còn ≤5 phút)
├── question-palette.tsx     lưới số câu, click để nhảy tới, tô theo đã/chưa trả lời
├── audio-block.tsx          <audio> player
├── mc-question-card.tsx     câu trắc nghiệm — lưu NGAY khi chọn (không debounce)
├── writing-question-card.tsx câu tự luận — lưu SAU 1.2s ngừng gõ (debounce)
└── section-panel.tsx        gộp danh sách câu hỏi 1 phần thi, gộp audio liên tiếp
```

### Timer — server là nguồn xác thực duy nhất
`useCountdown` nhận `expiresAt` (ISO) từ response `GET/POST /exam-sessions`,
tự tính `remaining = expiresAt - Date.now()` mỗi giây — KHÔNG bao giờ tự
cộng thêm thời gian hay suy ra deadline từ phía client. Khi về 0, tự động
gọi `submit` — đồng thời `GET /exam-sessions/{id}` được poll lại mỗi 20s để
phát hiện trường hợp backend đã tự lazy-auto-submit phiên trong lúc tab
không hoạt động (README.md Phase 1 Part 4: "mọi action chạm vào phiên thi
đều kiểm tra `UtcNow >= ExpiresAt` trước").

### Auto-save
- Trắc nghiệm (`McQuestionCard`): lưu **ngay** khi chọn — hành động rời rạc
  (1 click), không cần debounce.
- Tự luận (`WritingQuestionCard`): debounce 1200ms sau khi ngừng gõ, dùng
  `setTimeout` trong event handler (KHÔNG dùng `useEffect` theo dõi state —
  tránh đúng cái bẫy `set-state-in-effect` đã gặp ở Part 2).
- Cả 2 dùng chung `useSaveAnswer` — cập nhật cache React Query ngay
  (optimistic) để `QuestionPalette` phản ánh tức thì, rollback nếu lỗi.

## 4. Đã tự verify trong sandbox

| Kiểm tra | Kết quả |
|---|---|
| `npm run build` (production) | ✅ Thành công, 0 lỗi (25 route) |
| `npx eslint .` | ✅ 0 lỗi, 0 warning |
| RBAC: `/exam-room/*` yêu cầu đúng CANDIDATE (ADMIN/RATER → `/unauthorized`) | ✅ curl test |
| RBAC: `/candidate/exams/*` không hồi quy | ✅ curl test |
| Giao diện Exam Room (Listening — audio gộp đúng, trắc nghiệm, palette) | ✅ Chụp ảnh qua Playwright + mock response |
| Giao diện Exam Room (Writing — ảnh, required words, textarea, đếm từ) | ✅ Chụp ảnh |
| Auto-save debounce: gõ liên tục chỉ gọi API lưu **1 lần** sau khi ngừng | ✅ Verify bằng route interception (đếm số lần gọi) |
| Trạng thái "Đã lưu lúc HH:mm" + palette cập nhật tức thì (optimistic) | ✅ |
| Dialog xác nhận "Nộp bài?" | ✅ |
| Đồng hồ đếm ngược chạy đúng, đổi màu đỏ khi ≤5 phút | ✅ (logic `isLow`, chưa test trực quan mốc 5 phút vì tốn thời gian chờ thật) |

**Lưu ý về test bằng mock:** Vì sandbox không có backend .NET thật, các
kiểm tra giao diện/auto-save/debounce ở trên dùng Playwright chặn
(`page.route`) và trả JSON giả lập đúng theo shape đã định nghĩa ở
`types/exam-session.ts` — xác nhận ĐÚNG hành vi Frontend, nhưng KHÔNG xác
nhận shape JSON giả định đó khớp với backend thật (xem PHASE7-ISSUE-12).

## 5. Chưa làm ở Part này (đúng phạm vi, để Part sau)

- Trang Result đầy đủ sau khi nộp bài — Part 4 (Result/History/Analytics).
  Part 3 chỉ có màn hình xác nhận "Đã nộp bài" tối giản.
- Xử lý trường hợp mất mạng khi đang auto-save (retry/queue offline) — chưa
  có trong yêu cầu gốc, có thể bổ sung nếu Dragon cần.
- Scroll-spy tự động highlight câu đang xem trong palette khi cuộn (hiện
  chỉ highlight câu vừa bấm) — tối ưu UX nhỏ, không chặn demo.
- Rater/Admin Dashboard, Evaluation — Part 5/6.

## 6. Việc BẮT BUỘC Dragon cần làm trước khi coi Part 3 là "Done"

1. **Xác nhận/bổ sung endpoint cho PHASE7-ISSUE-10** — đây là việc quan
   trọng nhất, chặn toàn bộ luồng demo nếu chưa có. Kiểm tra xem
   `Toeic.Api` đã có cách nào cho Candidate liệt kê Exam chưa (có thể đã
   có mà tài liệu giao cho sandbox này chưa nhắc tới); nếu chưa, cần thêm.
2. **Chạy `npm run dev` với backend .NET Phase 1 Part 4 thật đang chạy
   song song**, thử đúng kịch bản: login Candidate → `/candidate/exams` →
   chọn 1 Exam đã `ONGOING` → bắt đầu → làm đủ 3 phần → submit.
3. **Đối chiếu shape thật của `GET .../questions`** (PHASE7-ISSUE-12) —
   đặc biệt xác nhận câu trả lời đã lưu nằm ở đâu trong response.
4. **Đối chiếu body thật của `POST .../answers` cho câu Writing** — hiện
   giả định `{questionId, answerText}`.
5. **Test refresh giữa chừng bài thi** — reload trang `/exam-room/{id}`,
   xác nhận câu trả lời đã lưu được khôi phục đúng, đồng hồ tiếp tục đếm
   đúng từ `expiresAt` (không reset).
6. **Test hết giờ thật** (rút ngắn `writingDurationSeconds` của 1 Test test
   xuống vài chục giây để test nhanh) — xác nhận tự động submit đúng lúc
   về 0, không cho sửa câu trả lời sau đó.
