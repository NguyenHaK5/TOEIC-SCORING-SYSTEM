"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { examsApi } from "@/lib/admin/exams-api";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/admin/status-badge";
import { Pagination } from "@/components/admin/pagination";
import { emptyPagedResult } from "@/types/pagination";
import type { ExamStatus } from "@/types/exam";

export default function ExamsListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin", "exams", page],
    queryFn: () => examsApi.list({ page, pageSize: 20 }),
    placeholderData: (prev) => prev,
  });

  const result = data ?? emptyPagedResult();

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-serif text-2xl font-medium text-ink">Kỳ thi</h1>
          <p className="mt-1 text-sm text-muted">
            Quản lý kỳ thi, gán bộ đề, chuyển trạng thái theo lịch tổ chức.
          </p>
        </div>
        <Button asChild variant="teal">
          <Link href="/admin/exams/new">Tạo kỳ thi</Link>
        </Button>
      </div>

      <div className="mt-6 overflow-hidden rounded-sm border border-border bg-paper-raised">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-paper text-[13px] text-muted">
            <tr>
              <th className="px-4 py-3 font-medium">Mã kỳ thi</th>
              <th className="px-4 py-3 font-medium">Tên kỳ thi</th>
              <th className="px-4 py-3 font-medium">Ngày thi</th>
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
                  Không tải được danh sách kỳ thi. Kiểm tra kết nối backend.
                </td>
              </tr>
            )}
            {!isLoading && !isError && result.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted">
                  Chưa có kỳ thi nào.
                </td>
              </tr>
            )}
            {result.items.map((exam) => (
              <tr key={exam.examId} className="transition-colors hover:bg-paper">
                <td className="px-4 py-3 font-medium text-ink">{exam.examCode}</td>
                <td className="px-4 py-3 text-ink-soft">{exam.examName}</td>
                <td className="px-4 py-3 text-ink-soft">{exam.examDate}</td>
                <td className="px-4 py-3">
                  <StatusBadge status={exam.status as ExamStatus} />
                </td>
                <td className="px-4 py-3 text-right">
                  <Link
                    href={`/admin/exams/${exam.examId}`}
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
