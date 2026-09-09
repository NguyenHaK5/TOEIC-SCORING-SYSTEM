import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { logoutBackend } from "@/lib/backend-auth";
import { clearSessionCookies, REFRESH_COOKIE } from "@/lib/auth-cookies";

export async function POST() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_COOKIE)?.value;

  if (refreshToken) {
    // Best-effort: dù backend revoke lỗi (vd token đã hết hạn sẵn), vẫn xoá
    // cookie phía trình duyệt — người dùng không nên bị kẹt "đăng xuất
    // không được" vì lỗi mạng tạm thời.
    try {
      await logoutBackend(refreshToken);
    } catch (err) {
      console.warn("[auth/logout] backend revoke failed, clearing cookies anyway", err);
    }
  }

  const response = NextResponse.json({ success: true });
  clearSessionCookies(response.cookies);
  return response;
}
