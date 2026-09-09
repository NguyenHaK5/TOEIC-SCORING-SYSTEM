"use client";

import Link from "next/link";
import { useAuthStore } from "@/lib/auth-store";

const quickLinks = [
  { href: "/rater/writing-answers", label: "Bài chấm chờ xử lý", desc: "Chấm điểm Writing theo tiêu chí Rubric." },
  { href: "/rater/comparison", label: "So sánh AI vs Human", desc: "Các trường hợp AI và Rater chấm lệch nhiều nhất." },
];

export default function RaterHomePage() {
  const user = useAuthStore((s) => s.user);

  return (
    <div className="max-w-2xl">
      <h1 className="font-serif text-2xl font-medium text-ink">
        Chào {user?.fullName ?? "bạn"}
      </h1>
      <p className="mt-2 text-sm text-muted">Chấm bài Writing và đối chiếu với đánh giá AI.</p>

      <div className="mt-8 grid gap-4 sm:grid-cols-2">
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
