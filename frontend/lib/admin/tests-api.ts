import { apiClient } from "@/lib/api-client";
import { buildQuery } from "@/lib/admin/query";
import type { PagedResult } from "@/types/pagination";
import type {
  CreateTestPayload,
  PublishTestResult,
  Test,
  TestStatus,
  UpdateTestPayload,
} from "@/types/test";

export const testsApi = {
  list: (params: { page?: number; pageSize?: number; status?: TestStatus; search?: string } = {}) =>
    apiClient.get<PagedResult<Test>>(`/tests${buildQuery(params)}`),

  get: (id: string) => apiClient.get<Test>(`/tests/${id}`),

  create: (payload: CreateTestPayload) => apiClient.post<Test>(`/tests`, payload),

  update: (id: string, payload: UpdateTestPayload) =>
    apiClient.put<Test>(`/tests/${id}`, payload),

  /**
   * README.md: publish validate toàn bộ câu hỏi, liệt kê đầy đủ lỗi thay vì
   * dừng ở lỗi đầu tiên. GIẢ ĐỊNH: khi có lỗi, backend trả 400 kèm body
   * `{ errors: [...] }` — nếu backend trả 200 kèm `{success:false,errors}`
   * thay vì throw, chỉ cần đổi 1 chỗ dùng hàm này trong
   * `admin/tests/[testId]/page.tsx`.
   */
  publish: (id: string) => apiClient.post<PublishTestResult>(`/tests/${id}/publish`),

  archive: (id: string) => apiClient.post<Test>(`/tests/${id}/archive`),
};
