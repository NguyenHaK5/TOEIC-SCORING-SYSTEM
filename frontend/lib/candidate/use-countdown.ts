"use client";

import { useEffect, useState } from "react";

/**
 * Đếm ngược dựa trên `expiresAt` do SERVER trả về (mục 16 prompt gốc:
 * "Server phải là nguồn xác thực thời gian cuối cùng"). Đồng hồ máy khách
 * chỉ dùng để hiển thị mượt mà giữa các lần đồng bộ lại với server — KHÔNG
 * bao giờ tự ý kéo dài hạn hoặc tự tính lại `expiresAt`.
 *
 * `onExpire` được gọi đúng 1 lần khi đếm về 0 — nơi gọi (Exam Room) dùng
 * callback này để tự động submit, đồng thời backend cũng tự lazy
 * auto-submit ở lần request tiếp theo chạm tới phiên (defense in depth,
 * xem README.md Phase 1 Part 4).
 */
export function useCountdown(expiresAtIso: string | undefined, onExpire: () => void) {
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);

  useEffect(() => {
    if (!expiresAtIso) return;
    const expiresAtMs = new Date(expiresAtIso).getTime();

    function tick() {
      const remaining = Math.max(0, Math.round((expiresAtMs - Date.now()) / 1000));
      setSecondsLeft(remaining);
      return remaining;
    }

    const initial = tick();
    if (initial <= 0) {
      onExpire();
      return;
    }

    const interval = setInterval(() => {
      const remaining = tick();
      if (remaining <= 0) {
        clearInterval(interval);
        onExpire();
      }
    }, 1000);

    return () => clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [expiresAtIso]);

  return {
    secondsLeft,
    formatted: secondsLeft === null ? "--:--:--" : formatDuration(secondsLeft),
    isLow: secondsLeft !== null && secondsLeft <= 300,
  };
}

function formatDuration(totalSeconds: number): string {
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  const pad = (n: number) => String(n).padStart(2, "0");
  return h > 0 ? `${pad(h)}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`;
}
