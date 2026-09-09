"use client";

import { useEffect, useRef } from "react";
import { refreshRequest } from "@/lib/auth-client";
import { useAuthStore } from "@/lib/auth-store";
import { msUntilExpiry } from "@/lib/jwt";

/** Làm mới sớm hơn 60s so với thời điểm hết hạn thật, tránh race condition
 * giữa lúc token "còn hạn" và lúc request nghiệp vụ thực sự tới backend. */
const REFRESH_SKEW_MS = 60_000;
const MIN_REFRESH_DELAY_MS = 5_000;

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const { status, accessToken, setSession, clear, setStatus } = useAuthStore();
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // 1) Khôi phục phiên khi app mount (accessToken chỉ ở bộ nhớ, mất khi F5).
  useEffect(() => {
    let cancelled = false;
    setStatus("loading");
    refreshRequest().then((result) => {
      if (cancelled) return;
      if (result) {
        setSession(result.accessToken, result.user);
      } else {
        clear();
      }
    });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // 2) Lên lịch tự refresh trước khi accessToken hết hạn.
  useEffect(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    if (!accessToken) return;

    const msLeft = msUntilExpiry(accessToken);
    if (msLeft === null) return;

    const delay = Math.max(msLeft - REFRESH_SKEW_MS, MIN_REFRESH_DELAY_MS);
    timerRef.current = setTimeout(async () => {
      const result = await refreshRequest();
      if (result) {
        setSession(result.accessToken, result.user);
      } else {
        clear();
      }
    }, delay);

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [accessToken, setSession, clear]);

  if (status === "idle" || status === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-paper">
        <p className="font-sans text-sm text-muted">Đang khôi phục phiên đăng nhập…</p>
      </div>
    );
  }

  return <>{children}</>;
}
