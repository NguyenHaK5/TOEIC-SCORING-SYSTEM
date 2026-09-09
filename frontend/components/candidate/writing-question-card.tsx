"use client";

import { useRef, useState } from "react";
import type { UseMutationResult } from "@tanstack/react-query";
import { Textarea } from "@/components/ui/textarea";
import type { ExamSessionQuestion, SaveAnswerPayload } from "@/types/exam-session";

const SAVE_DEBOUNCE_MS = 1200;

export function WritingQuestionCard({
  question,
  saveMutation,
  disabled,
}: {
  question: ExamSessionQuestion;
  saveMutation: UseMutationResult<void, unknown, SaveAnswerPayload>;
  disabled: boolean;
}) {
  // Khởi tạo trực tiếp từ prop — component được key={question.questionId} ở
  // nơi gọi nên remount đúng khi chuyển câu, không cần effect đồng bộ.
  const [text, setText] = useState(question.answerText ?? "");
  const [savedAt, setSavedAt] = useState<Date | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  function scheduleSave(value: string) {
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      saveMutation.mutate(
        { questionId: question.questionId, answerText: value },
        { onSuccess: () => setSavedAt(new Date()) }
      );
    }, SAVE_DEBOUNCE_MS);
  }

  function handleChange(value: string) {
    setText(value);
    scheduleSave(value);
  }

  const requiredWords = question.requiredWords
    ? question.requiredWords.split(",").map((w) => w.trim()).filter(Boolean)
    : [];

  return (
    <div
      id={`question-${question.questionId}`}
      className="scroll-mt-24 rounded-sm border border-border bg-paper-raised p-5"
    >
      <div className="flex items-start justify-between gap-4">
        <p className="text-sm font-medium text-ink">Câu {question.questionNumber}</p>
        <SaveStatus pending={saveMutation.isPending} savedAt={savedAt} />
      </div>

      <p className="mt-2 whitespace-pre-wrap text-sm text-ink-soft">{question.questionText}</p>

      {question.imageUrl && (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={question.imageUrl}
          alt={`Tranh minh hoạ câu ${question.questionNumber}`}
          className="mt-3 max-h-72 rounded-sm border border-border object-contain"
        />
      )}

      {requiredWords.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-1.5">
          {requiredWords.map((w) => (
            <span
              key={w}
              className="rounded-full border border-gold/30 bg-gold/5 px-2.5 py-0.5 text-[12px] text-gold"
            >
              {w}
            </span>
          ))}
        </div>
      )}

      <Textarea
        className="mt-4 min-h-40"
        value={text}
        disabled={disabled}
        placeholder="Nhập câu trả lời của bạn…"
        onChange={(e) => handleChange(e.target.value)}
      />
      <p className="mt-1.5 text-right text-[12px] text-muted">{wordCount(text)} từ</p>
    </div>
  );
}

function SaveStatus({ pending, savedAt }: { pending: boolean; savedAt: Date | null }) {
  if (pending) return <span className="text-[12px] text-muted">Đang lưu…</span>;
  if (savedAt)
    return (
      <span className="text-[12px] text-success">
        Đã lưu lúc {savedAt.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })}
      </span>
    );
  return null;
}

function wordCount(text: string): number {
  return text.trim().length === 0 ? 0 : text.trim().split(/\s+/).length;
}
