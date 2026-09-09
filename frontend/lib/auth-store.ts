import { create } from "zustand";
import type { AuthUser } from "@/types/auth";

/**
 * accessToken sống DUY NHẤT ở đây (bộ nhớ JS phía client) — không bao giờ
 * ghi vào localStorage/sessionStorage (PHASE7-ISSUE-01). Bị mất khi F5,
 * `AuthProvider` sẽ tự khôi phục bằng cách gọi `/api/auth/refresh` (dựa vào
 * cookie httpOnly `refresh_token`) ngay khi app mount.
 */
export type AuthStatus = "idle" | "loading" | "authenticated" | "unauthenticated";

interface AuthState {
  status: AuthStatus;
  accessToken: string | null;
  user: AuthUser | null;
  setSession: (accessToken: string, user: AuthUser) => void;
  clear: () => void;
  setStatus: (status: AuthStatus) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  status: "idle",
  accessToken: null,
  user: null,
  setSession: (accessToken, user) =>
    set({ accessToken, user, status: "authenticated" }),
  clear: () => set({ accessToken: null, user: null, status: "unauthenticated" }),
  setStatus: (status) => set({ status }),
}));
