import { apiClient } from "@/lib/api-client";
import { buildQuery } from "@/lib/admin/query";
import type { AdminDashboardOverview, WritingErrorAnalytics } from "@/types/admin-dashboard";

export const adminDashboardApi = {
  overview: () => apiClient.get<AdminDashboardOverview>(`/admin/dashboard/overview`),

  writingErrors: (topN = 20) =>
    apiClient.get<WritingErrorAnalytics>(`/admin/dashboard/writing-errors${buildQuery({ topN })}`),
};
