import { RoleShell, type RoleNavItem } from "@/components/layout/role-shell";

const navItems: RoleNavItem[] = [
  { label: "Tổng quan", href: "/candidate", available: true },
  { label: "Danh sách kỳ thi", href: "/candidate/exams", available: true },
  { label: "Lịch sử & kết quả", href: "/candidate/history", available: true },
  { label: "Phân tích năng lực", href: "/candidate/analytics", available: true },
];

export default function CandidateLayout({ children }: { children: React.ReactNode }) {
  return (
    <RoleShell role="CANDIDATE" navItems={navItems}>
      {children}
    </RoleShell>
  );
}
