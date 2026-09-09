"use client";

import Link from "next/link";
import { useAuthStore } from "@/lib/auth-store";

const quickLinks = [
  { href: "/admin/exams", label: "Kỳ thi", desc: "Tạo kỳ thi, gán bộ đề, chuyển trạng thái." },
  { href: "/admin/tests", label: "Bộ đề (Test)", desc: "Ngân hàng đề, câu hỏi, phương án trả lời." },
  { href: "/admin/rubrics", label: "Rubric", desc: "Tiêu chí chấm Writing theo từng dạng câu." },
  { href: "/admin/dashboard", label: "Dashboard hệ thống", desc: "Tổng quan số liệu, lỗi Writing thường gặp." },
  { href: "/admin/evaluation", label: "Đánh giá AI vs Human", desc: "Metrics MAE/RMSE/Correlation, báo cáo lệch điểm." },
];

export default function AdminHomePage() {
  const user = useAuthStore((s) => s.user);

  return (
    <div className="max-w-2xl">
      <h1 className="font-serif text-2xl font-medium text-ink">
        Chào {user?.fullName ?? "bạn"}
      </h1>
      <p className="mt-2 text-sm text-muted">
        Quản lý nội dung thi, dashboard hệ thống và đánh giá AI vs Human —
        Phase 7 hoàn tất (Part 1-6).
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
