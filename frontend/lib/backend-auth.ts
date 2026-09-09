import "server-only";
import { applyDevTlsWorkaround, env } from "@/lib/env";
import type {
  BackendTokenResponse,
  AuthUser,
  LoginPayload,
  RegisterPayload,
} from "@/types/auth";

applyDevTlsWorkaround();

/** Lỗi nghiệp vụ trả về từ backend (`application/problem+json`, xem
 * README.md — middleware `ExceptionHandlingMiddleware`). */
export class BackendAuthError extends Error {
  constructor(
    public status: number,
    message: string,
    public detail?: unknown
  ) {
    super(message);
    this.name = "BackendAuthError";
  }
}

async function callBackend<T>(
  path: string,
  init: RequestInit
): Promise<T> {
  const res = await fetch(`${env.backendUrl}/api/v1${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...init.headers,
    },
    // Route Handler chạy server-side — không cache response auth.
    cache: "no-store",
  });

  if (!res.ok) {
    let detail: unknown;
    try {
      detail = await res.json();
    } catch {
      detail = await res.text().catch(() => undefined);
    }
    const message =
      (typeof detail === "object" &&
        detail !== null &&
        "title" in detail &&
        typeof (detail as { title?: unknown }).title === "string" &&
        (detail as { title: string }).title) ||
      `Backend trả lỗi ${res.status}`;
    throw new BackendAuthError(res.status, message, detail);
  }

  // 204 No Content (vd logout) — không có body để parse.
  if (res.status === 204) {
    return undefined as T;
  }
  return (await res.json()) as T;
}

export function loginBackend(payload: LoginPayload) {
  return callBackend<BackendTokenResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function registerBackend(payload: RegisterPayload) {
  return callBackend<AuthUser>("/auth/register", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

/**
 * GIẢ ĐỊNH cần đối chiếu: README.md chỉ ghi `/auth/refresh` và `/auth/logout`
 * là "Public (cần refresh token hợp lệ)" mà không show rõ field JSON body
 * (`curl` mẫu không có ví dụ cho 2 endpoint này). Giả định request body là
 * `{ "refreshToken": "..." }` — nếu backend thật dùng tên field khác (vd
 * `token`), chỉ cần sửa 2 hàm dưới đây, không ảnh hưởng phần còn lại.
 */
export function refreshBackend(refreshToken: string) {
  return callBackend<BackendTokenResponse>("/auth/refresh", {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
}

export function logoutBackend(refreshToken: string) {
  return callBackend<void>("/auth/logout", {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
}
