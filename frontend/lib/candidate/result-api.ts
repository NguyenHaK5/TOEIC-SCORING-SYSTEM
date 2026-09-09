import { apiClient } from "@/lib/api-client";
import type { ExamSessionResult } from "@/types/candidate-result";

export const resultApi = {
  get: (sessionId: string) => apiClient.get<ExamSessionResult>(`/exam-sessions/${sessionId}/result`),
};
