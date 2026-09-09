import { apiClient } from "@/lib/api-client";
import { buildQuery } from "@/lib/admin/query";
import type { PagedResult } from "@/types/pagination";
import type {
  CreateRubricPayload,
  CreateRubricSetPayload,
  Rubric,
  RubricSet,
  RubricSetStatus,
  UpdateRubricPayload,
  UpdateRubricSetPayload,
} from "@/types/rubric";
import type { QuestionType } from "@/types/question";

/** Xem PHASE7-ISSUE-07 (PHASE7_PART2_INTEGRATION_GUIDE.md) — toàn bộ path
 * dưới đây là giả định, CẦN Dragon đối chiếu với Phase 3 Part 1 thật. */
export const rubricsApi = {
  list: (
    params: { page?: number; pageSize?: number; questionType?: QuestionType; status?: RubricSetStatus } = {}
  ) => apiClient.get<PagedResult<RubricSet>>(`/rubric-sets${buildQuery(params)}`),

  get: (id: string) => apiClient.get<RubricSet>(`/rubric-sets/${id}`),

  create: (payload: CreateRubricSetPayload) => apiClient.post<RubricSet>(`/rubric-sets`, payload),

  update: (id: string, payload: UpdateRubricSetPayload) =>
    apiClient.put<RubricSet>(`/rubric-sets/${id}`, payload),

  remove: (id: string) => apiClient.delete<void>(`/rubric-sets/${id}`),

  activate: (id: string) => apiClient.post<RubricSet>(`/rubric-sets/${id}/activate`),

  archive: (id: string) => apiClient.post<RubricSet>(`/rubric-sets/${id}/archive`),

  addRubric: (rubricSetId: string, payload: CreateRubricPayload) =>
    apiClient.post<Rubric>(`/rubric-sets/${rubricSetId}/rubrics`, payload),

  updateRubric: (rubricId: string, payload: UpdateRubricPayload) =>
    apiClient.put<Rubric>(`/rubrics/${rubricId}`, payload),

  removeRubric: (rubricId: string) => apiClient.delete<void>(`/rubrics/${rubricId}`),
};
