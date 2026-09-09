"use client";

import { cn } from "@/lib/utils";
import type { UseMutationResult } from "@tanstack/react-query";
import type { ExamSessionQuestion, SaveAnswerPayload } from "@/types/exam-session";

export function McQuestionCard({
  question,
  saveMutation,
  disabled,
}: {
  question: ExamSessionQuestion;
  saveMutation: UseMutationResult<void, unknown, SaveAnswerPayload>;
  disabled: boolean;
}) {
  return (
    <div
      id={`question-${question.questionId}`}
      className="scroll-mt-24 rounded-sm border border-border bg-paper-raised p-5"
    >
      <p className="text-sm font-medium text-ink">
        Câu {question.questionNumber}. {question.questionText}
      </p>

      <div className="mt-4 space-y-2">
        {question.options.map((opt) => {
          const selected = question.selectedOptionId === opt.questionOptionId;
          return (
            <label
              key={opt.questionOptionId}
              className={cn(
                "flex cursor-pointer items-center gap-3 rounded-sm border px-3.5 py-2.5 text-sm transition-colors",
                selected ? "border-teal bg-teal/5" : "border-border hover:border-teal/40",
                disabled && "cursor-not-allowed opacity-60"
              )}
            >
              <input
                type="radio"
                name={question.questionId}
                className="accent-teal"
                checked={selected}
                disabled={disabled}
                onChange={() =>
                  saveMutation.mutate({
                    questionId: question.questionId,
                    selectedOptionId: opt.questionOptionId,
                  })
                }
              />
              <span className="font-medium text-ink-soft">{opt.optionLabel}.</span>
              <span className="text-ink-soft">{opt.optionText}</span>
            </label>
          );
        })}
      </div>
    </div>
  );
}
