# PHASE 7 – PART 1: FOUNDATION — Integration Guide

## 1. Mục tiêu Part này

Dựng phần lõi của Frontend mà **mọi Part sau (2→6) đều phụ thuộc vào**:
scaffold Next.js, cơ chế Auth BFF (httpOnly cookie cho refresh token,
access token trong bộ nhớ), RBAC middleware theo route group, layout
riêng cho 3 role, và `apiClient` dùng chung cho mọi lời gọi nghiệp vụ tới
backend .NET ở các Part sau.

**Demo cuối Part** (đã tự verify được phần không cần backend thật — xem
mục 6): Login đúng role → vào đúng layout tương ứng; sai role bị chặn.

## 2. Kiến trúc

```
Browser
  │
  ├── /login, /register (Server Component trang + Client Component form)
  │     → gọi POST /api/auth/login | /api/auth/register  (Route Handler, cùng origin)
  │
  ├── /api/auth/*  (Route Handler — BFF, chạy trên server Next.js)
  │     → gọi thẳng Toeic.Api (.NET) qua BACKEND_URL (server-only)
  │     → set cookie httpOnly: refresh_token, session_role
  │     → trả JSON { accessToken, user } cho client (KHÔNG có refreshToken)
  │
  ├── proxy.ts (Middleware/Proxy, Edge runtime)
  │     → đọc cookie refresh_token + session_role
  │     → RBAC theo prefix route: /admin, /rater, /candidate
  │
  ├── AuthProvider (Client, bọc toàn app trong app/layout.tsx)
  │     → mount: gọi /api/auth/refresh để khôi phục phiên sau F5
  │     → giữ accessToken + user trong Zustand store (bộ nhớ, không localStorage)
  │     → tự lên lịch refresh trước khi accessToken hết hạn (dựa vào claim `exp`)
  │
  └── apiClient (lib/api-client.ts) — dùng ở Part 2-6
        → gọi THẲNG Toeic.Api từ browser với Authorization: Bearer <accessToken>
        → 401 giữa chừng → tự refresh 1 lần → retry → nếu vẫn fail → về /login
```

## 3. Rà soát trước khi code (tiếp nối PHASE7-ISSUE-01 → 04 đã có trong
`phase-tracker.md`)

### PHASE7-ISSUE-05 — Middleware RBAC không đọc được access token thật
**Problem:** Theo PHASE7-ISSUE-01 (đã chốt), `accessToken` chỉ sống trong
bộ nhớ JS phía client (Zustand), không có trong bất kỳ cookie nào — nhưng
Next.js Middleware/Proxy chạy ở Edge runtime, **trước khi** React/JS phía
client mount, nên không thể đọc state đó để quyết định cho qua route
`/admin`, `/rater`, `/candidate` hay không.
**Severity:** Medium — nếu không xử lý, middleware chỉ có thể kiểm tra
"đã đăng nhập hay chưa" (dựa vào có cookie `refresh_token` hay không) mà
không biết role, nghĩa là 1 Candidate gõ thẳng URL `/admin` vẫn "vào được"
tới khi Client Component render và có thể tự kiểm tra role để chặn — chậm,
và có khoảnh khắc UI Admin bị lộ ra trước khi kịp chặn.
**Root Cause:** access token (đúng decision PHASE7-ISSUE-01) chủ đích
không đặt vào cookie để giảm bề mặt tấn công XSS.
**Recommended Solution:** Thêm một cookie httpOnly **thứ hai**, `session_role`,
chỉ chứa duy nhất giá trị role (vd `"ADMIN"`) — không phải secret, không
phải access token, không dùng để gọi API nghiệp vụ. BFF (`/api/auth/login`,
`/refresh`) set cookie này cùng lúc với `refresh_token`. Middleware đọc
`session_role` để quyết định route, hoàn toàn tách biệt khỏi authorization
thật.
**Alternative:** Không có middleware RBAC, để mỗi Client Component tự kiểm
tra `useAuthStore().user.role` rồi `router.replace()` nếu sai — đơn giản
hơn nhưng có "flash" UI sai role trước khi redirect (UX kém, và về lý
thuyết vẫn lộ cấu trúc UI Admin trong 1 khoảnh khắc).
**Decision:** Cookie `session_role` httpOnly, đọc ở middleware.
**Reason:** Đây là lớp **routing/UX**, KHÔNG phải biên giới bảo mật cuối
cùng — biên giới bảo mật thật vẫn là backend .NET validate JWT Bearer
token + `[Authorize(Roles=...)]` trên từng endpoint (đã build/test ở Phase
1-6). Nếu ai đó tự sửa cookie `session_role` bằng tay để vượt middleware,
họ vẫn không có accessToken hợp lệ để gọi bất kỳ API nghiệp vụ nào thành
công — middleware chỉ tránh flash UI sai, không phải điểm chốt an ninh.

### PHASE7-ISSUE-06 — Next.js 16 đổi tên convention `middleware.ts` → `proxy.ts`
**Problem:** Bản scaffold mới nhất của `create-next-app` (Next.js 16.3.4,
tại thời điểm code Part này) cảnh báo `middleware.ts` là convention cũ,
khuyến nghị đổi tên file thành `proxy.ts` (cùng nội dung, đổi tên export
`middleware` → `proxy`).
**Severity:** Low.
**Decision:** Dùng `proxy.ts` (convention mới) để tránh cảnh báo
deprecation ngay từ đầu — không phải sửa lại sau. Nếu Dragon dùng version
Next.js cũ hơn khi cài đặt và gặp lỗi do convention chưa được hỗ trợ, đổi
tên ngược lại thành `middleware.ts` + export `middleware` là đủ (logic bên
trong không đổi).

## 4. Design tokens (tại sao không dùng theme mặc định)

Áp dụng đúng quy trình "2 pass" của frontend-design skill: lên token trước,
review có bị trùng các "tell" AI-generated hay không, rồi mới code.

- **Màu nền `paper` #EEF2F5** — cố ý chọn tông lạnh (xám-xanh nhạt) thay vì
  cream ấm gần #F4F1EA (tell phổ biến nhất). **`ink` #16213A** làm chữ/nền
  tối — không dùng đen thuần.
- **Accent `teal` #1C7A72** cho hành động chính (không dùng cam đất/terracotta
  #D97757 — tell thứ hai). **`gold` #B8862E** dùng rất tiết chế, dành riêng
  cho điểm số/badge ở các Part sau (chưa xuất hiện ở Part 1).
- **Type:** "Source Serif 4" (tiêu đề, cảm giác "văn bản chính thức" của một
  cơ quan khảo thí) + "IBM Plex Sans" (UI/form, rõ ràng — không dùng Inter
  mặc định).
- **Hoạ tiết trang trí:** lưới chấm mô phỏng phiếu trả lời trắc nghiệm
  (`components/layout/answer-sheet-pattern.tsx`) — bám sát chủ đề "chấm
  điểm khách quan bằng Answer Key" của chính hệ thống, thay cho gradient/
  blob trang trí chung chung.
- **Tránh:** không dùng eyebrow label viết hoa toàn bộ phía trên tiêu đề,
  không numbered marker giả (01/02/03) vì nội dung không phải một chuỗi
  bước.

Toàn bộ token khai báo tại `app/globals.css` (`@theme inline`, cú pháp
Tailwind v4) — Part 2 trở đi tái sử dụng nguyên token này (`bg-paper`,
`text-ink`, `bg-teal`, `font-serif`, ...), không định nghĩa lại.

## 5. Danh sách file mới

```
frontend/
├── .env.example
├── README.md
├── proxy.ts                                  # RBAC middleware (PHASE7-ISSUE-05/06)
├── app/
│   ├── globals.css                           # Design tokens (mục 4)
│   ├── layout.tsx                            # Root layout + AuthProvider
│   ├── page.tsx                              # "/" — fallback, luôn bị proxy.ts redirect trước
│   ├── login/
│   │   ├── page.tsx                          # Split-screen hero + form
│   │   └── login-form.tsx                    # Client form
│   ├── register/
│   │   ├── page.tsx
│   │   └── register-form.tsx
│   ├── unauthorized/page.tsx
│   ├── admin/{layout.tsx,page.tsx}           # Shell + placeholder, nav Part 2/6
│   ├── rater/{layout.tsx,page.tsx}           # Shell + placeholder, nav Part 5
│   ├── candidate/{layout.tsx,page.tsx}       # Shell + placeholder, nav Part 3/4
│   └── api/auth/
│       ├── login/route.ts
│       ├── register/route.ts                 # Auto-login sau khi tạo tài khoản
│       ├── refresh/route.ts                  # Rotation — set lại cả 2 cookie
│       └── logout/route.ts
├── components/
│   ├── ui/{button,input,label,form-error}.tsx   # Primitives kiểu shadcn, dùng token riêng
│   ├── layout/{wordmark,answer-sheet-pattern,role-shell}.tsx
│   └── providers/auth-provider.tsx
├── lib/
│   ├── env.ts                # Đọc BACKEND_URL, TLS dev workaround
│   ├── backend-auth.ts       # server-only — gọi thẳng Toeic.Api /auth/*
│   ├── auth-cookies.ts       # helper set/clear 2 cookie httpOnly
│   ├── auth-client.ts        # client-safe — gọi /api/auth/* (BFF của chính app)
│   ├── auth-store.ts         # Zustand — accessToken + user trong bộ nhớ
│   ├── api-client.ts         # Dùng ở Part 2-6 — gọi thẳng Toeic.Api kèm Bearer token
│   ├── jwt.ts                 # decodeJwt (đọc `exp`, không verify)
│   └── utils.ts                # cn() helper
└── types/auth.ts
```

## 6. Đã tự verify được gì trong sandbox (không cần backend .NET)

| Kiểm tra | Kết quả |
|---|---|
| `npm run build` (production) | ✅ Thành công, 0 lỗi |
| `npx eslint .` | ✅ 0 lỗi, 0 warning |
| Middleware: `/admin` chưa đăng nhập | ✅ 307 → `/login?next=%2Fadmin` |
| Middleware: cookie `session_role=CANDIDATE` cố vào `/admin` | ✅ 307 → `/unauthorized` |
| Middleware: cookie `session_role=CANDIDATE` vào đúng `/candidate` | ✅ 200 |
| Middleware: đã đăng nhập (`session_role=ADMIN`) mà vào `/login` | ✅ 307 → `/admin` |
| Middleware: `/` chưa đăng nhập | ✅ 307 → `/login` |
| `POST /api/auth/login` khi backend .NET không phản hồi được | ✅ 502 JSON có kiểm soát, không crash server |
| Giao diện Login (desktop 1440px + mobile 390px) | ✅ Đã chụp ảnh, đúng token/layout đã lên kế hoạch |
| Giao diện Register | ✅ Đã chụp ảnh |

## 7. Việc BẮT BUỘC Dragon cần làm trước khi coi Part 1 là "Done"

1. **Chạy `npm install` + `npm run dev` với backend .NET Phase 1 Part 2
   thật đang chạy song song**, thử đủ luồng ở mục "Kiểm tra nhanh" trong
   `README.md` — đây là phần **duy nhất** sandbox này không tự verify
   được (không có `dotnet` SDK).
2. **Đối chiếu response body thật của `POST /auth/refresh` và
   `POST /auth/logout`** — `lib/backend-auth.ts` giả định request body là
   `{ "refreshToken": "..." }` vì README.md gốc không có ví dụ `curl` cho 2
   endpoint này (chỉ có ví dụ cho `/login`). Nếu tên field thật khác (vd
   `token`), chỉ cần sửa 2 hàm `refreshBackend`/`logoutBackend` trong file
   đó.
3. **Đối chiếu response body thật của `GET /auth/me`** với `types/auth.ts`
   (`AuthUser`) — hiện suy ra từ claim JWT liệt kê trong README.md
   (`sub/role/email/username`), CHƯA thấy response JSON thật.
4. **Xác nhận TTL thật của RefreshToken** bên backend, cập nhật
   `REFRESH_COOKIE_TTL_DAYS` trong `.env.local` cho khớp (hiện mặc định 7
   ngày — giá trị giả định, xem `lib/env.ts`).
5. Set `BACKEND_ALLOW_SELF_SIGNED_CERT=true` trong `.env.local` khi chạy
   backend local với cert tự ký (đúng như `curl -k` trong README.md gốc) —
   **KHÔNG** bật cờ này ở production.

## 8. Chưa làm ở Part này (đúng phạm vi, để Part sau)

- Toàn bộ CRUD Admin, Exam Room Candidate, Rater review, Dashboard — chỉ có
  layout + nav placeholder ("Sắp có").
- Upload ảnh/audio thật (PHASE7-ISSUE-04 — chờ xác nhận endpoint upload).
- Toast/notification system dùng chung (sẽ thêm khi Part 2 cần, tránh
  over-engineering lúc chưa có ai dùng).
- Test tự động (unit test cho `apiClient` refresh-retry logic, RBAC
  middleware) — có thể bổ sung `vitest`/`playwright` ở một bản vá nhỏ nếu
  Dragon muốn, không chặn tiến độ Part 2.
