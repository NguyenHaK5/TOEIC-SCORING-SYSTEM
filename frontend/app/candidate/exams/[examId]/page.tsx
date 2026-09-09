"use client";

import { useParams, useRouter } from "next/navigation";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { candidateExamsApi } from "@/lib/candidate/candidate-exams-api";
import { examSessionApi } from "@/lib/candidate/exam-session-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/admin/status-badge";

export default function CandidateExamDetailPage() {
  const { examId } = useParams<{ examId: string }>();
  const router = useRouter();

  // Không có endpoint chi tiết riêng cho Candidate (xem PHASE7-ISSUE-10) —
  // tái dùng cache của danh sách, tự refetch nếu cache trống (vd vào thẳng
  // bằng link, chưa từng ghé /candidate/exams).
  const { data: exams, isLoading } = useQuery({
    queryKey: ["candidate", "exams", "available"],
    queryFn: () => candidateExamsApi.listAvailable(),
  });
  const exam = exams?.find((e) => e.examId === examId);

  const startMutation = useMutation({
    mutationFn: () => examSessionApi.start({ examId }),
    onSuccess: (session) => {
      router.push(`/exam-room/${session.examSessionId}`);
    },
    onError: (err) => toast.error(errorMessage(err, "Không bắt đầu được phiên thi.")),
  });

  if (isLoading) return <p className="text-sm text-muted">Đang tải…</p>;

  if (!exam) {
    return (
      <div className="max-w-xl">
        <p className="text-sm text-muted">Không tìm thấy kỳ thi này hoặc kỳ thi chưa khả dụng.</p>
        <Button variant="outline" size="sm" className="mt-4" onClick={() => router.push("/candidate/exams")}>
          ← Về danh sách kỳ thi
        </Button>
      </div>
    );
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-muted">{exam.examCode}</p>
          <h1 className="mt-0.5 font-serif text-2xl font-medium text-ink">{exam.examName}</h1>
        </div>
        <StatusBadge status={exam.status} />
      </div>

      {exam.description && <p className="mt-4 text-sm text-ink-soft">{exam.description}</p>}

      <div className="mt-6 grid grid-cols-3 gap-4 text-sm">
        <div>
          <p className="text-muted">Ngày thi</p>
          <p className="mt-1 text-ink-soft">{exam.examDate}</p>
        </div>
        <div>
          <p className="text-muted">Bắt đầu</p>
          <p className="mt-1 text-ink-soft">{formatTime(exam.startTime)}</p>
        </div>
        <div>
          <p className="text-muted">Kết thúc</p>
          <p className="mt-1 text-ink-soft">{formatTime(exam.endTime)}</p>
        </div>
      </div>

      <div className="mt-8 rounded-sm border border-gold/30 bg-gold/5 p-4 text-[13px] text-gold">
        Sau khi bắt đầu, đồng hồ đếm ngược chạy liên tục cho tới khi nộp bài
        hoặc hết giờ — không thể tạm dừng. Đóng/refresh trình duyệt không
        làm mất bài đã lưu, nhưng không dừng được đồng hồ.
      </div>

      <Button
        type="button"
        variant="teal"
        size="lg"
        className="mt-6"
        disabled={startMutation.isPending}
        onClick={() => startMutation.mutate()}
      >
        {startMutation.isPending ? "Đang chuẩn bị…" : "Bắt đầu / Tiếp tục làm bài"}
      </Button>
    </div>
  );
}

function formatTime(iso: string) {
  try {
    return new Date(iso).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
  } catch {
    return iso;
  }
}
