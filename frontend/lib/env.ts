/**
 * Đọc biến môi trường phía server (Route Handlers, Middleware).
 * KHÔNG import file này từ Client Component — `BACKEND_URL` không có
 * tiền tố `NEXT_PUBLIC_` vì mọi lời gọi auth đều đi qua BFF, backend .NET
 * không cần lộ ra trình duyệt cho các endpoint auth (PHASE7-ISSUE-01).
 */
function required(name: string, fallback?: string): string {
  const value = process.env[name] ?? fallback;
  if (!value) {
    throw new Error(
      `Thiếu biến môi trường ${name}. Xem .env.example để biết danh sách đầy đủ.`
    );
  }
  return value;
}

export const env = {
  /** Base URL của Toeic.Api, ví dụ https://localhost:5001 */
  backendUrl: required("BACKEND_URL", "https://localhost:5001"),
  /**
   * Base URL của evaluation-service (Python/FastAPI, Phase 5) — service
   * độc lập, KHÔNG cùng origin với Toeic.Api. Chỉ dùng server-side trong
   * Route Handler proxy (PHASE7-ISSUE-03/16) — không lộ ra trình duyệt.
   */
  evaluationServiceUrl: required("EVALUATION_SERVICE_URL", "http://localhost:8000"),
  /**
   * Thời hạn cookie refresh_token (ngày). Backend Phase 1 Part 2 không
   * công khai TTL thật của RefreshToken trong tài liệu — 7 ngày là giá trị
   * mặc định hợp lý, CẦN Dragon đối chiếu với
   * `RefreshTokenOptions`/config thật (xem PHASE7_PART1_INTEGRATION_GUIDE.md).
   */
  refreshCookieTtlDays: Number(process.env.REFRESH_COOKIE_TTL_DAYS ?? "7"),
};

/**
 * Backend .NET dùng self-signed cert khi chạy local dev (`-k` trong các ví dụ
 * curl của README.md). `fetch` phía Node sẽ reject self-signed cert theo mặc
 * định. Set biến này trong `.env.local` (KHÔNG dùng ở production) để bỏ qua
 * kiểm tra TLS khi gọi backend local — xem `.env.example`.
 */
export function applyDevTlsWorkaround() {
  if (process.env.BACKEND_ALLOW_SELF_SIGNED_CERT === "true") {
    process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
  }
}
