"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { examSessionApi } from "@/lib/candidate/exam-session-api";
import type { ExamSessionQuestion, SaveAnswerPayload } from "@/types/exam-session";

export function useSaveAnswer(sessionId: string) {
  const queryClient = useQueryClient();
  const queryKey = ["candidate", "exam-sessions", sessionId, "questions"];

  return useMutation({
    mutationFn: (payload: SaveAnswerPayload) => examSessionApi.saveAnswer(sessionId, payload),
    onMutate: async (payload) => {
      // Cập nhật cache ngay (optimistic) để palette/ô nhập phản ánh tức thì,
      // không đợi round-trip mạng — auto-save không nên tạo cảm giác "giật".
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<ExamSessionQuestion[]>(queryKey);
      queryClient.setQueryData<ExamSessionQuestion[]>(queryKey, (old) =>
        old?.map((q) =>
          q.questionId === payload.questionId
            ? {
                ...q,
                selectedOptionId: payload.selectedOptionId ?? q.selectedOptionId,
                answerText: payload.answerText ?? q.answerText,
              }
            : q
        )
      );
      return { previous };
    },
    onError: (_err, _payload, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKey, context.previous);
      }
    },
  });
}
