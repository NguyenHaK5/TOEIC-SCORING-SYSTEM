"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { rubricsApi } from "@/lib/admin/rubrics-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FormError } from "@/components/ui/form-error";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { QUESTION_TYPE_LABEL, type QuestionType } from "@/types/question";
import type { CreateRubricSetPayload } from "@/types/rubric";

const initialForm: CreateRubricSetPayload = {
  name: "",
  questionType: "OPINION_ESSAY",
  description: "",
  version: "1.0",
};

export default function NewRubricSetPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (payload: CreateRubricSetPayload) => rubricsApi.create(payload),
    onSuccess: (rubricSet) => {
      queryClient.invalidateQueries({ queryKey: ["admin", "rubric-sets"] });
      toast.success(`Đã tạo bộ rubric "${rubricSet.name}".`);
      router.push(`/admin/rubrics/${rubricSet.rubricSetId}`);
    },
    onError: (err) => setError(errorMessage(err, "Không tạo được bộ rubric.")),
  });

  function update<K extends keyof CreateRubricSetPayload>(key: K, value: CreateRubricSetPayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    mutation.mutate(form);
  }

  return (
    <div className="max-w-xl">
      <h1 className="font-serif text-2xl font-medium text-ink">Tạo bộ rubric</h1>
      <p className="mt-1 text-sm text-muted">
        Sau khi tạo, thêm các tiêu chí (Grammar, Vocabulary, …) rồi kích hoạt (Active).
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5" noValidate>
        <FormError message={error} />

        <div className="space-y-2">
          <Label htmlFor="name">Tên bộ rubric</Label>
          <Input
            id="name"
            placeholder="TOEIC Q8 Essay Rubric"
            value={form.name}
            onChange={(e) => update("name", e.target.value)}
            required
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>Áp dụng cho dạng câu</Label>
            <Select
              value={form.questionType}
              onValueChange={(v) => update("questionType", v as QuestionType)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(QUESTION_TYPE_LABEL).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
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
          <Label htmlFor="description">Mô tả</Label>
          <Textarea
            id="description"
            value={form.description}
            onChange={(e) => update("description", e.target.value)}
          />
        </div>

        <div className="flex gap-3 pt-2">
          <Button type="submit" variant="teal" disabled={mutation.isPending}>
            {mutation.isPending ? "Đang tạo…" : "Tạo bộ rubric"}
          </Button>
          <Button type="button" variant="outline" onClick={() => router.back()}>
            Huỷ
          </Button>
        </div>
      </form>
    </div>
  );
}
