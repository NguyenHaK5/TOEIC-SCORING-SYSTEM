import { cn } from "@/lib/utils";

export function TimerDisplay({ formatted, isLow }: { formatted: string; isLow: boolean }) {
  return (
    <div
      className={cn(
        "flex items-center gap-2 rounded-sm border px-3 py-1.5 font-serif text-base tabular-nums",
        isLow ? "border-danger/40 bg-danger/5 text-danger" : "border-border bg-paper-raised text-ink"
      )}
    >
      <span className="h-2 w-2 rounded-full bg-current" />
      {formatted}
    </div>
  );
}
