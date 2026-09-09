"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { FormError } from "@/components/ui/form-error";
import { registerRequest, AuthRequestError } from "@/lib/auth-client";
import { useAuthStore } from "@/lib/auth-store";
import { roleHomePath } from "@/types/auth";

const initialForm = { username: "", fullName: "", email: "", password: "" };

export function RegisterForm() {
  const router = useRouter();
  const setSession = useAuthStore((s) => s.setSession);
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function update<K extends keyof typeof initialForm>(key: K, value: string) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const { accessToken, user } = await registerRequest(form);
      setSession(accessToken, user);
      router.push(roleHomePath(user.role));
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
        <Label htmlFor="fullName">Họ và tên</Label>
        <Input
          id="fullName"
          autoComplete="name"
          value={form.fullName}
          onChange={(e) => update("fullName", e.target.value)}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="username">Tên đăng nhập</Label>
        <Input
          id="username"
          autoComplete="username"
          value={form.username}
          onChange={(e) => update("username", e.target.value)}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="email">Email</Label>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          value={form.email}
          onChange={(e) => update("email", e.target.value)}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="password">Mật khẩu</Label>
        <Input
          id="password"
          type="password"
          autoComplete="new-password"
          value={form.password}
          onChange={(e) => update("password", e.target.value)}
          minLength={8}
          required
        />
      </div>
      <Button type="submit" variant="teal" className="w-full" disabled={submitting}>
        {submitting ? "Đang tạo tài khoản…" : "Tạo tài khoản"}
      </Button>
    </form>
  );
}
