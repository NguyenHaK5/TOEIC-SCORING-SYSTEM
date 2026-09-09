import { apiClient } from "@/lib/api-client";
import { buildQuery } from "@/lib/admin/query";
import type { PagedResult } from "@/types/pagination";
import type { CreateExamPayload, Exam, ExamStatus, UpdateExamPayload } from "@/types/exam";

export const examsApi = {
  list: (params: { page?: number; pageSize?: number; status?: ExamStatus; search?: string } = {}) =>
    apiClient.get<PagedResult<Exam>>(`/exams${buildQuery(params)}`),

  get: (id: string) => apiClient.get<Exam>(`/exams/${id}`),

  create: (payload: CreateExamPayload) => apiClient.post<Exam>(`/exams`, payload),

  update: (id: string, payload: UpdateExamPayload) =>
    apiClient.put<Exam>(`/exams/${id}`, payload),

  remove: (id: string) => apiClient.delete<void>(`/exams/${id}`),

  changeStatus: (id: string, status: ExamStatus) =>
    apiClient.post<Exam>(`/exams/${id}/status`, { status }),

  assignTest: (examId: string, testId: string) =>
    apiClient.post<void>(`/exams/${examId}/tests`, { testId }),

  unassignTest: (examId: string, testId: string) =>
    apiClient.delete<void>(`/exams/${examId}/tests/${testId}`),
};
