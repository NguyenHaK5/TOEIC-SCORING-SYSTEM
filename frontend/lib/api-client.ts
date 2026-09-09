import { refreshRequest } from "@/lib/auth-client";
import { useAuthStore } from "@/lib/auth-store";

/**
 * Client dùng cho MỌI lời gọi nghiệp vụ tới backend .NET (Exam/Test/
 * Question/Rubric/ExamSession/...) ở các Part sau — Part 1 chỉ dựng phần
 * lõi này, chưa có màn hình nào dùng tới.
 *
 * Khác với auth (luôn đi qua BFF `/api/auth/*`), API nghiệp vụ gọi THẲNG
 * backend .NET từ trình duyệt kèm `Authorization: Bearer <accessToken>`
 * lấy từ bộ nhớ (Zustand) — đúng quyết định đã chốt ở PHASE7-ISSUE-01.
 *
 * `NEXT_PUBLIC_BACKEND_URL` phải trỏ tới cùng backend với `BACKEND_URL`
 * (server-only, dùng trong BFF) — xem `.env.example`.
 */

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public detail?: unknown
  ) {
    super(message);
    this.name = "ApiError";
  }
}

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL ?? "https://localhost:5001";

interface ApiFetchOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  /** Bỏ qua vòng tự-refresh-rồi-retry khi 401 (dùng nội bộ để tránh loop). */
  _isRetry?: boolean;
}

async function parseErrorBody(res: Response): Promise<{ message: string; detail?: unknown }> {
  try {
    const data = await res.json();
    const message =
      typeof data?.title === "string"
        ? data.title
        : typeof data?.message === "string"
          ? data.message
          : `Yêu cầu thất bại (${res.status})`;
    return { message, detail: data };
  } catch {
    return { message: `Yêu cầu thất bại (${res.status})` };
  }
}

export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const { body, _isRetry, headers, ...rest } = options;
  const accessToken = useAuthStore.getState().accessToken;

  const res = await fetch(`${BACKEND_URL}/api/v1${path}`, {
    ...rest,
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...headers,
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401 && !_isRetry) {
    // accessToken hết hạn giữa chừng -> thử refresh 1 lần rồi gọi lại.
    const refreshed = await refreshRequest();
    if (refreshed) {
      useAuthStore.getState().setSession(refreshed.accessToken, refreshed.user);
      return apiFetch<T>(path, { ...options, _isRetry: true });
    }
    useAuthStore.getState().clear();
    if (typeof window !== "undefined") {
      // apiClient được gọi từ nhiều ngữ cảnh khác nhau (event handler, hook,
      // thậm chí ngoài React), không phải lúc nào cũng có `useRouter()` sẵn
      // trong scope. Hard redirect là lựa chọn thực dụng ở đây.
      // eslint-disable-next-line @next/next/no-location-assign-relative-destination
      window.location.href = "/login";
    }
    throw new ApiError(401, "Phiên đăng nhập đã hết hạn.");
  }

  if (!res.ok) {
    const { message, detail } = await parseErrorBody(res);
    throw new ApiError(res.status, message, detail);
  }

  if (res.status === 204) {
    return undefined as T;
  }
  return (await res.json()) as T;
}

export const apiClient = {
  get: <T>(path: string) => apiFetch<T>(path, { method: "GET" }),
  post: <T>(path: string, body?: unknown) => apiFetch<T>(path, { method: "POST", body }),
  put: <T>(path: string, body?: unknown) => apiFetch<T>(path, { method: "PUT", body }),
  patch: <T>(path: string, body?: unknown) => apiFetch<T>(path, { method: "PATCH", body }),
  delete: <T>(path: string) => apiFetch<T>(path, { method: "DELETE" }),
};
