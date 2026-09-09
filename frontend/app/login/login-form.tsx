"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { FormError } from "@/components/ui/form-error";
import { loginRequest, AuthRequestError } from "@/lib/auth-client";
import { useAuthStore } from "@/lib/auth-store";
import { roleHomePath } from "@/types/auth";

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const setSession = useAuthStore((s) => s.setSession);

  const [usernameOrEmail, setUsernameOrEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const { accessToken, user } = await loginRequest({ usernameOrEmail, password });
      setSession(accessToken, user);
      const next = searchParams.get("next");
      router.push(next && next.startsWith("/") ? next : roleHomePath(user.role));
      router.refresh();
    } catch (err) {
      setError(
        err instanceof AuthRequestError
          ? err.message
          : "Không thể kết nối tới máy chủ. Vui lòng thử lại."
      );
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mt-8 space-y-5" noValidate>
      <FormError message={error} />
      <div className="space-y-2">
        <Label htmlFor="usernameOrEmail">Tên đăng nhập hoặc email</Label>
        <Input
          id="usernameOrEmail"
          name="usernameOrEmail"
          autoComplete="username"
          value={usernameOrEmail}
          onChange={(e) => setUsernameOrEmail(e.target.value)}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="password">Mật khẩu</Label>
        <Input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
      </div>
      <Button type="submit" variant="teal" className="w-full" disabled={submitting}>
        {submitting ? "Đang đăng nhập…" : "Đăng nhập"}
      </Button>
    </form>
  );
}
