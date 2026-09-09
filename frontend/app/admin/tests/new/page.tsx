"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { testsApi } from "@/lib/admin/tests-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FormError } from "@/components/ui/form-error";
import type { CreateTestPayload } from "@/types/test";

const initialForm: CreateTestPayload = {
  testCode: "",
  testName: "",
  version: "1.0",
  description: "",
  listeningDurationSeconds: 2700,
  readingDurationSeconds: 4500,
  writingDurationSeconds: 3600,
};

export default function NewTestPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (payload: CreateTestPayload) => testsApi.create(payload),
    onSuccess: (test) => {
      queryClient.invalidateQueries({ queryKey: ["admin", "tests"] });
      toast.success(`Đã tạo bộ đề "${test.testName}".`);
      router.push(`/admin/tests/${test.testId}`);
    },
    onError: (err) => setError(errorMessage(err, "Không tạo được bộ đề.")),
  });

  function update<K extends keyof CreateTestPayload>(key: K, value: CreateTestPayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    mutation.mutate(form);
  }

  return (
    <div className="max-w-xl">
      <h1 className="font-serif text-2xl font-medium text-ink">Tạo bộ đề</h1>
      <p className="mt-1 text-sm text-muted">
        Sau khi tạo, thêm đủ câu hỏi cho từng phần rồi Publish trước khi có thể gán vào kỳ thi.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5" noValidate>
        <FormError message={error} />

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="testCode">Mã bộ đề</Label>
            <Input
              id="testCode"
              placeholder="FORM-A"
              value={form.testCode}
              onChange={(e) => update("testCode", e.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="version">Phiên bản</Label>
            <Input
              id="version"
              value={form.version}
              onChange={(e) => update("version", e.target.value)}
              required
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="testName">Tên bộ đề</Label>
          <Input
            id="testName"
            placeholder="TOEIC Form A"
            value={form.testName}
            onChange={(e) => update("testName", e.target.value)}
            required
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="description">Mô tả</Label>
          <Textarea
            id="description"
            value={form.description}
            onChange={(e) => update("description", e.target.value)}
          />
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div className="space-y-2">
            <Label htmlFor="listeningDurationSeconds">Listening (giây)</Label>
            <Input
              id="listeningDurationSeconds"
              type="number"
              min={0}
              value={form.listeningDurationSeconds}
              onChange={(e) => update("listeningDurationSeconds", Number(e.target.value))}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="readingDurationSeconds">Reading (giây)</Label>
            <Input
              id="readingDurationSeconds"
              type="number"
              min={0}
              value={form.readingDurationSeconds}
              onChange={(e) => update("readingDurationSeconds", Number(e.target.value))}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="writingDurationSeconds">Writing (giây)</Label>
            <Input
              id="writingDurationSeconds"
              type="number"
              min={0}
              value={form.writingDurationSeconds}
              onChange={(e) => update("writingDurationSeconds", Number(e.target.value))}
              required
            />
          </div>
        </div>

        <div className="flex gap-3 pt-2">
          <Button type="submit" variant="teal" disabled={mutation.isPending}>
            {mutation.isPending ? "Đang tạo…" : "Tạo bộ đề"}
          </Button>
          <Button type="button" variant="outline" onClick={() => router.back()}>
            Huỷ
          </Button>
        </div>
      </form>
    </div>
  );
}
