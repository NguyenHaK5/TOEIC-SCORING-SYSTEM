import { apiClient } from "@/lib/api-client";
import { buildQuery } from "@/lib/admin/query";
import type { PagedResult } from "@/types/pagination";
import type { ExamHistoryItem } from "@/types/candidate-history";

export const historyApi = {
  list: (params: { page?: number; pageSize?: number } = {}) =>
    apiClient.get<PagedResult<ExamHistoryItem>>(`/candidates/me/history${buildQuery(params)}`),
};
