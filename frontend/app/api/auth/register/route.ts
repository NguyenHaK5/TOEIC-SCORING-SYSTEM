import { NextResponse } from "next/server";
import { BackendAuthError, loginBackend, registerBackend } from "@/lib/backend-auth";
import { setSessionCookies } from "@/lib/auth-cookies";
import type { ClientSessionResponse, RegisterPayload } from "@/types/auth";

/**
 * `/auth/register` (backend) luôn tạo tài khoản CANDIDATE (README.md Part 2
 * — server ép `Role = CANDIDATE` bất kể client gửi gì). Sau khi tạo xong,
 * route này **tự đăng nhập luôn** (gọi lại `loginBackend`) để tránh bắt
 * người dùng gõ lại mật khẩu ngay sau khi vừa đăng ký — UX thông thường,
 * không phải yêu cầu bảo mật, có thể bỏ nếu Dragon muốn giữ 2 bước tách
 * biệt.
 */
export async function POST(request: Request) {
  let payload: RegisterPayload;
  try {
    payload = await request.json();
  } catch {
    return NextResponse.json(
      { title: "Body request không hợp lệ." },
      { status: 400 }
    );
  }

  if (!payload.username || !payload.fullName || !payload.email || !payload.password) {
    return NextResponse.json(
      { title: "Vui lòng điền đầy đủ thông tin." },
      { status: 400 }
    );
  }

  try {
    await registerBackend(payload);
    const { accessToken, refreshToken, user } = await loginBackend({
      usernameOrEmail: payload.username,
      password: payload.password,
    });

    const body: ClientSessionResponse = { accessToken, user };
    const response = NextResponse.json(body, { status: 201 });
    setSessionCookies(response.cookies, refreshToken, user.role);
    return response;
  } catch (err) {
    if (err instanceof BackendAuthError) {
      return NextResponse.json({ title: err.message }, { status: err.status });
    }
    console.error("[auth/register] unexpected error", err);
    return NextResponse.json(
      { title: "Không thể kết nối tới máy chủ. Vui lòng thử lại." },
      { status: 502 }
    );
  }
}
