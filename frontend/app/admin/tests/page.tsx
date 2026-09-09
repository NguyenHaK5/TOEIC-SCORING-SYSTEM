"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { testsApi } from "@/lib/admin/tests-api";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/admin/status-badge";
import { Pagination } from "@/components/admin/pagination";
import { emptyPagedResult } from "@/types/pagination";

export default function TestsListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin", "tests", page],
    queryFn: () => testsApi.list({ page, pageSize: 20 }),
    placeholderData: (prev) => prev,
  });

  const result = data ?? emptyPagedResult();

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-ink">Bộ đề (Test)</h1>
          <p className="mt-1 text-sm text-muted">
            Ngân hàng đề — mỗi Test có thể được gán cho nhiều kỳ thi sau khi publish.
          </p>
        </div>
        <Button asChild variant="teal">
          <Link href="/admin/tests/new">Tạo bộ đề</Link>
        </Button>
      </div>

      <div className="mt-6 overflow-hidden rounded-sm border border-border bg-paper-raised">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-paper text-[13px] text-muted">
            <tr>
              <th className="px-4 py-3 font-medium">Mã bộ đề</th>
              <th className="px-4 py-3 font-medium">Tên bộ đề</th>
              <th className="px-4 py-3 font-medium">Phiên bản</th>
              <th className="px-4 py-3 font-medium">Trạng thái</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted">
                  Đang tải…
                </td>
              </tr>
            )}
            {isError && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-danger">
                  Không tải được danh sách bộ đề. Kiểm tra kết nối backend.
                </td>
              </tr>
            )}
            {!isLoading && !isError && result.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted">
                  Chưa có bộ đề nào.
                </td>
              </tr>
            )}
            {result.items.map((test) => (
              <tr key={test.testId} className="transition-colors hover:bg-paper">
                <td className="px-4 py-3 font-medium text-ink">{test.testCode}</td>
                <td className="px-4 py-3 text-ink-soft">{test.testName}</td>
                <td className="px-4 py-3 text-ink-soft">{test.version}</td>
                <td className="px-4 py-3">
                  <StatusBadge status={test.status} />
                </td>
                <td className="px-4 py-3 text-right">
                  <Link
                    href={`/admin/tests/${test.testId}`}
                    className="text-sm font-medium text-teal hover:underline"
                  >
                    Xem chi tiết
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />
    </div>
  );
}
