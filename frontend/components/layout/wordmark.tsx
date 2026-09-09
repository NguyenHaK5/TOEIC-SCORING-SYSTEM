import { cn } from "@/lib/utils";

export function Wordmark({ className, tone = "ink" }: { className?: string; tone?: "ink" | "paper" }) {
  return (
    <div className={cn("flex items-baseline gap-2 font-serif", className)}>
      <span
        className={cn(
          "text-xl font-semibold tracking-tight",
          tone === "ink" ? "text-ink" : "text-paper-raised"
        )}
      >
        TOEIC Scoring
      </span>
      <span
        className={cn(
          "text-sm",
          tone === "ink" ? "text-muted" : "text-paper-raised/60"
        )}
      >
        Support System
      </span>
    </div>
  );
}
