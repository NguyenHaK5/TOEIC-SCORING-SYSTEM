import { useAuthStore } from "@/lib/auth-store";
import { buildQuery } from "@/lib/admin/query";
import { ApiError } from "@/lib/api-client";
import type { DatasetSummary, MetricsResult, ReportResult } from "@/types/evaluation";
import type { QuestionType } from "@/types/question";

/**
 * Client riêng biệt cho `evaluation-service` (Python, Phase 5) — KHÔNG
 * dùng chung `apiClient` (gọi thẳng Toeic.Api). Đúng quyết định
 * PHASE7-ISSUE-03: đi qua Route Handler proxy `/api/evaluation/*`
 * (server-side, giữ base URL evaluation-service khỏi lộ ra trình duyệt),
 * kèm Bearer accessToken hiện có để proxy có cơ sở tối thiểu chặn request
 * nặc danh (xem PHASE7-ISSUE-16).
 */

async function evalFetch<T>(
  path: string,
  params: Record<string, string | number | undefined> = {}
): Promise<T> {
  const accessToken = useAuthStore.getState().accessToken;
  const res = await fetch(`/api/evaluation${path}${buildQuery(params)}`, {
    headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new ApiError(res.status, typeof data?.title === "string" ? data.title : "Lỗi evaluation-service.");
  }
  return (await res.json()) as T;
}

export const evaluationApi = {
  dataset: () => evalFetch<DatasetSummary>("/dataset"),

  metrics: (params: { questionType?: QuestionType; agreementThreshold?: number } = {}) =>
    evalFetch<MetricsResult>("/metrics", params),

  report: (params: { questionType?: QuestionType; minDifference?: number; topN?: number } = {}) =>
    evalFetch<ReportResult>("/report", params),

  /** Tải file export (CSV/JSON) kèm Authorization header — không thể dùng
   * thẻ `<a href>` thuần vì cần đính header, phải fetch thủ công rồi tự
   * tạo link tải xuống. */
  async downloadExport(params: {
    format: "csv" | "json";
    level: "overall" | "criterion";
  }): Promise<void> {
    const accessToken = useAuthStore.getState().accessToken;
    const res = await fetch(`/api/evaluation/report/export${buildQuery(params)}`, {
      headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
    });
    if (!res.ok) {
      throw new ApiError(res.status, "Không tải được file export.");
    }
    const blob = await res.blob();
    const disposition = res.headers.get("content-disposition");
    const filenameMatch = disposition?.match(/filename="?([^"]+)"?/);
    const filename = filenameMatch?.[1] ?? `evaluation-report.${params.format}`;

    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  },
};
