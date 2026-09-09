import { cn } from "@/lib/utils";
import type { WritingScoreStatus } from "@/types/candidate-result";

const LABEL: Record<WritingScoreStatus, string> = {
  Pending: "Đang chấm",
  Scored: "Đã chấm",
  NotAnswered: "Chưa trả lời",
};

const CLASS: Record<WritingScoreStatus, string> = {
  Pending: "bg-gold/10 text-gold border-gold/30",
  Scored: "bg-success/10 text-success border-success/30",
  NotAnswered: "bg-muted/10 text-muted border-border",
};

export function WritingStatusBadge({ status }: { status: WritingScoreStatus }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-[12px] font-medium",
        CLASS[status]
      )}
    >
      {status === "Pending" && (
        <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-current" />
      )}
      {LABEL[status]}
    </span>
  );
}
