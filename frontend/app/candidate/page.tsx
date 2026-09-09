"use client";

import Link from "next/link";
import { useAuthStore } from "@/lib/auth-store";

const quickLinks = [
  { href: "/candidate/exams", label: "Danh sách kỳ thi", desc: "Xem kỳ thi khả dụng và bắt đầu làm bài." },
  { href: "/candidate/history", label: "Lịch sử làm bài", desc: "Xem lại các phiên thi đã nộp và kết quả." },
  { href: "/candidate/analytics", label: "Phân tích năng lực", desc: "Điểm mạnh/yếu và xu hướng điểm qua thời gian." },
];

export default function CandidateHomePage() {
  const user = useAuthStore((s) => s.user);

  return (
    <div className="max-w-2xl">
      <h1 className="font-serif text-2xl font-medium text-ink">
        Chào {user?.fullName ?? "bạn"}
      </h1>
      <p className="mt-2 text-sm text-muted">
        Phòng thi, kết quả, lịch sử và phân tích năng lực đã sẵn sàng ở
        Phase 7, Part 3-4.
      </p>

      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        {quickLinks.map((link) => (
          <Link
            key={link.href}
            href={link.href}
            className="rounded-sm border border-border bg-paper-raised p-4 transition-colors hover:border-teal"
          >
            <p className="font-serif text-base font-medium text-ink">{link.label}</p>
            <p className="mt-1 text-[13px] text-muted">{link.desc}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
