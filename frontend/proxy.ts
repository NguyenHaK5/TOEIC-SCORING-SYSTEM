import { NextResponse, type NextRequest } from "next/server";
import { REFRESH_COOKIE, ROLE_COOKIE } from "@/lib/auth-cookies";
import { roleHomePath, type UserRole } from "@/types/auth";

/**
 * RBAC ở tầng route, chạy trên Edge trước khi React render.
 *
 * QUAN TRỌNG (xem PHASE7-ISSUE-05 trong PHASE7_PART1_INTEGRATION_GUIDE.md):
 * đây là lớp UX/routing, KHÔNG phải biên giới bảo mật cuối cùng — accessToken
 * thật chỉ sống trong bộ nhớ phía client nên middleware không đọc được.
 * Biên giới bảo mật thật là backend .NET validate JWT Bearer token trên mỗi
 * request nghiệp vụ (đã build/test ở Phase 1 Part 2). Middleware này chỉ
 * ngăn người dùng "nhìn thấy" UI sai role, không thay thế authorization
 * thật.
 */

const ROLE_PREFIXES: Record<UserRole, string[]> = {
  ADMIN: ["/admin"],
  RATER: ["/rater"],
  // "/exam-room" cố ý đặt NGOÀI "/candidate" — xem PHASE7-ISSUE-11
  // (PHASE7_PART3_INTEGRATION_GUIDE.md): Phòng thi cần giao diện tối giản,
  // không có sidebar nav/logout dễ bấm nhầm của RoleShell, nhưng vẫn phải
  // được bảo vệ đúng quyền CANDIDATE như mọi route khác.
  CANDIDATE: ["/candidate", "/exam-room"],
};

const PUBLIC_PATHS = ["/login", "/register", "/unauthorized"];

function matchesRoleArea(pathname: string): UserRole | null {
  for (const [role, prefixes] of Object.entries(ROLE_PREFIXES) as [UserRole, string[]][]) {
    for (const prefix of prefixes) {
      if (pathname === prefix || pathname.startsWith(`${prefix}/`)) {
        return role;
      }
    }
  }
  return null;
}

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Route Handler auth, static assets, Next.js internals — luôn cho qua.
  if (
    pathname.startsWith("/api/") ||
    pathname.startsWith("/_next/") ||
    pathname === "/favicon.ico"
  ) {
    return NextResponse.next();
  }

  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;
  const role = request.cookies.get(ROLE_COOKIE)?.value as UserRole | undefined;
  const isAuthenticated = Boolean(refreshToken && role);

  if (PUBLIC_PATHS.includes(pathname)) {
    // Đã đăng nhập mà cố vào /login hoặc /register -> đưa thẳng về home của role.
    if (isAuthenticated && (pathname === "/login" || pathname === "/register")) {
      return NextResponse.redirect(new URL(roleHomePath(role!), request.url));
    }
    return NextResponse.next();
  }

  if (pathname === "/") {
    return NextResponse.redirect(
      new URL(isAuthenticated ? roleHomePath(role!) : "/login", request.url)
    );
  }

  const requiredRole = matchesRoleArea(pathname);
  if (requiredRole) {
    if (!isAuthenticated) {
      const loginUrl = new URL("/login", request.url);
      loginUrl.searchParams.set("next", pathname);
      return NextResponse.redirect(loginUrl);
    }
    if (role !== requiredRole) {
      return NextResponse.redirect(new URL("/unauthorized", request.url));
    }
    return NextResponse.next();
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image).*)"],
};
