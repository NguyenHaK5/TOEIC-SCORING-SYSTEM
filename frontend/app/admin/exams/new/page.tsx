"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { examsApi } from "@/lib/admin/exams-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FormError } from "@/components/ui/form-error";
import type { CreateExamPayload } from "@/types/exam";

const initialForm: CreateExamPayload = {
  examCode: "",
  examName: "",
  description: "",
  examDate: "",
  startTime: "",
  endTime: "",
};

export default function NewExamPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (payload: CreateExamPayload) => examsApi.create(payload),
    onSuccess: (exam) => {
      queryClient.invalidateQueries({ queryKey: ["admin", "exams"] });
      toast.success(`Đã tạo kỳ thi "${exam.examName}".`);
      router.push(`/admin/exams/${exam.examId}`);
    },
    onError: (err) => setError(errorMessage(err, "Không tạo được kỳ thi.")),
  });

  function update<K extends keyof CreateExamPayload>(key: K, value: CreateExamPayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    mutation.mutate({
      ...form,
      startTime: toIsoDateTime(form.examDate, form.startTime),
      endTime: toIsoDateTime(form.examDate, form.endTime),
    });
  }

  return (
    <div className="max-w-xl">
      <h1 className="font-serif text-2xl font-medium text-ink">Tạo kỳ thi</h1>
      <p className="mt-1 text-sm text-muted">
        Sau khi tạo, gán bộ đề (Test) đã publish rồi chuyển trạng thái sang
        &quot;Đã lên lịch&quot;.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5" noValidate>
        <FormError message={error} />

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="examCode">Mã kỳ thi</Label>
            <Input
              id="examCode"
              placeholder="TOEIC-2026-08"
              value={form.examCode}
              onChange={(e) => update("examCode", e.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="examDate">Ngày thi</Label>
            <Input
              id="examDate"
              type="date"
              value={form.examDate}
              onChange={(e) => update("examDate", e.target.value)}
              required
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="examName">Tên kỳ thi</Label>
          <Input
            id="examName"
            value={form.examName}
            onChange={(e) => update("examName", e.target.value)}
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

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="startTime">Giờ bắt đầu</Label>
            <Input
              id="startTime"
              type="time"
              value={form.startTime}
              onChange={(e) => update("startTime", e.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="endTime">Giờ kết thúc</Label>
            <Input
              id="endTime"
              type="time"
              value={form.endTime}
              onChange={(e) => update("endTime", e.target.value)}
              required
            />
          </div>
        </div>

        <div className="flex gap-3 pt-2">
          <Button type="submit" variant="teal" disabled={mutation.isPending}>
            {mutation.isPending ? "Đang tạo…" : "Tạo kỳ thi"}
          </Button>
          <Button type="button" variant="outline" onClick={() => router.back()}>
            Huỷ
          </Button>
        </div>
      </form>
    </div>
  );
}

/**
 * Input type="date" + type="time" tách rời để UX dễ nhập hơn, gộp lại
 * thành ISO datetime khi submit — README.md ví dụ `startTime`/`endTime`
 * dùng format `"2026-08-30T08:00:00Z"` (UTC). GIẢ ĐỊNH: giờ nhập là giờ
 * local của Admin, quy đổi qua UTC khi gửi backend. Nếu backend thật cần
 * giờ theo timezone khác, sửa hàm này.
 */
function toIsoDateTime(date: string, time: string): string {
  if (!date || !time) return "";
  return new Date(`${date}T${time}:00`).toISOString();
}
