"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { historyApi } from "@/lib/candidate/history-api";
import { Pagination } from "@/components/admin/pagination";
import { emptyPagedResult } from "@/types/pagination";

export default function CandidateHistoryPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError } = useQuery({
    queryKey: ["candidate", "history", page],
    queryFn: () => historyApi.list({ page, pageSize: 20 }),
    placeholderData: (prev) => prev,
  });

  const result = data ?? emptyPagedResult();

  return (
    <div>
      <h1 className="font-serif text-2xl font-medium text-ink">Lịch sử làm bài</h1>
      <p className="mt-1 text-sm text-muted">Danh sách các phiên thi đã nộp.</p>

      <div className="mt-6 overflow-hidden rounded-sm border border-border bg-paper-raised">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-paper text-[13px] text-muted">
            <tr>
              <th className="px-4 py-3 font-medium">Kỳ thi</th>
              <th className="px-4 py-3 font-medium">Ngày nộp</th>
              <th className="px-4 py-3 font-medium">Listening</th>
              <th className="px-4 py-3 font-medium">Reading</th>
              <th className="px-4 py-3 font-medium">Writing</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {isLoading && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-muted">
                  Đang tải…
                </td>
              </tr>
            )}
            {isError && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-danger">
                  Không tải được lịch sử. Kiểm tra kết nối backend.
                </td>
              </tr>
            )}
            {!isLoading && !isError && result.items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-muted">
                  Bạn chưa nộp bài thi nào.
                </td>
              </tr>
            )}
            {result.items.map((item) => (
              <tr key={item.examSessionId} className="transition-colors hover:bg-paper">
                <td className="px-4 py-3">
                  <p className="font-medium text-ink">{item.examName}</p>
                  <p className="text-[13px] text-muted">{item.examCode}</p>
                </td>
                <td className="px-4 py-3 text-ink-soft">
                  {new Date(item.submittedAt).toLocaleDateString("vi-VN")}
                </td>
                <td className="px-4 py-3 text-ink-soft">{item.listeningScore ?? "—"}</td>
                <td className="px-4 py-3 text-ink-soft">{item.readingScore ?? "—"}</td>
                <td className="px-4 py-3 text-ink-soft">
                  {item.writingScore ?? <span className="text-[12px] text-gold">Đang chấm</span>}
                </td>
                <td className="px-4 py-3 text-right">
                  <Link
                    href={`/candidate/results/${item.examSessionId}`}
                    className="text-sm font-medium text-teal hover:underline"
                  >
                    Xem kết quả
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
