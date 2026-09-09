"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { resultApi } from "@/lib/candidate/result-api";
import { SectionScoreCard } from "@/components/candidate/section-score-card";
import { WritingStatusBadge } from "@/components/candidate/writing-status-badge";
import { writingOverallStatus } from "@/types/candidate-result";

export default function ExamResultPage() {
  const { sessionId } = useParams<{ sessionId: string }>();

  const { data: result, isLoading, isError } = useQuery({
    queryKey: ["candidate", "exam-sessions", sessionId, "result"],
    queryFn: () => resultApi.get(sessionId),
    // Writing được chấm bất đồng bộ (background job) — tự đồng bộ lại cho
    // tới khi hết câu Pending, sau đó dừng poll (README.md Phase 3 Part 4).
    refetchInterval: (query) => {
      const data = query.state.data;
      if (!data?.writing) return false;
      return writingOverallStatus(data.writing) === "Pending" ? 5000 : false;
    },
  });

  if (isLoading || !result) {
    return <p className="text-sm text-muted">Đang tải kết quả…</p>;
  }
  if (isError) {
    return (
      <p className="rounded-sm border border-danger/30 bg-danger/5 px-3.5 py-2.5 text-sm text-danger">
        Không tải được kết quả. Kiểm tra kết nối backend.
      </p>
    );
  }

  const writingStatus = writingOverallStatus(result.writing);

  return (
    <div className="max-w-2xl">
      <h1 className="font-serif text-2xl font-medium text-ink">Kết quả bài thi</h1>
      <p className="mt-1 text-[13px] text-muted">
        Nộp bài lúc {new Date(result.submittedAt).toLocaleString("vi-VN")}
      </p>

      <p className="mt-4 rounded-sm border border-border bg-paper-raised px-3.5 py-2.5 text-[13px] text-muted">
        Listening/Reading và Writing dùng thang điểm khác nhau nên hiển thị
        riêng biệt, không gộp thành một điểm tổng. Điểm là điểm thô, chưa
        quy đổi thang TOEIC 5–495.
      </p>

      <div className="mt-6 grid grid-cols-2 gap-4">
        <SectionScoreCard
          title="Listening"
          score={result.listeningScore}
          correctCount={result.listeningCorrectCount}
          totalCount={result.listeningTotalCount}
        />
        <SectionScoreCard
          title="Reading"
          score={result.readingScore}
          correctCount={result.readingCorrectCount}
          totalCount={result.readingTotalCount}
        />
      </div>

      <div className="mt-8">
        <div className="flex items-center justify-between">
          <h2 className="font-serif text-lg font-medium text-ink">Writing</h2>
          <WritingStatusBadge status={writingStatus} />
        </div>
        {result.writing.overallScore !== null && (
          <p className="mt-1 font-serif text-3xl font-medium text-ink">
            {result.writing.overallScore}
          </p>
        )}
        <p className="mt-1 text-[12px] text-muted">
          Đã chấm {result.writing.questionsScored}/{result.writing.questionsTotal} câu
        </p>

        <ul className="mt-4 space-y-3">
          {result.writing.questions.map((q, i) => (
            <li key={q.questionId} className="rounded-sm border border-border bg-paper-raised p-4">
              <div className="flex items-center justify-between">
                {/* ⚠️ Response không có `questionNumber` — dùng số thứ tự
                    trong mảng làm nhãn tạm thời (xem type comment). */}
                <p className="text-sm font-medium text-ink">Câu {i + 1}</p>
                <div className="flex items-center gap-2">
                  {q.overallScore !== null && (
                    <span className="font-serif text-lg text-ink">{q.overallScore}</span>
                  )}
                  <WritingStatusBadge status={q.status} />
                </div>
              </div>

              {q.feedback && <p className="mt-2 text-[13px] text-ink-soft">{q.feedback}</p>}

              {q.weaknesses.length > 0 && (
                <ul className="mt-2 list-inside list-disc space-y-0.5 text-[12px] text-muted">
                  {q.weaknesses.map((w, wi) => (
                    <li key={wi}>{w}</li>
                  ))}
                </ul>
              )}

              {q.criterionScores.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-1.5">
                  {q.criterionScores.map((c) => (
                    <span
                      key={c.criterion}
                      className="rounded-full border border-border bg-paper px-2.5 py-0.5 text-[12px] text-ink-soft"
                    >
                      {c.criterion}: {c.score}
                    </span>
                  ))}
                </div>
              )}
            </li>
          ))}
        </ul>
      </div>

      <Link href="/candidate/history" className="mt-8 inline-block text-sm font-medium text-teal hover:underline">
        ← Xem lịch sử các lần thi
      </Link>
    </div>
  );
}
