"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { raterApi } from "@/lib/rater/rater-api";
import { Pagination } from "@/components/admin/pagination";
import { emptyPagedResult } from "@/types/pagination";
import { QUESTION_TYPE_LABEL, type QuestionType } from "@/types/question";
import type { RaterFilterStatus } from "@/types/rater";
import { cn } from "@/lib/utils";

const STATUS_TABS: { value: RaterFilterStatus; label: string }[] = [
  { value: "pending", label: "Chờ chấm" },
  { value: "rated", label: "Đã chấm" },
  { value: "all", label: "Tất cả" },
];

export default function WritingAnswersListPage() {
  const [status, setStatus] = useState<RaterFilterStatus>("pending");
  const [questionType, setQuestionType] = useState<QuestionType | "ALL">("ALL");
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["rater", "writing-answers", status, questionType, page],
    queryFn: () =>
      raterApi.list({
        status,
        questionType: questionType === "ALL" ? undefined : questionType,
        page,
        pageSize: 20,
      }),
    placeholderData: (prev) => prev,
  });

  const result = data ?? emptyPagedResult();

  function changeStatus(next: RaterFilterStatus) {
    setStatus(next);
    setPage(1);
  }

  return (
    <div>
      <h1 className="font-serif text-2xl font-medium text-ink">Bài Writing cần chấm</h1>
      <p className="mt-1 text-sm text-muted">
        Chấm điểm theo từng tiêu chí Rubric, so sánh với đánh giá AI hiện hành.
      </p>

      <div className="mt-6 flex items-center justify-between">
        <div className="flex gap-1 border-b border-border">
          {STATUS_TABS.map((tab) => (
            <button
              key={tab.value}
              type="button"
              onClick={() => changeStatus(tab.value)}
              className={cn(
                "px-4 py-2.5 text-sm font-medium transition-colors",
                status === tab.value
                  ? "border-b-2 border-teal text-teal-dark"
                  : "text-muted hover:text-ink"
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <select
          value={questionType}
          onChange={(e) => {
            setQuestionType(e.target.value as QuestionType | "ALL");
            setPage(1);
          }}
          className="h-9 rounded-sm border border-border bg-paper-raised px-2.5 text-[13px] text-ink-soft"
        >
          <option value="ALL">Mọi dạng câu</option>
          {Object.entries(QUESTION_TYPE_LABEL).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </div>

      <div className="mt-4 overflow-hidden rounded-sm border border-border bg-paper-raised">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-paper text-[13px] text-muted">
            <tr>
              <th className="px-4 py-3 font-medium">Thí sinh</th>
              <th className="px-4 py-3 font-medium">Câu</th>
              <th className="px-4 py-3 font-medium">Nộp lúc</th>
              <th className="px-4 py-3 font-medium">Điểm AI</th>
              <th className="px-4 py-3 font-medium">Điểm Human</th>
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
                  Không tải được danh sách. Kiểm tra kết nối backend.
                </td>
              </tr>
            )}
            {!isLoading && !isError && result.items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-muted">
                  Không có bài nào phù hợp bộ lọc.
                </td>
              </tr>
            )}
            {result.items.map((item) => (
              <tr key={item.candidateAnswerId} className="transition-colors hover:bg-paper">
                <td className="px-4 py-3">
                  <p className="font-medium text-ink">{item.candidateName}</p>
                  <p className="text-[13px] text-muted">{item.candidateCode}</p>
                </td>
                <td className="px-4 py-3 text-ink-soft">
                  Câu {item.questionNumber}
                  <span className="ml-1.5 text-[12px] text-muted">
                    {QUESTION_TYPE_LABEL[item.questionType]}
                  </span>
                </td>
                <td className="px-4 py-3 text-ink-soft">
                  {new Date(item.answeredAt).toLocaleString("vi-VN")}
                </td>
                <td className="px-4 py-3 text-ink-soft">{item.aiScore ?? "—"}</td>
                <td className="px-4 py-3 text-ink-soft">{item.humanScore ?? "—"}</td>
                <td className="px-4 py-3 text-right">
                  <Link
                    href={`/rater/writing-answers/${item.candidateAnswerId}`}
                    className="text-sm font-medium text-teal hover:underline"
                  >
                    {item.isRated ? "Xem lại" : "Chấm điểm"}
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
