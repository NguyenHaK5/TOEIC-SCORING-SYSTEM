/**
 * Khớp với contract thật của `Toeic.Api` AuthController (Phase 1 Part 2,
 * đã build/test — xem README.md gốc mục "Part 2").
 *
 * GIẢ ĐỊNH cần Dragon đối chiếu (backend không lộ OpenAPI schema cho
 * sandbox này, chỉ có ví dụ curl trong README.md):
 * - `GET /auth/me` trả về đúng các field dưới đây (id/username/fullName/
 *   email/role) — suy ra từ JWT claims `sub/role/email/username` đã liệt
 *   kê trong README. Nếu field name thật khác, chỉ cần sửa type này +
 *   `lib/backend-auth.ts`, không ảnh hưởng phần còn lại của Part 1.
 */

export type UserRole = "ADMIN" | "RATER" | "CANDIDATE";

export interface AuthUser {
  id: string;
  username: string;
  fullName: string;
  email: string;
  role: UserRole;
}

/** Response thật của POST /auth/login và POST /auth/refresh. */
export interface BackendTokenResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}

/** Response mà Route Handler BFF trả về cho phía client (KHÔNG có refreshToken). */
export interface ClientSessionResponse {
  accessToken: string;
  user: AuthUser;
}

export interface LoginPayload {
  usernameOrEmail: string;
  password: string;
}

export interface RegisterPayload {
  username: string;
  fullName: string;
  email: string;
  password: string;
}

export function roleHomePath(role: UserRole): string {
  switch (role) {
    case "ADMIN":
      return "/admin";
    case "RATER":
      return "/rater";
    case "CANDIDATE":
      return "/candidate";
  }
}
