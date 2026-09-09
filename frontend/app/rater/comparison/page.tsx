"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { raterApi } from "@/lib/rater/rater-api";
import { Pagination } from "@/components/admin/pagination";
import { emptyPagedResult } from "@/types/pagination";
import { QUESTION_TYPE_LABEL } from "@/types/question";
import { cn } from "@/lib/utils";

export default function ComparisonPage() {
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["rater", "comparison", page],
    queryFn: () => raterApi.comparison({ page, pageSize: 20 }),
    placeholderData: (prev) => prev,
  });

  const result = data ?? emptyPagedResult();

  return (
    <div>
      <h1 className="font-serif text-2xl font-medium text-ink">So sánh AI vs Human</h1>
      <p className="mt-1 text-sm text-muted">
        Sắp xếp theo mức lệch điểm tổng giảm dần — ưu tiên xem lại các
        trường hợp AI và Rater chấm khác nhau nhiều nhất.
      </p>

      <div className="mt-6 overflow-hidden rounded-sm border border-border bg-paper-raised">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-paper text-[13px] text-muted">
            <tr>
              <th className="px-4 py-3 font-medium">Thí sinh</th>
              <th className="px-4 py-3 font-medium">Câu</th>
              <th className="px-4 py-3 font-medium">Điểm AI</th>
              <th className="px-4 py-3 font-medium">Điểm Human</th>
              <th className="px-4 py-3 font-medium">Mức lệch</th>
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
                  Không tải được dữ liệu so sánh. Kiểm tra kết nối backend.
                </td>
              </tr>
            )}
            {!isLoading && !isError && result.items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-muted">
                  Chưa có cặp AI/Human nào để so sánh.
                </td>
              </tr>
            )}
            {result.items.map((item) => {
              const absDiff = Math.abs(item.overallDifference);
              return (
                <tr key={item.candidateAnswerId} className="transition-colors hover:bg-paper">
                  <td className="px-4 py-3 text-ink-soft">{item.candidateName}</td>
                  <td className="px-4 py-3 text-ink-soft">
                    Câu {item.questionNumber}
                    <span className="ml-1.5 text-[12px] text-muted">
                      {QUESTION_TYPE_LABEL[item.questionType]}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-ink-soft">{item.aiScore}</td>
                  <td className="px-4 py-3 text-ink-soft">{item.humanScore}</td>
                  <td className="px-4 py-3">
                    <span
                      className={cn(
                        "font-medium",
                        absDiff >= 15 ? "text-danger" : absDiff >= 7 ? "text-gold" : "text-success"
                      )}
                    >
                      {item.overallDifference > 0 ? "+" : ""}
                      {item.overallDifference}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link
                      href={`/rater/writing-answers/${item.candidateAnswerId}`}
                      className="text-sm font-medium text-teal hover:underline"
                    >
                      Xem lại
                    </Link>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />
    </div>
  );
}
