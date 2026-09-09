/**
 * ✅ Đã sửa theo JSON thật: response `GET /exam-sessions/{id}/result` chỉ
 * có `{section}Score`/`{section}CorrectCount`/`{section}TotalCount` —
 * KHÔNG tách riêng "sai" và "bỏ trống", nên chỉ hiển thị "chưa đúng"
 * (= total - correct) thay vì cố tách 2 khái niệm không có dữ liệu.
 */
export function SectionScoreCard({
  title,
  score,
  correctCount,
  totalCount,
}: {
  title: string;
  score: number;
  correctCount: number;
  totalCount: number;
}) {
  const incorrect = Math.max(0, totalCount - correctCount);
  return (
    <div className="rounded-sm border border-border bg-paper-raised p-5">
      <p className="text-[13px] text-muted">{title}</p>
      <p className="mt-1 font-serif text-3xl font-medium text-ink">{score}</p>
      <p className="mt-2 text-[13px] text-ink-soft">
        Đúng {correctCount}/{totalCount}
        {incorrect > 0 && <span className="text-muted"> · {incorrect} chưa đúng</span>}
      </p>
    </div>
  );
}
