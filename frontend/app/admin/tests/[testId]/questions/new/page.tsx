"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { questionsApi } from "@/lib/admin/questions-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FormError } from "@/components/ui/form-error";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  QUESTION_TYPE_LABEL,
  SECTION_LABEL,
  type CreateQuestionPayload,
  type QuestionSection,
  type QuestionType,
} from "@/types/question";

const initialForm: CreateQuestionPayload = {
  section: "LISTENING",
  questionType: "MULTIPLE_CHOICE",
  questionNumber: 1,
  questionText: "",
  imageUrl: "",
  audioUrl: "",
  requiredWords: "",
  points: undefined,
};

export default function NewQuestionPage() {
  const { testId } = useParams<{ testId: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (payload: CreateQuestionPayload) => questionsApi.create(testId, payload),
    onSuccess: (question) => {
      queryClient.invalidateQueries({ queryKey: ["admin", "tests", testId, "questions"] });
      toast.success(`Đã tạo câu ${question.questionNumber}.`);
      router.push(`/admin/tests/${testId}/questions/${question.questionId}`);
    },
    onError: (err) => setError(errorMessage(err, "Không tạo được câu hỏi.")),
  });

  function update<K extends keyof CreateQuestionPayload>(key: K, value: CreateQuestionPayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    mutation.mutate(form);
  }

  const showImage = form.questionType === "PICTURE_SENTENCE";
  const showAudio = form.section === "LISTENING";
  const showRequiredWords = form.questionType === "PICTURE_SENTENCE";

  return (
    <div className="max-w-xl">
      <h1 className="font-serif text-2xl font-medium text-ink">Thêm câu hỏi</h1>
      <p className="mt-1 text-sm text-muted">
        Sau khi tạo, nếu là câu trắc nghiệm bạn sẽ thêm các phương án (A/B/C/D) ở bước tiếp theo.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5" noValidate>
        <FormError message={error} />

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>Phần thi</Label>
            <Select
              value={form.section}
              onValueChange={(v) => update("section", v as QuestionSection)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(SECTION_LABEL).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>Dạng câu hỏi</Label>
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
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="questionNumber">Số thứ tự</Label>
            <Input
              id="questionNumber"
              type="number"
              min={1}
              value={form.questionNumber}
              onChange={(e) => update("questionNumber", Number(e.target.value))}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="points">Điểm tham chiếu</Label>
            <Input
              id="points"
              type="number"
              min={0}
              step="0.1"
              value={form.points ?? ""}
              onChange={(e) => update("points", e.target.value ? Number(e.target.value) : undefined)}
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="questionText">Nội dung câu hỏi / Prompt</Label>
          <Textarea
            id="questionText"
            value={form.questionText}
            onChange={(e) => update("questionText", e.target.value)}
            required
          />
        </div>

        {showAudio && (
          <div className="space-y-2">
            <Label htmlFor="audioUrl">Audio URL</Label>
            <Input
              id="audioUrl"
              placeholder="https://…"
              value={form.audioUrl}
              onChange={(e) => update("audioUrl", e.target.value)}
            />
          </div>
        )}

        {showImage && (
          <div className="space-y-2">
            <Label htmlFor="imageUrl">Image URL</Label>
            <Input
              id="imageUrl"
              placeholder="https://…"
              value={form.imageUrl}
              onChange={(e) => update("imageUrl", e.target.value)}
            />
          </div>
        )}

        {showRequiredWords && (
          <div className="space-y-2">
            <Label htmlFor="requiredWords">Từ/cụm từ bắt buộc</Label>
            <Input
              id="requiredWords"
              placeholder="cách nhau bởi dấu phẩy"
              value={form.requiredWords}
              onChange={(e) => update("requiredWords", e.target.value)}
            />
          </div>
        )}

        <div className="flex gap-3 pt-2">
          <Button type="submit" variant="teal" disabled={mutation.isPending}>
            {mutation.isPending ? "Đang tạo…" : "Tạo câu hỏi"}
          </Button>
          <Button type="button" variant="outline" onClick={() => router.back()}>
            Huỷ
          </Button>
        </div>
      </form>
    </div>
  );
}
