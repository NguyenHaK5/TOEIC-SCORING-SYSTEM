# TOEIC Scoring Support System — Frontend (Phase 7)

Next.js 16 (App Router) + TypeScript + Tailwind CSS v4.

- **Part 1 — Foundation:** `PHASE7_PART1_INTEGRATION_GUIDE.md`
- **Part 2 — Admin Content Management:** `PHASE7_PART2_INTEGRATION_GUIDE.md`
- **Part 3 — Candidate Exam Room:** `PHASE7_PART3_INTEGRATION_GUIDE.md`
- **Part 4 — Candidate Result/History/Analytics:** `PHASE7_PART4_INTEGRATION_GUIDE.md`
- **Part 5 — Rater:** `PHASE7_PART5_INTEGRATION_GUIDE.md`
- **Part 6 — Admin Dashboard & Evaluation:** `PHASE7_PART6_INTEGRATION_GUIDE.md` *(Part cuối cùng của Phase 7)*

## ✅ Đã build/lint/test thật trong sandbox (khác hoàn toàn với backend .NET)

Không giống backend .NET (sandbox không có `dotnet` SDK/NuGet), mạng của
sandbox này **có** truy cập `registry.npmjs.org`, nên toàn bộ phần này đã
được:

- `npm run build` — build production **thành công**, 0 lỗi (33 route).
- `npx eslint .` — **0 lỗi, 0 warning**.
- Chạy thử `next dev` thật + `curl`/Playwright để xác nhận **middleware
  RBAC vẫn hoạt động đúng** qua tất cả các Part, và **không route nào
  SSR-crash** khi backend .NET/evaluation-service chưa chạy.
- **Exam Room (Part 3):** dùng Playwright chặn response (`page.route`) để
  giả lập backend, xác nhận: audio player gộp đúng cho các câu chung 1
  file, đáp án đã lưu trước hiển thị đúng khi tải trang, auto-save debounce
  chỉ gọi API **1 lần** dù gõ liên tục, đồng hồ đếm ngược chạy đúng theo
  `expiresAt`, dialog xác nhận nộp bài hoạt động.
- **Result/History/Analytics (Part 4):** xác nhận Result page **tự chuyển
  Writing Pending → Scored qua polling, không cần F5**; biểu đồ xu hướng
  điểm (recharts) vẽ đúng, đứt đoạn đúng chỗ thiếu dữ liệu.
- **Rater (Part 5):** form chấm điểm sinh đúng input theo Rubric ACTIVE
  giả lập, giữ giá trị khi điền; trang so sánh AI vs Human tô màu đúng
  ngưỡng mức lệch.
- **Dashboard & Evaluation (Part 6):** proxy `/api/evaluation/*` chặn đúng
  request thiếu Authorization (401) và xử lý mềm dẻo khi evaluation-service
  không chạy (502 JSON, không crash); Metrics hiển thị "Không đủ dữ liệu"
  thay vì số 0 khi `n` quá nhỏ; export CSV lỗi → toast, không crash trang.
- Chụp ảnh màn hình các trang chính để tự kiểm tra thiết kế trước khi giao.

**Điều KHÔNG thể test trong sandbox này:** luồng CRUD/thi/chấm/đánh giá
thật với backend .NET + evaluation-service, vì sandbox không chạy được
`dotnet` và không có instance evaluation-service thật. Xem mục "Việc BẮT
BUỘC" trong từng `PHASE7_PARTn_INTEGRATION_GUIDE.md`. **Đặc biệt ở Part
3:** endpoint Candidate xem danh sách Exam (`GET /exams/available`) là giả
định CHƯA XÁC NHẬN backend có tồn tại hay không (PHASE7-ISSUE-10) — đây
vẫn là điểm chặn quan trọng nhất của toàn bộ Phase 7, xem integration
guide Part 3 mục 6.

## Phase 7 (Frontend) đã hoàn tất 6/6 Part

Xem `PHASE7_PART6_INTEGRATION_GUIDE.md` mục cuối để biết danh sách việc
cần làm trước khi chuyển sang Phase 8 (DevOps).

## Cách chạy

```bash
cd frontend
npm install

cp .env.example .env.local
# Sửa .env.local: BACKEND_URL và NEXT_PUBLIC_BACKEND_URL trỏ đúng cổng
# Toeic.Api đang chạy local.

npm run dev
# http://localhost:3000
```

## Kiểm tra nhanh Part 1 — Auth/RBAC (khi có backend .NET thật đang chạy)

1. Vào `http://localhost:3000/` → redirect `/login` (middleware).
2. Đăng ký 1 tài khoản Candidate mới ở `/register` → tự động đăng nhập →
   vào `/candidate`.
3. Mở tab ẩn danh, cố vào `http://localhost:3000/admin` trực tiếp (chưa
   đăng nhập) → redirect `/login?next=/admin`.
4. Đăng nhập bằng tài khoản Candidate vừa tạo, thử vào `/admin` → redirect
   `/unauthorized`.
5. Đăng xuất → cookie bị xoá, vào lại `/candidate` → redirect `/login`.
6. F5 (reload) khi đang ở `/candidate` đã đăng nhập → `AuthProvider` tự
   khôi phục phiên qua `/api/auth/refresh` (không bị đá về `/login`).

## Kiểm tra nhanh Part 2 (khi có backend .NET thật đang chạy, đăng nhập ADMIN)

1. `/admin/tests/new` → tạo 1 Test (FORM-A).
2. Vào chi tiết Test vừa tạo → "Thêm câu hỏi" → tạo vài câu Listening
   (nhớ điền Audio URL), Reading (Multiple Choice, thêm đủ option + đúng 1
   option `Đúng`).
3. Bấm "Publish" — nếu thiếu dữ liệu bắt buộc (vd thiếu Audio URL, thiếu
   option đúng), hệ thống liệt kê đủ lỗi để sửa.
4. `/admin/exams/new` → tạo 1 Exam.
5. Vào chi tiết Exam → "Gán bộ đề" → chọn Test vừa publish.
6. Bấm "Lên lịch" (chuyển sang SCHEDULED) — chỉ thành công nếu đã gán đủ
   Test active.
7. `/admin/rubrics/new` → tạo 1 RubricSet cho Q8 (Opinion Essay) → thêm vài
   tiêu chí (Grammar, Vocabulary, ...) với tổng trọng số = 1.00 → "Kích
   hoạt (Active)".

## Kiểm tra nhanh Part 3 (khi có backend .NET thật đang chạy, đăng nhập CANDIDATE)

**Điều kiện tiên quyết:** endpoint `GET /exams/available` (hoặc tương
đương) phải tồn tại ở backend — xem PHASE7-ISSUE-10 trong
`PHASE7_PART3_INTEGRATION_GUIDE.md`. Nếu chưa có, sửa
`lib/candidate/candidate-exams-api.ts` trỏ đúng endpoint thật trước.

1. Đăng nhập Candidate → `/candidate/exams` → thấy Exam đã `SCHEDULED`
   hoặc `ONGOING`.
2. Bấm vào 1 Exam → "Bắt đầu / Tiếp tục làm bài" → chuyển tới
   `/exam-room/<sessionId>` với đồng hồ đếm ngược đang chạy.
3. Chọn đáp án trắc nghiệm ở tab Nghe/Đọc → chuyển tab → quay lại → xác
   nhận đáp án vẫn còn (đã lưu).
4. Gõ vào ô tự luận ở tab Viết → đợi ~1.5s → thấy "Đã lưu lúc HH:mm".
5. Reload cả trang (F5) → xác nhận toàn bộ câu trả lời đã lưu được khôi
   phục, đồng hồ tiếp tục đếm đúng (không reset).
6. Bấm "Nộp bài" → xác nhận trong dialog → thấy màn hình "Đã nộp bài".
7. Thử vào lại `/exam-room/<sessionId>` sau khi đã nộp → thấy màn hình
   "Phiên thi đã kết thúc", không cho sửa câu trả lời nữa.

## Kiểm tra nhanh Part 4 (khi có backend .NET thật đang chạy, đăng nhập CANDIDATE)

1. Sau khi nộp bài ở Part 3 → bấm "Xem kết quả" → thấy Listening/Reading
   có điểm ngay, Writing hiện "Đang chấm".
2. Đợi background job Phase 3 Part 4 chấm xong (không cần F5) → xác nhận
   trang tự cập nhật điểm + feedback + breakdown theo tiêu chí Writing.
3. Vào `/candidate/history` → thấy phiên vừa nộp, bấm "Xem kết quả" →
   quay lại đúng trang Result.
4. Vào `/candidate/analytics` → xác nhận biểu đồ xu hướng điểm, thanh
   accuracy Listening/Reading, card điểm trung bình theo tiêu chí Writing
   đều có dữ liệu (cần ít nhất vài phiên đã nộp + chấm xong để có ý nghĩa).

## Kiểm tra nhanh Part 5 (khi có backend .NET thật đang chạy, đăng nhập RATER)

1. `/rater/writing-answers` (tab "Chờ chấm") → thấy các bài Writing đã
   submit, kèm điểm AI hiện hành.
2. Bấm "Chấm điểm" 1 bài → xem đánh giá AI (điểm/feedback/breakdown tiêu
   chí) + nội dung bài làm.
3. Điền điểm cho từng Criterion theo Rubric ACTIVE, nhập nhận xét → "Lưu
   điểm chấm".
4. Quay lại danh sách → chuyển tab "Đã chấm" → xác nhận bài vừa chấm xuất
   hiện với điểm Human.
5. Vào `/rater/comparison` → xác nhận thấy cặp AI/Human vừa tạo, sắp xếp
   theo mức lệch.
6. (Tuỳ chọn, nối với Part 4) Đăng nhập lại bằng tài khoản Candidate của
   bài vừa chấm → vào Result → xác nhận điểm hiển thị là điểm Human vừa
   chấm (không phải điểm AI) — đúng quy tắc `IsFinal` ưu tiên.

## Kiểm tra nhanh Part 6 (khi có backend .NET + evaluation-service thật đang chạy, đăng nhập ADMIN)

1. `/admin/dashboard` → xác nhận số liệu tổng quan + thống kê điểm 3 phần
   + top lỗi Writing khớp dữ liệu thật trong Postgres.
2. `/admin/evaluation` → xác nhận Dataset hiển thị đúng tổng số cặp AI-
   Human đã có (từ Part 5).
3. Đổi filter `questionType`/`Ngưỡng đồng ý` ở Metrics → xác nhận số liệu
   thay đổi theo.
4. Đổi `Lệch tối thiểu`/`Top` ở Report → xác nhận danh sách case lọc đúng.
5. Bấm "Xuất CSV" → mở file bằng Excel, xác nhận encoding UTF-8 hiển thị
   đúng tiếng Việt trong tên thí sinh.

