import { RoleShell, type RoleNavItem } from "@/components/layout/role-shell";

const navItems: RoleNavItem[] = [
  { label: "Tổng quan", href: "/admin", available: true },
  { label: "Kỳ thi (Exam)", href: "/admin/exams", available: true },
  { label: "Bộ đề (Test)", href: "/admin/tests", available: true },
  { label: "Rubric", href: "/admin/rubrics", available: true },
  { label: "Dashboard hệ thống", href: "/admin/dashboard", available: true },
  { label: "Đánh giá AI vs Human", href: "/admin/evaluation", available: true },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <RoleShell role="ADMIN" navItems={navItems}>
      {children}
    </RoleShell>
  );
}
