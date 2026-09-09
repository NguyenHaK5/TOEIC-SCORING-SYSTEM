import type {
  ClientSessionResponse,
  LoginPayload,
  RegisterPayload,
} from "@/types/auth";

export class AuthRequestError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = "AuthRequestError";
  }
}

async function parseSessionResponse(res: Response): Promise<ClientSessionResponse> {
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    throw new AuthRequestError(
      res.status,
      typeof data?.title === "string" ? data.title : "Đã có lỗi xảy ra."
    );
  }
  return data as ClientSessionResponse;
}

export async function loginRequest(payload: LoginPayload) {
  const res = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  return parseSessionResponse(res);
}

export async function registerRequest(payload: RegisterPayload) {
  const res = await fetch("/api/auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  return parseSessionResponse(res);
}

/** Trả `null` (thay vì throw) khi không có phiên hợp lệ — dùng lúc khởi
 * động app để âm thầm thử khôi phục phiên, không phải lỗi cần báo người dùng. */
export async function refreshRequest(): Promise<ClientSessionResponse | null> {
  const res = await fetch("/api/auth/refresh", { method: "POST" });
  if (!res.ok) return null;
  return (await res.json()) as ClientSessionResponse;
}

export async function logoutRequest(): Promise<void> {
  await fetch("/api/auth/logout", { method: "POST" });
}
