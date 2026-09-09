import { NextResponse } from "next/server";
import { BackendAuthError, loginBackend } from "@/lib/backend-auth";
import { setSessionCookies } from "@/lib/auth-cookies";
import type { ClientSessionResponse, LoginPayload } from "@/types/auth";

export async function POST(request: Request) {
  let payload: LoginPayload;
  try {
    payload = await request.json();
  } catch {
    return NextResponse.json(
      { title: "Body request không hợp lệ." },
      { status: 400 }
    );
  }

  if (!payload.usernameOrEmail || !payload.password) {
    return NextResponse.json(
      { title: "Thiếu tên đăng nhập/email hoặc mật khẩu." },
      { status: 400 }
    );
  }

  try {
    const { accessToken, refreshToken, user } = await loginBackend(payload);

    const body: ClientSessionResponse = { accessToken, user };
    const response = NextResponse.json(body);
    // Raw refresh token & role KHÔNG bao giờ vào JSON body — chỉ vào cookie
    // httpOnly (PHASE7-ISSUE-01 / PHASE7-ISSUE-05).
    setSessionCookies(response.cookies, refreshToken, user.role);
    return response;
  } catch (err) {
    if (err instanceof BackendAuthError) {
      return NextResponse.json({ title: err.message }, { status: err.status });
    }
    console.error("[auth/login] unexpected error", err);
    return NextResponse.json(
      { title: "Không thể kết nối tới máy chủ. Vui lòng thử lại." },
      { status: 502 }
    );
  }
}
