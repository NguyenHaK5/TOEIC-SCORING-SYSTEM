import "server-only";
import { env } from "@/lib/env";
import type { UserRole } from "@/types/auth";

/**
 * Interface tối giản khớp cả `NextResponse.cookies` lẫn cookie jar mutable
 * trả về từ `next/headers` `cookies()` — tránh import type nội bộ của
 * Next.js (đường dẫn `next/dist/...` không ổn định giữa các version).
 */
export interface CookieJar {
  set(
    name: string,
    value: string,
    options?: {
      httpOnly?: boolean;
      secure?: boolean;
      sameSite?: "lax" | "strict" | "none";
      path?: string;
      maxAge?: number;
    }
  ): void;
  delete(name: string): void;
}

/**
 * 2 cookie httpOnly được set bởi BFF (không bao giờ set từ client JS):
 *
 * - `refresh_token`: raw opaque refresh token thật từ backend. Chỉ được
 *   đọc lại bởi chính các Route Handler `/api/auth/*` để gọi
 *   `/auth/refresh`/`/auth/logout` — không bao giờ trả về body JSON cho
 *   client (PHASE7-ISSUE-01).
 * - `session_role`: CHỈ chứa role (vd "ADMIN") — không phải secret, không
 *   phải access token. Middleware (Edge runtime) đọc cookie này để quyết
 *   định route `/admin`, `/rater`, `/candidate` nào được vào, VÌ access
 *   token thật chỉ tồn tại trong bộ nhớ phía client và middleware không
 *   thể đọc được state đó. Đây là một lớp routing/UX, KHÔNG phải biên giới
 *   bảo mật thật — biên giới bảo mật thật vẫn là backend .NET validate JWT
 *   Bearer token trên từng request (đã build/test). Ghi lại như
 *   PHASE7-ISSUE-05 trong PHASE7_PART1_INTEGRATION_GUIDE.md.
 */
export const REFRESH_COOKIE = "refresh_token";
export const ROLE_COOKIE = "session_role";

const isProd = process.env.NODE_ENV === "production";

export function setSessionCookies(
  cookies: CookieJar,
  refreshToken: string,
  role: UserRole
) {
  const maxAge = env.refreshCookieTtlDays * 24 * 60 * 60;
  cookies.set(REFRESH_COOKIE, refreshToken, {
    httpOnly: true,
    secure: isProd,
    sameSite: "lax",
    path: "/",
    maxAge,
  });
  cookies.set(ROLE_COOKIE, role, {
    httpOnly: true,
    secure: isProd,
    sameSite: "lax",
    path: "/",
    maxAge,
  });
}

export function clearSessionCookies(cookies: CookieJar) {
  cookies.delete(REFRESH_COOKIE);
  cookies.delete(ROLE_COOKIE);
}
