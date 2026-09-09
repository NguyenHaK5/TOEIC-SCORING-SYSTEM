"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { candidateExamsApi } from "@/lib/candidate/candidate-exams-api";
import { StatusBadge } from "@/components/admin/status-badge";

export default function CandidateExamsListPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["candidate", "exams", "available"],
    queryFn: () => candidateExamsApi.listAvailable(),
  });

  return (
    <div className="max-w-2xl">
      <h1 className="font-serif text-2xl font-medium text-ink">Kỳ thi</h1>
      <p className="mt-1 text-sm text-muted">
        Danh sách kỳ thi bạn có thể tham gia. Mỗi kỳ thi chỉ cho phép 1 phiên
        làm bài đang diễn ra tại một thời điểm.
      </p>

      <div className="mt-6 space-y-3">
        {isLoading && <p className="text-sm text-muted">Đang tải…</p>}
        {isError && (
          <p className="rounded-sm border border-danger/30 bg-danger/5 px-3.5 py-2.5 text-sm text-danger">
            Không tải được danh sách kỳ thi. Kiểm tra kết nối backend.
          </p>
        )}
        {!isLoading && !isError && (data ?? []).length === 0 && (
          <p className="text-sm text-muted">Hiện chưa có kỳ thi nào khả dụng.</p>
        )}
        {(data ?? []).map((exam) => (
          <Link
            key={exam.examId}
            href={`/candidate/exams/${exam.examId}`}
            className="flex items-center justify-between rounded-sm border border-border bg-paper-raised p-4 transition-colors hover:border-teal"
          >
            <div>
              <p className="font-serif text-base font-medium text-ink">{exam.examName}</p>
              <p className="mt-0.5 text-[13px] text-muted">
                {exam.examCode} · {exam.examDate}
              </p>
            </div>
            <StatusBadge status={exam.status} />
          </Link>
        ))}
      </div>
    </div>
  );
}
