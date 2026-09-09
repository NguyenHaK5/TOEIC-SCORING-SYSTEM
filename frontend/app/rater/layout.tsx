import { RoleShell, type RoleNavItem } from "@/components/layout/role-shell";

const navItems: RoleNavItem[] = [
  { label: "Tổng quan", href: "/rater", available: true },
  { label: "Bài chấm chờ xử lý", href: "/rater/writing-answers", available: true },
  { label: "So sánh AI vs Human", href: "/rater/comparison", available: true },
];

export default function RaterLayout({ children }: { children: React.ReactNode }) {
  return (
    <RoleShell role="RATER" navItems={navItems}>
      {children}
    </RoleShell>
  );
}
