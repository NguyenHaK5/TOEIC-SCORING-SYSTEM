"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { adminDashboardApi } from "@/lib/admin/dashboard-api";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { ScoreSummaryStats } from "@/types/admin-dashboard";
import { QUESTION_TYPE_LABEL, type QuestionType } from "@/types/question";

export default function AdminDashboardPage() {
  const [topN, setTopN] = useState(20);

  const { data: overview, isLoading: overviewLoading, isError: overviewError } = useQuery({
    queryKey: ["admin", "dashboard", "overview"],
    queryFn: () => adminDashboardApi.overview(),
  });

  const { data: errors, isLoading: errorsLoading, isError: errorsError } = useQuery({
    queryKey: ["admin", "dashboard", "writing-errors", topN],
    queryFn: () => adminDashboardApi.writingErrors(topN),
  });

  return (
    <div className="max-w-4xl">
      <h1 className="font-serif text-2xl font-medium text-ink">Dashboard hệ thống</h1>
      <p className="mt-1 text-sm text-muted">Tổng quan số liệu và lỗi Writing thường gặp.</p>

      {overviewLoading && <p className="mt-6 text-sm text-muted">Đang tải…</p>}
      {overviewError && (
        <p className="mt-6 rounded-sm border border-danger/30 bg-danger/5 px-3.5 py-2.5 text-sm text-danger">
          Không tải được tổng quan. Kiểm tra kết nối backend.
        </p>
      )}

      {overview && (
        <>
          <div className="mt-6 grid grid-cols-3 gap-4">
            <CountCard label="Kỳ thi" value={overview.totalExams} />
            <CountCard label="Thí sinh" value={overview.totalCandidates} />
            <CountCard label="Phiên đã nộp" value={overview.totalExamSessions} />
          </div>

          <div className="mt-8">
            <h2 className="font-serif text-lg font-medium text-ink">Thống kê điểm từng phần</h2>
            <p className="mt-1 text-[13px] text-muted">
              Điểm thô, chưa quy đổi thang TOEIC 5–495 (để ngỏ — xem PHASE0).
            </p>
            <div className="mt-4 grid grid-cols-3 gap-4">
              <ScoreStatsCard title="Listening" stats={overview.listeningScoreSummary} />
              <ScoreStatsCard title="Reading" stats={overview.readingScoreSummary} />
              <ScoreStatsCard title="Writing" stats={overview.writingScoreSummary} />
            </div>
          </div>
        </>
      )}

      <div className="mt-10 border-t border-border pt-6">
        <div className="flex items-center justify-between">
          <h2 className="font-serif text-lg font-medium text-ink">Lỗi Writing phổ biến nhất</h2>
          <div className="flex items-center gap-2">
            <Label htmlFor="topN" className="text-[12px]">
              Top
            </Label>
            <Input
              id="topN"
              type="number"
              min={1}
              max={100}
              value={topN}
              onChange={(e) => setTopN(Number(e.target.value) || 20)}
              className="w-20"
            />
          </div>
        </div>

        {errorsLoading && <p className="mt-4 text-sm text-muted">Đang tải…</p>}
        {errorsError && (
          <p className="mt-4 rounded-sm border border-danger/30 bg-danger/5 px-3.5 py-2.5 text-sm text-danger">
            Không tải được thống kê lỗi. Kiểm tra kết nối backend.
          </p>
        )}
        {errors && (
          <>
            <p className="mt-2 text-[12px] text-muted">
              Phân tích trên {errors.totalAssessmentsAnalyzed} bài đã được AI chấm.
            </p>
            {errors.unparseableErrorsJsonCount > 0 && (
              <p className="mt-4 rounded-sm border border-gold/30 bg-gold/5 px-3.5 py-2.5 text-[13px] text-gold">
                {errors.unparseableErrorsJsonCount} bản ghi ErrorsJson không parse được — số liệu
                bên dưới có thể chưa đầy đủ (xem PHASE6-ISSUE-06).
              </p>
            )}
            {errors.topErrors.length === 0 ? (
              <p className="mt-4 text-sm text-muted">Chưa có dữ liệu lỗi Writing.</p>
            ) : (
              <ul className="mt-4 space-y-2">
                {errors.topErrors.map((e, i) => (
                  <li
                    key={`${e.errorType}-${i}`}
                    className="flex items-center justify-between rounded-sm border border-border bg-paper-raised px-4 py-3"
                  >
                    <div>
                      <span className="text-sm text-ink-soft">{e.errorType}</span>
                      <span className="ml-2 text-[12px] text-muted">
                        {QUESTION_TYPE_LABEL[e.questionType as QuestionType] ?? e.questionType}
                        {e.criterion !== "UNSPECIFIED" && ` · ${e.criterion}`}
                      </span>
                    </div>
                    <span className="font-serif text-lg text-ink">{e.count}</span>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </div>
    </div>
  );
}

function CountCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-sm border border-border bg-paper-raised p-5">
      <p className="text-[13px] text-muted">{label}</p>
      <p className="mt-1 font-serif text-3xl font-medium text-ink">{value}</p>
    </div>
  );
}

function ScoreStatsCard({ title, stats }: { title: string; stats: ScoreSummaryStats }) {
  return (
    <div className="rounded-sm border border-border bg-paper-raised p-4">
      <p className="text-[13px] text-muted">{title}</p>
      {stats.sampleSize === 0 ? (
        <p className="mt-2 text-sm text-muted">Chưa có dữ liệu</p>
      ) : (
        <dl className="mt-2 space-y-1 text-[13px]">
          <Row label="Trung bình" value={stats.average} />
          <Row label="Trung vị" value={stats.median} />
          <Row label="Thấp nhất" value={stats.min} />
          <Row label="Cao nhất" value={stats.max} />
          <Row label="Số bài" value={stats.sampleSize} />
        </dl>
      )}
    </div>
  );
}

function Row({ label, value }: { label: string; value: number | null }) {
  return (
    <div className="flex justify-between">
      <dt className="text-muted">{label}</dt>
      <dd className="text-ink-soft">{value ?? "—"}</dd>
    </div>
  );
}
