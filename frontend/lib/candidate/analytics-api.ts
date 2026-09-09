import { apiClient } from "@/lib/api-client";
import { buildQuery } from "@/lib/admin/query";
import type { CandidateAnalytics } from "@/types/candidate-analytics";

export const analyticsApi = {
  get: (trendLimit = 20) =>
    apiClient.get<CandidateAnalytics>(`/candidates/me/analytics${buildQuery({ trendLimit })}`),
};
