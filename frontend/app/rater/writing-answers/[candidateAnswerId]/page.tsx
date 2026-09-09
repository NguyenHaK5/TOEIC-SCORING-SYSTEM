"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { raterApi } from "@/lib/rater/rater-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { QUESTION_TYPE_LABEL } from "@/types/question";
import type { WritingAnswerDetail } from "@/types/rater";

export default function WritingAnswerDetailPage() {
  const { candidateAnswerId } = useParams<{ candidateAnswerId: string }>();
  const queryClient = useQueryClient();

  const { data: detail, isLoading, isError } = useQuery({
    queryKey: ["rater", "writing-answers", candidateAnswerId],
    queryFn: () => raterApi.get(candidateAnswerId),
  });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ["rater", "writing-answers", candidateAnswerId] });
    queryClient.invalidateQueries({ queryKey: ["rater", "writing-answers"] });
  }

  if (isLoading || !detail) return <p className="text-sm text-muted">Đang tải…</p>;
  if (isError) {
    return (
      <p className="rounded-sm border border-danger/30 bg-danger/5 px-3.5 py-2.5 text-sm text-danger">
        Không tải được bài làm. Kiểm tra kết nối backend.
      </p>
    );
  }

  return (
    <WritingAnswerReview
      key={detail.candidateAnswerId}
      detail={detail}
      candidateAnswerId={candidateAnswerId}
      onScored={invalidate}
    />
  );
}

function WritingAnswerReview({
  detail,
  candidateAnswerId,
  onScored,
}: {
  detail: WritingAnswerDetail;
  candidateAnswerId: string;
  onScored: () => void;
}) {
  const router = useRouter();

  return (
    <div className="max-w-3xl">
      <button
        type="button"
        onClick={() => router.push("/rater/writing-answers")}
        className="text-sm text-muted hover:underline"
      >
        ← Về danh sách
      </button>

      <div className="mt-3 flex items-start justify-between">
        <div>
          <p className="text-sm text-muted">{detail.candidateName}</p>
          <h1 className="mt-0.5 font-serif text-2xl font-medium text-ink">
            {QUESTION_TYPE_LABEL[detail.questionType]}
          </h1>
        </div>
      </div>

      <div className="mt-6 rounded-sm border border-border bg-paper-raised p-5">
        <p className="text-sm text-ink-soft">{detail.questionText}</p>

        {detail.imageUrl && (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={detail.imageUrl}
            alt="Tranh minh hoạ câu hỏi"
            className="mt-3 max-h-64 rounded-sm border border-border object-contain"
          />
        )}

        {detail.requiredWords && (
          <div className="mt-3 flex flex-wrap gap-1.5">
            {detail.requiredWords.split(",").map((w) => (
              <span
                key={w}
                className="rounded-full border border-gold/30 bg-gold/5 px-2.5 py-0.5 text-[12px] text-gold"
              >
                {w.trim()}
              </span>
            ))}
          </div>
        )}

        <div className="mt-4 whitespace-pre-wrap rounded-sm border border-border bg-paper p-4 text-sm text-ink">
          {detail.answerText}
        </div>
      </div>

      <AiAssessmentCard detail={detail} />

      {/* ✅ Tên field đã xác nhận: `humanAssessments` (không phải
          `humanHistory` như giả định ban đầu — đây chính là field gây
          crash `.length` trước đó). */}
      {(detail.humanAssessments ?? []).length > 0 && (
        <HumanHistoryList history={detail.humanAssessments} />
      )}

      {detail.activeRubric ? (
        <ScoringForm
          candidateAnswerId={candidateAnswerId}
          activeRubric={detail.activeRubric}
          onScored={onScored}
        />
      ) : (
        <p className="mt-8 rounded-sm border border-gold/30 bg-gold/5 px-3.5 py-2.5 text-sm text-gold">
          Không có Rubric đang Active cho dạng câu này — không thể chấm điểm.
        </p>
      )}
    </div>
  );
}

function AiAssessmentCard({ detail }: { detail: WritingAnswerDetail }) {
  const ai = detail.aiAssessment;
  return (
    <div className="mt-6">
      <div className="flex items-center justify-between">
        <h2 className="font-serif text-lg font-medium text-ink">Đánh giá AI</h2>
        {ai && <span className="font-serif text-2xl font-medium text-ink">{ai.overallScore}</span>}
      </div>
      {!ai ? (
        <p className="mt-2 text-sm text-muted">AI chưa chấm xong bài này.</p>
      ) : (
        <div className="mt-3 rounded-sm border border-border bg-paper-raised p-5">
          {ai.modelName && <p className="text-[12px] text-muted">Model: {ai.modelName}</p>}
          {ai.feedback && <p className="mt-2 text-sm text-ink-soft">{ai.feedback}</p>}
          {(ai.weaknesses ?? []).length > 0 && (
            <ul className="mt-2 list-inside list-disc space-y-0.5 text-[12px] text-muted">
              {(ai.weaknesses ?? []).map((w, i) => (
                <li key={i}>{w}</li>
              ))}
            </ul>
          )}
          {(ai.criterionScores ?? []).length > 0 && (
            <div className="mt-3 flex flex-wrap gap-1.5">
              {(ai.criterionScores ?? []).map((c) => (
                <span
                  key={c.criterion}
                  className="rounded-full border border-border bg-paper px-2.5 py-0.5 text-[12px] text-ink-soft"
                  title={c.reasoning ?? undefined}
                >
                  {c.criterion}: {c.score}
                </span>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function HumanHistoryList({ history }: { history: WritingAnswerDetail["humanAssessments"] }) {
  return (
    <div className="mt-6">
      <h2 className="font-serif text-lg font-medium text-ink">Lịch sử chấm thủ công</h2>
      <ul className="mt-3 space-y-2">
        {history
          .slice()
          .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
          .map((h) => (
            <li key={h.humanAssessmentId} className="rounded-sm border border-border bg-paper-raised p-4">
              <div className="flex items-center justify-between">
                <p className="text-sm font-medium text-ink">
                  {h.raterName}
                  {h.isFinal && (
                    <span className="ml-2 rounded-full border border-success/30 bg-success/10 px-2 py-0.5 text-[11px] text-success">
                      Chính thức
                    </span>
                  )}
                </p>
                <span className="font-serif text-lg text-ink">{h.overallScore}</span>
              </div>
              <p className="mt-1 text-[12px] text-muted">
                {new Date(h.createdAt).toLocaleString("vi-VN")}
              </p>
              {h.feedback && <p className="mt-2 text-[13px] text-ink-soft">{h.feedback}</p>}
            </li>
          ))}
      </ul>
    </div>
  );
}

function ScoringForm({
  candidateAnswerId,
  activeRubric,
  onScored,
}: {
  candidateAnswerId: string;
  activeRubric: NonNullable<WritingAnswerDetail["activeRubric"]>;
  onScored: () => void;
}) {
  // ⚠️ Fallback phòng khi `activeRubric.criteria` thiếu trong response thật
  // (cùng loại rủi ro PHASE7-ISSUE-15) — tránh crash toàn trang chấm điểm.
  const criteria = activeRubric.criteria ?? [];

  // Khởi tạo trực tiếp từ activeRubric — không qua effect. Mỗi tiêu chí bắt
  // đầu ở điểm giữa khoảng [minScore, maxScore] làm gợi ý trung tính.
  const [scores, setScores] = useState<Record<string, number>>(() =>
    Object.fromEntries(
      criteria.map((c) => [c.criterion, Math.round((c.minScore + c.maxScore) / 2)])
    )
  );
  const [feedback, setFeedback] = useState("");

  const submitMutation = useMutation({
    mutationFn: () =>
      raterApi.submitScore(candidateAnswerId, {
        criterionScores: criteria.map((c) => ({
          criterion: c.criterion,
          score: scores[c.criterion],
        })),
        feedback: feedback || undefined,
      }),
    onSuccess: () => {
      onScored();
      toast.success("Đã lưu điểm chấm.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không lưu được điểm chấm.")),
  });

  return (
    <div className="mt-8 border-t border-border pt-6">
      <h2 className="font-serif text-lg font-medium text-ink">Chấm điểm — {activeRubric.name}</h2>

      <div className="mt-4 space-y-4">
        {criteria.map((c) => (
          <div key={c.criterion} className="rounded-sm border border-border bg-paper-raised p-4">
            <div className="flex items-center justify-between">
              <Label htmlFor={`criterion-${c.criterion}`}>{c.criterion}</Label>
              <span className="text-[12px] text-muted">
                {c.minScore}–{c.maxScore} · trọng số {c.weight}
              </span>
            </div>
            {c.description && <p className="mt-1 text-[12px] text-muted">{c.description}</p>}
            <Input
              id={`criterion-${c.criterion}`}
              type="number"
              min={c.minScore}
              max={c.maxScore}
              value={scores[c.criterion]}
              onChange={(e) =>
                setScores((s) => ({ ...s, [c.criterion]: Number(e.target.value) }))
              }
              className="mt-2 w-32"
            />
          </div>
        ))}
      </div>

      <div className="mt-4 space-y-2">
        <Label htmlFor="feedback">Nhận xét</Label>
        <Textarea id="feedback" value={feedback} onChange={(e) => setFeedback(e.target.value)} />
      </div>

      <Button
        type="button"
        variant="teal"
        className="mt-4"
        disabled={submitMutation.isPending}
        onClick={() => submitMutation.mutate()}
      >
        {submitMutation.isPending ? "Đang lưu…" : "Lưu điểm chấm"}
      </Button>
    </div>
  );
}
