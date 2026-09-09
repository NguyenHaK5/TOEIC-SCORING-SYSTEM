"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import { Wordmark } from "@/components/layout/wordmark";
import { Button } from "@/components/ui/button";
import { logoutRequest } from "@/lib/auth-client";
import { useAuthStore } from "@/lib/auth-store";
import type { UserRole } from "@/types/auth";
import { cn } from "@/lib/utils";

const ROLE_LABEL: Record<UserRole, string> = {
  ADMIN: "Quản trị viên",
  RATER: "Giám khảo",
  CANDIDATE: "Thí sinh",
};

export interface RoleNavItem {
  label: string;
  href: string;
  /** false = chưa xây ở Part hiện tại, hiển thị disabled + nhãn "Sắp có". */
  available: boolean;
}

export function RoleShell({
  role,
  navItems,
  children,
}: {
  role: UserRole;
  navItems: RoleNavItem[];
  children: React.ReactNode;
}) {
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);

  async function handleLogout() {
    await logoutRequest();
    clear();
    router.push("/login");
    router.refresh();
  }

  return (
    <div className="min-h-screen bg-paper">
      <header className="flex h-16 items-center justify-between border-b border-border bg-paper-raised px-6">
        <div className="flex items-center gap-4">
          <Wordmark className="text-base" />
          <span className="rounded-full border border-border px-2.5 py-0.5 text-[12px] text-muted">
            {ROLE_LABEL[role]}
          </span>
        </div>
        <div className="flex items-center gap-4">
          {user && (
            <span className="text-sm text-ink-soft">{user.fullName}</span>
          )}
          <Button variant="outline" size="sm" onClick={handleLogout}>
            Đăng xuất
          </Button>
        </div>
      </header>

      <div className="mx-auto flex max-w-6xl">
        <aside className="hidden w-60 shrink-0 border-r border-border px-4 py-8 md:block">
          <nav className="space-y-1">
            {navItems.map((item) =>
              item.available ? (
                <Link
                  key={item.href}
                  href={item.href}
                  className="block rounded-sm px-3 py-2 text-sm text-ink-soft transition-colors hover:bg-white hover:text-ink"
                >
                  {item.label}
                </Link>
              ) : (
                <div
                  key={item.href}
                  className={cn(
                    "flex items-center justify-between rounded-sm px-3 py-2 text-sm text-muted/70"
                  )}
                >
                  <span>{item.label}</span>
                  <span className="text-[11px] text-muted/60">Sắp có</span>
                </div>
              )
            )}
          </nav>
        </aside>

        <main className="min-w-0 flex-1 px-6 py-8 md:px-10">{children}</main>
      </div>
    </div>
  );
}
