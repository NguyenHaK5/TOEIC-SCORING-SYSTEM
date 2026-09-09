"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { examSessionApi } from "@/lib/candidate/exam-session-api";
import { useSaveAnswer } from "@/lib/candidate/use-save-answer";
import { useCountdown } from "@/lib/candidate/use-countdown";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { ConfirmButton } from "@/components/admin/confirm-button";
import { Wordmark } from "@/components/layout/wordmark";
import { TimerDisplay } from "@/components/candidate/timer-display";
import { QuestionPalette } from "@/components/candidate/question-palette";
import { SectionPanel } from "@/components/candidate/section-panel";
import { cn } from "@/lib/utils";
import type { QuestionSection } from "@/types/question";

const SECTIONS: { value: QuestionSection; label: string }[] = [
  { value: "LISTENING", label: "Nghe" },
  { value: "READING", label: "Đọc" },
  { value: "WRITING", label: "Viết" },
];

export default function ExamRoomPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [activeSection, setActiveSection] = useState<QuestionSection>("LISTENING");

  const sessionKey = ["candidate", "exam-sessions", sessionId];
  const { data: session, isLoading: sessionLoading } = useQuery({
    queryKey: sessionKey,
    queryFn: () => examSessionApi.get(sessionId),
    // Đồng bộ lại định kỳ để phát hiện trường hợp phiên đã bị backend tự
    // auto-submit khi hết giờ trong lúc tab không hoạt động (lazy
    // auto-submit — README.md Phase 1 Part 4).
    refetchInterval: 20_000,
  });

  const isInProgress = session?.status === "IN_PROGRESS";

  const { data: questions, isLoading: questionsLoading } = useQuery({
    queryKey: [...sessionKey, "questions"],
    queryFn: () => examSessionApi.getQuestions(sessionId),
    enabled: isInProgress,
  });

  const saveMutation = useSaveAnswer(sessionId);

  const submitMutation = useMutation({
    mutationFn: () => examSessionApi.submit(sessionId),
    onSuccess: (updated) => {
      queryClient.setQueryData(sessionKey, updated);
      toast.success("Đã nộp bài.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không nộp được bài. Thử lại.")),
  });

  const { formatted, isLow } = useCountdown(
    isInProgress ? session?.expiresAt : undefined,
    () => {
      if (isInProgress) submitMutation.mutate();
    }
  );

  // Cảnh báo trước khi rời/đóng tab trong lúc đang làm bài — không chặn
  // được hoàn toàn (trình duyệt giới hạn), nhưng giảm rủi ro rời nhầm.
  useEffect(() => {
    if (!isInProgress) return;
    function handler(e: BeforeUnloadEvent) {
      e.preventDefault();
    }
    window.addEventListener("beforeunload", handler);
    return () => window.removeEventListener("beforeunload", handler);
  }, [isInProgress]);

  if (sessionLoading || !session) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-paper">
        <p className="text-sm text-muted">Đang tải phiên thi…</p>
      </div>
    );
  }

  if (!isInProgress) {
    return (
      <SessionEndedNotice
        status={session.status}
        sessionId={sessionId}
        onBack={() => router.push("/candidate/exams")}
      />
    );
  }

  const sectionQuestions = (questions ?? []).filter((q) => q.section === activeSection);

  return (
    <div className="min-h-screen bg-paper">
      <header className="sticky top-0 z-40 flex items-center justify-between border-b border-border bg-paper-raised px-6 py-3">
        <Wordmark className="text-base" />
        <div className="flex items-center gap-4">
          <TimerDisplay formatted={formatted} isLow={isLow} />
          <ConfirmButton
            label="Nộp bài"
            title="Nộp bài thi?"
            description="Sau khi nộp, bạn không thể chỉnh sửa câu trả lời. Hành động này không thể hoàn tác."
            confirmLabel="Nộp bài"
            variant="teal"
            onConfirm={() => submitMutation.mutateAsync()}
          />
        </div>
      </header>

      <div className="mx-auto flex max-w-6xl gap-8 px-6 py-8">
        <div className="min-w-0 flex-1">
          <div className="flex gap-1 border-b border-border">
            {SECTIONS.map((s) => (
              <button
                key={s.value}
                type="button"
                onClick={() => setActiveSection(s.value)}
                className={cn(
                  "px-4 py-2.5 text-sm font-medium transition-colors",
                  activeSection === s.value
                    ? "border-b-2 border-teal text-teal-dark"
                    : "text-muted hover:text-ink"
                )}
              >
                {s.label}
              </button>
            ))}
          </div>

          <div className="mt-6">
            {questionsLoading ? (
              <p className="text-sm text-muted">Đang tải câu hỏi…</p>
            ) : sectionQuestions.length === 0 ? (
              <p className="text-sm text-muted">Phần này không có câu hỏi.</p>
            ) : (
              <SectionPanel questions={sectionQuestions} saveMutation={saveMutation} disabled={false} />
            )}
          </div>
        </div>

        <aside className="hidden w-56 shrink-0 lg:block">
          <div className="sticky top-20 rounded-sm border border-border bg-paper-raised p-4">
            <p className="text-[13px] font-medium text-ink-soft">
              {SECTIONS.find((s) => s.value === activeSection)?.label}
            </p>
            {sectionQuestions.length > 0 && (
              <div className="mt-3">
                <QuestionPalette
                  questions={sectionQuestions}
                  activeQuestionId={null}
                  onJump={(id) =>
                    document
                      .getElementById(`question-${id}`)
                      ?.scrollIntoView({ behavior: "smooth", block: "center" })
                  }
                />
              </div>
            )}
          </div>
        </aside>
      </div>
    </div>
  );
}

function SessionEndedNotice({
  status,
  sessionId,
  onBack,
}: {
  status: string;
  sessionId: string;
  onBack: () => void;
}) {
  const message =
    status === "SUBMITTED"
      ? "Bạn đã nộp bài. Kết quả sẽ hiển thị sau khi hệ thống chấm xong."
      : status === "ABSENT"
        ? "Phiên thi này được ghi nhận là vắng thi."
        : "Phiên thi này đã kết thúc.";

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-6 bg-paper px-6 text-center">
      <Wordmark />
      <div>
        <h1 className="font-serif text-2xl font-medium text-ink">Phiên thi đã kết thúc</h1>
        <p className="mt-2 max-w-sm text-sm text-muted">{message}</p>
      </div>
      <div className="flex gap-3">
        {status === "SUBMITTED" && (
          <Button variant="teal" asChild>
            <Link href={`/candidate/results/${sessionId}`}>Xem kết quả</Link>
          </Button>
        )}
        <Button variant="outline" onClick={onBack}>
          Về danh sách kỳ thi
        </Button>
      </div>
    </div>
  );
}
