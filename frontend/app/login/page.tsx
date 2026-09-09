import { Suspense } from "react";
import { LoginForm } from "./login-form";
import { AnswerSheetPattern } from "@/components/layout/answer-sheet-pattern";
import { Wordmark } from "@/components/layout/wordmark";

export default function LoginPage() {
  return (
    <div className="grid min-h-screen md:grid-cols-2">
      <section className="relative hidden overflow-hidden bg-ink px-14 py-12 text-paper-raised md:flex md:flex-col md:justify-between">
        <AnswerSheetPattern className="pointer-events-none absolute -right-10 top-1/2 h-[520px] w-[210px] -translate-y-1/2 text-paper-raised" />
        <Wordmark tone="paper" />
        <div className="relative max-w-sm">
          <h1 className="font-serif text-4xl font-medium leading-[1.15]">
            Chấm điểm chính xác, từng phần thi một.
          </h1>
          <ul className="mt-8 space-y-3 text-sm text-paper-raised/75">
            <li className="flex items-center gap-3">
              <span className="h-1.5 w-1.5 rounded-full bg-teal" />
              Listening &amp; Reading — chấm khách quan bằng Answer Key
            </li>
            <li className="flex items-center gap-3">
              <span className="h-1.5 w-1.5 rounded-full bg-teal" />
              Writing — AI phân tích, Rubric định nghĩa tiêu chí
            </li>
            <li className="flex items-center gap-3">
              <span className="h-1.5 w-1.5 rounded-full bg-teal" />
              Rater đối chiếu, hệ thống đánh giá độ tin cậy AI
            </li>
          </ul>
        </div>
        <p className="relative text-xs text-paper-raised/50">
          Server là nguồn xác thực thời gian thi duy nhất.
        </p>
      </section>

      <section className="flex items-center justify-center px-6 py-16">
        <div className="w-full max-w-sm">
          <div className="mb-10 md:hidden">
            <Wordmark />
          </div>
          <h2 className="font-serif text-2xl font-medium text-ink">Đăng nhập</h2>
          <p className="mt-1.5 text-sm text-muted">
            Dùng tài khoản đã được cấp cho vai trò của bạn.
          </p>
          <Suspense>
            <LoginForm />
          </Suspense>
          <p className="mt-8 text-sm text-muted">
            Chưa có tài khoản thí sinh?{" "}
            <a href="/register" className="font-medium text-teal hover:underline">
              Đăng ký dự thi
            </a>
          </p>
        </div>
      </section>
    </div>
  );
}
