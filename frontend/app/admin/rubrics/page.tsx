"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { rubricsApi } from "@/lib/admin/rubrics-api";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/admin/status-badge";
import { Pagination } from "@/components/admin/pagination";
import { emptyPagedResult } from "@/types/pagination";
import { QUESTION_TYPE_LABEL } from "@/types/question";

export default function RubricSetsListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin", "rubric-sets", page],
    queryFn: () => rubricsApi.list({ page, pageSize: 20 }),
    placeholderData: (prev) => prev,
  });

  const result = data ?? emptyPagedResult();

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-ink">Rubric</h1>
          <p className="mt-1 text-sm text-muted">
            Bộ tiêu chí chấm Writing theo từng dạng câu. Đúng 1 bộ Active cho mỗi dạng câu.
          </p>
        </div>
        <Button asChild variant="teal">
          <Link href="/admin/rubrics/new">Tạo bộ rubric</Link>
        </Button>
      </div>

      <div className="mt-6 overflow-hidden rounded-sm border border-border bg-paper-raised">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-paper text-[13px] text-muted">
            <tr>
              <th className="px-4 py-3 font-medium">Tên bộ rubric</th>
              <th className="px-4 py-3 font-medium">Dạng câu</th>
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
                  Không tải được danh sách rubric. Kiểm tra kết nối backend.
                </td>
              </tr>
            )}
            {!isLoading && !isError && result.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted">
                  Chưa có bộ rubric nào.
                </td>
              </tr>
            )}
            {result.items.map((rs) => (
              <tr key={rs.rubricSetId} className="transition-colors hover:bg-paper">
                <td className="px-4 py-3 font-medium text-ink">{rs.name}</td>
                <td className="px-4 py-3 text-ink-soft">{QUESTION_TYPE_LABEL[rs.questionType]}</td>
                <td className="px-4 py-3 text-ink-soft">{rs.version}</td>
                <td className="px-4 py-3">
                  <StatusBadge status={rs.status} />
                </td>
                <td className="px-4 py-3 text-right">
                  <Link
                    href={`/admin/rubrics/${rs.rubricSetId}`}
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
