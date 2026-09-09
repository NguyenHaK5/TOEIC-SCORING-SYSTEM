import Link from "next/link";
import { Wordmark } from "@/components/layout/wordmark";

export default function UnauthorizedPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-6 px-6 text-center">
      <Wordmark />
      <div>
        <h1 className="font-serif text-2xl font-medium text-ink">Không có quyền truy cập</h1>
        <p className="mt-2 max-w-sm text-sm text-muted">
          Tài khoản của bạn không có quyền xem trang này. Nếu bạn nghĩ đây là
          nhầm lẫn, liên hệ quản trị viên.
        </p>
      </div>
      <Link href="/" className="text-sm font-medium text-teal hover:underline">
        Về trang chính
      </Link>
    </div>
  );
}
