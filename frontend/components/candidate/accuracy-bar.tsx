export function AccuracyBar({
  label,
  correct,
  total,
}: {
  label: string;
  correct: number;
  total: number;
}) {
  const pct = total > 0 ? Math.round((correct / total) * 100) : 0;
  const tone = pct >= 70 ? "bg-success" : pct >= 40 ? "bg-gold" : "bg-danger";

  return (
    <div>
      <div className="flex items-center justify-between text-[13px]">
        <span className="text-ink-soft">{label}</span>
        <span className="text-muted">
          {correct}/{total} ({pct}%)
        </span>
      </div>
      <div className="mt-1.5 h-2 overflow-hidden rounded-full bg-border/60">
        <div className={`h-full ${tone}`} style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}
