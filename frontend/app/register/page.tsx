import { RegisterForm } from "./register-form";
import { Wordmark } from "@/components/layout/wordmark";

export default function RegisterPage() {
  return (
    <div className="flex min-h-screen items-center justify-center px-6 py-16">
      <div className="w-full max-w-sm">
        <Wordmark />
        <h2 className="mt-10 font-serif text-2xl font-medium text-ink">Đăng ký dự thi</h2>
        <p className="mt-1.5 text-sm text-muted">
          Tài khoản mới được tạo với vai trò Thí sinh (Candidate).
        </p>
        <RegisterForm />
        <p className="mt-8 text-sm text-muted">
          Đã có tài khoản?{" "}
          <a href="/login" className="font-medium text-teal hover:underline">
            Đăng nhập
          </a>
        </p>
      </div>
    </div>
  );
}
