"use client";

import { useQuery } from "@tanstack/react-query";
import { analyticsApi } from "@/lib/candidate/analytics-api";
import { AccuracyBar } from "@/components/candidate/accuracy-bar";
import { ScoreTrendChart } from "@/components/candidate/score-trend-chart";
import { SECTION_LABEL } from "@/types/question";

export default function CandidateAnalyticsPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["candidate", "analytics"],
    queryFn: () => analyticsApi.get(20),
  });

  if (isLoading) return <p className="text-sm text-muted">Đang tải…</p>;
  if (isError || !data) {
    return (
      <p className="rounded-sm border border-danger/30 bg-danger/5 px-3.5 py-2.5 text-sm text-danger">
        Không tải được dữ liệu phân tích. Kiểm tra kết nối backend.
      </p>
    );
  }

  // ✅ Field top-level đã xác nhận đúng (objectiveWeakness/writingWeakness)
  // — vẫn giữ fallback `?? []` vì mảng objectiveWeakness trong ví dụ thật
  // đang rỗng, chưa xác nhận shape phần tử bên trong (xem type comment).
  const bySection = groupBySection(data.objectiveWeakness ?? []);
  const writingWeakness = data.writingWeakness ?? [];
  const scoreTrend = data.scoreTrend ?? [];

  return (
    <div className="max-w-3xl space-y-10">
      <div>
        <h1 className="font-serif text-2xl font-medium text-ink">Phân tích năng lực</h1>
        <p className="mt-1 text-sm text-muted">
          Tổng hợp từ toàn bộ phiên thi đã nộp — cập nhật sau mỗi lần có kết quả mới.
        </p>
      </div>

      <div>
        <h2 className="font-serif text-lg font-medium text-ink">Xu hướng điểm</h2>
        <div className="mt-4 rounded-sm border border-border bg-paper-raised p-5">
          <ScoreTrendChart points={scoreTrend} />
        </div>
      </div>

      <div>
        <h2 className="font-serif text-lg font-medium text-ink">Độ chính xác — Listening &amp; Reading</h2>
        {bySection.length === 0 ? (
          <p className="mt-3 text-sm text-muted">Chưa có đủ dữ liệu.</p>
        ) : (
          <div className="mt-4 space-y-5 rounded-sm border border-border bg-paper-raised p-5">
            {bySection.map((s) => (
              <AccuracyBar
                key={`${s.section}-${s.questionType}`}
                label={`${SECTION_LABEL[s.section]} · ${s.questionType}`}
                correct={s.correctCount}
                total={s.totalCount}
              />
            ))}
          </div>
        )}
      </div>

      <div>
        <h2 className="font-serif text-lg font-medium text-ink">Điểm trung bình theo tiêu chí Writing</h2>
        {writingWeakness.length === 0 ? (
          <p className="mt-3 text-sm text-muted">Chưa có bài Writing nào được chấm.</p>
        ) : (
          <div className="mt-4 grid grid-cols-2 gap-3">
            {writingWeakness.map((c) => (
              <div key={c.criterionCode} className="rounded-sm border border-border bg-paper-raised p-4">
                <p className="text-[13px] text-muted">{c.criterionCode}</p>
                <p className="mt-1 font-serif text-2xl font-medium text-ink">
                  {c.averageScore.toFixed(1)}
                </p>
                <p className="mt-0.5 text-[12px] text-muted">{c.sampleSize} bài</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function groupBySection<T extends { section: string; questionType: string }>(items: T[]) {
  return items.slice().sort((a, b) => a.section.localeCompare(b.section));
}
