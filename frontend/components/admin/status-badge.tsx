import { cn } from "@/lib/utils";

type Tone = "neutral" | "teal" | "success" | "warning" | "danger";

const STATUS_TONE: Record<string, Tone> = {
  DRAFT: "neutral",
  SCHEDULED: "teal",
  ONGOING: "warning",
  COMPLETED: "success",
  CANCELLED: "danger",
  PUBLISHED: "success",
  ARCHIVED: "neutral",
  ACTIVE: "success",
};

const TONE_CLASSES: Record<Tone, string> = {
  neutral: "bg-muted/10 text-muted border-border",
  teal: "bg-teal/10 text-teal-dark border-teal/30",
  success: "bg-success/10 text-success border-success/30",
  warning: "bg-gold/10 text-gold border-gold/30",
  danger: "bg-danger/10 text-danger border-danger/30",
};

const STATUS_LABEL: Record<string, string> = {
  DRAFT: "Nháp",
  SCHEDULED: "Đã lên lịch",
  ONGOING: "Đang diễn ra",
  COMPLETED: "Đã hoàn thành",
  CANCELLED: "Đã huỷ",
  PUBLISHED: "Đã publish",
  ARCHIVED: "Đã lưu trữ",
  ACTIVE: "Đang áp dụng",
};

export function StatusBadge({ status }: { status: string }) {
  const tone = STATUS_TONE[status] ?? "neutral";
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border px-2.5 py-0.5 text-[12px] font-medium",
        TONE_CLASSES[tone]
      )}
    >
      {STATUS_LABEL[status] ?? status}
    </span>
  );
}
