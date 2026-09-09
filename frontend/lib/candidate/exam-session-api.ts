import { apiClient } from "@/lib/api-client";
import type {
  ExamSessionQuestion,
  ExamSessionSummary,
  SaveAnswerPayload,
  StartExamSessionPayload,
} from "@/types/exam-session";

export const examSessionApi = {
  /** Idempotent: nếu đã có phiên IN_PROGRESS cho Exam này, backend resume
   * thay vì tạo mới (README.md Part 4). */
  start: (payload: StartExamSessionPayload) =>
    apiClient.post<ExamSessionSummary>(`/exam-sessions`, payload),

  get: (sessionId: string) => apiClient.get<ExamSessionSummary>(`/exam-sessions/${sessionId}`),

  getQuestions: (sessionId: string) =>
    apiClient.get<ExamSessionQuestion[]>(`/exam-sessions/${sessionId}/questions`),

  saveAnswer: (sessionId: string, payload: SaveAnswerPayload) =>
    apiClient.post<void>(`/exam-sessions/${sessionId}/answers`, payload),

  /** Idempotent — gọi lại sau khi đã nộp không lỗi (README.md Part 4). */
  submit: (sessionId: string) => apiClient.post<ExamSessionSummary>(`/exam-sessions/${sessionId}/submit`),
};
