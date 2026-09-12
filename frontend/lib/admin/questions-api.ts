import { apiClient } from "@/lib/api-client";
import type {
  CreateQuestionOptionPayload,
  CreateQuestionPayload,
  Question,
  QuestionOption,
  UpdateQuestionOptionPayload,
  UpdateQuestionPayload,
} from "@/types/question";

import { 
  BulkQuestionRow, 
  BulkCreateQuestionsResult, 
  QuestionExportRow, 
  BulkUpdateQuestionRow, 
  BulkUpdateQuestionsResult 
} from "@/types/question-import";

export const questionsApi = {
  listByTest: (testId: string) => apiClient.get<Question[]>(`/tests/${testId}/questions`),

  create: (testId: string, payload: CreateQuestionPayload) =>
    apiClient.post<Question>(`/tests/${testId}/questions`, payload),

  get: (id: string) => apiClient.get<Question>(`/questions/${id}`),

  update: (id: string, payload: UpdateQuestionPayload) =>
    apiClient.put<Question>(`/questions/${id}`, payload),

  remove: (id: string) => apiClient.delete<void>(`/questions/${id}`),

  addOption: (questionId: string, payload: CreateQuestionOptionPayload) =>
    apiClient.post<QuestionOption>(`/questions/${questionId}/options`, payload),

  updateOption: (optionId: string, payload: UpdateQuestionOptionPayload) =>
    apiClient.put<QuestionOption>(`/question-options/${optionId}`, payload),

  removeOption: (optionId: string) => apiClient.delete<void>(`/question-options/${optionId}`),
};

// File: frontend/lib/admin/questions-api.ts — thêm function
export async function bulkCreateQuestions(testId: string, rows: BulkQuestionRow[]) {
  return apiClient.post<BulkCreateQuestionsResult>(
    `/tests/${testId}/questions/bulk`,
    { rows },
  );
}

export async function fetchQuestionsForExport(testId: string) {
  return apiClient.get<QuestionExportRow[]>(`/tests/${testId}/questions/export`);
}

export async function bulkUpdateQuestions(testId: string, rows: BulkUpdateQuestionRow[]) {
  return apiClient.put<BulkUpdateQuestionsResult>(
    `/tests/${testId}/questions/bulk`,
    { rows },
  );
}
