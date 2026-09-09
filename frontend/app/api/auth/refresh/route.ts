import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { BackendAuthError, refreshBackend } from "@/lib/backend-auth";
import {
  clearSessionCookies,
  setSessionCookies,
  REFRESH_COOKIE,
} from "@/lib/auth-cookies";
import type { ClientSessionResponse } from "@/types/auth";

/**
 * Gọi khi:
 * 1) AuthProvider mount lần đầu (khôi phục phiên sau khi reload trang, vì
 *    accessToken chỉ sống trong bộ nhớ JS — mất khi F5).
 * 2) `apiClient` gặp 401 từ backend .NET (accessToken hết hạn giữa chừng).
 *
 * Backend đã implement rotation (README.md Part 2): mỗi lần refresh, token
 * cũ bị revoke, token mới được cấp; nếu 1 token đã-revoke bị dùng lại
 * (dấu hiệu bị đánh cắp), backend tự revoke toàn bộ token đang active của
 * user — trường hợp đó `refreshBackend` sẽ trả lỗi và route này sẽ xoá
 * cookie, buộc đăng nhập lại.
 */
export async function POST() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_COOKIE)?.value;

  if (!refreshToken) {
    return NextResponse.json(
      { title: "Không có phiên đăng nhập." },
      { status: 401 }
    );
  }

  try {
    const result = await refreshBackend(refreshToken);
    const body: ClientSessionResponse = {
      accessToken: result.accessToken,
      user: result.user,
    };
    const response = NextResponse.json(body);
    setSessionCookies(response.cookies, result.refreshToken, result.user.role);
    return response;
  } catch (err) {
    const response = NextResponse.json(
      { title: err instanceof BackendAuthError ? err.message : "Phiên đăng nhập đã hết hạn." },
      { status: err instanceof BackendAuthError ? err.status : 401 }
    );
    clearSessionCookies(response.cookies);
    return response;
  }
}
