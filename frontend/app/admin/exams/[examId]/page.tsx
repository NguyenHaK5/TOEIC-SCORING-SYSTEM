"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { examsApi } from "@/lib/admin/exams-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { StatusBadge } from "@/components/admin/status-badge";
import { ConfirmButton } from "@/components/admin/confirm-button";
import { AssignTestDialog } from "./assign-test-dialog";
import { EXAM_STATUS_TRANSITIONS, type Exam, type ExamStatus } from "@/types/exam";

const TRANSITION_LABEL: Record<ExamStatus, string> = {
  DRAFT: "Đưa về Nháp",
  SCHEDULED: "Lên lịch",
  ONGOING: "Bắt đầu kỳ thi",
  COMPLETED: "Kết thúc kỳ thi",
  CANCELLED: "Huỷ kỳ thi",
};

export default function ExamDetailPage() {
  const { examId } = useParams<{ examId: string }>();
  const queryClient = useQueryClient();

  const { data: exam, isLoading } = useQuery({
    queryKey: ["admin", "exams", examId],
    queryFn: () => examsApi.get(examId),
  });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ["admin", "exams"] });
  }

  const statusMutation = useMutation({
    mutationFn: (status: ExamStatus) => examsApi.changeStatus(examId, status),
    onSuccess: (_data, status) => {
      invalidate();
      toast.success(`Đã chuyển trạng thái sang "${TRANSITION_LABEL[status]}".`);
    },
    onError: (err) => toast.error(errorMessage(err, "Không chuyển được trạng thái.")),
  });

  const assignMutation = useMutation({
    mutationFn: (testId: string) => examsApi.assignTest(examId, testId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "exams", examId] });
      toast.success("Đã gán bộ đề.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không gán được bộ đề.")),
  });

  const unassignMutation = useMutation({
    mutationFn: (testId: string) => examsApi.unassignTest(examId, testId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "exams", examId] });
      toast.success("Đã gỡ bộ đề (vẫn giữ lịch sử).");
    },
    onError: (err) => toast.error(errorMessage(err, "Không gỡ được bộ đề.")),
  });

  if (isLoading || !exam) {
    return <p className="text-sm text-muted">Đang tải…</p>;
  }

  const isDraft = exam.status === "DRAFT";
  const nextStatuses = EXAM_STATUS_TRANSITIONS[exam.status];
  const assignedTests = exam.assignedTests ?? [];

  return (
    <div className="max-w-2xl">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-muted">{exam.examCode}</p>
          <h1 className="mt-0.5 font-serif text-2xl font-medium text-ink">{exam.examName}</h1>
        </div>
        <StatusBadge status={exam.status} />
      </div>

      {/* State machine */}
      {nextStatuses.length > 0 && (
        <div className="mt-6 flex flex-wrap gap-2 border-b border-border pb-6">
          {nextStatuses.map((target) => (
            <ConfirmButton
              key={target}
              label={TRANSITION_LABEL[target]}
              title={`${TRANSITION_LABEL[target]}?`}
              description={
                target === "SCHEDULED"
                  ? "Yêu cầu kỳ thi đã có ít nhất 1 bộ đề đang active."
                  : target === "CANCELLED"
                    ? "Hành động này không thể hoàn tác."
                    : undefined
              }
              variant={target === "CANCELLED" ? "outline" : "teal"}
              onConfirm={() => statusMutation.mutateAsync(target)}
            />
          ))}
        </div>
      )}

      {/* Basic info — key={exam.examId} đảm bảo form state khởi tạo lại đúng khi id đổi */}
      <ExamBasicInfoForm key={exam.examId} exam={exam} isDraft={isDraft} onSaved={invalidate} />

      {/* Assigned tests */}
      <div className="mt-10 border-t border-border pt-6">
        <div className="flex items-center justify-between">
          <h2 className="font-serif text-lg font-medium text-ink">Bộ đề đã gán</h2>
          <AssignTestDialog
            excludeTestIds={assignedTests.map((t) => t.testId)}
            onAssign={(testId) => assignMutation.mutateAsync(testId)}
          />
        </div>

        {assignedTests.length === 0 ? (
          <p className="mt-4 text-sm text-muted">Chưa gán bộ đề nào.</p>
        ) : (
          <ul className="mt-4 divide-y divide-border rounded-sm border border-border bg-paper-raised">
            {assignedTests.map((t) => (
              <li key={t.testId} className="flex items-center justify-between px-4 py-3">
                <div>
                  <p className="text-sm font-medium text-ink">{t.testCode}</p>
                  <p className="text-sm text-muted">{t.testName}</p>
                </div>
                <div className="flex items-center gap-3">
                  <span
                    className={
                      t.isActive
                        ? "text-[12px] font-medium text-success"
                        : "text-[12px] text-muted"
                    }
                  >
                    {t.isActive ? "Đang active" : "Đã gỡ"}
                  </span>
                  {t.isActive && (
                    <ConfirmButton
                      label="Gỡ"
                      title={`Gỡ bộ đề ${t.testCode}?`}
                      description="Vẫn giữ lại lịch sử gán, chỉ tắt trạng thái active."
                      onConfirm={() => unassignMutation.mutateAsync(t.testId)}
                    />
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function ExamBasicInfoForm({
  exam,
  isDraft,
  onSaved,
}: {
  exam: Exam;
  isDraft: boolean;
  onSaved: () => void;
}) {
  const router = useRouter();
  // Khởi tạo trực tiếp từ prop (component chỉ mount sau khi `exam` đã có
  // dữ liệu thật, và `key={exam.examId}` ở nơi gọi đảm bảo remount đúng lúc) —
  // không cần useEffect để đồng bộ, tránh lint rule react-hooks/set-state-in-effect.
  const [examName, setExamName] = useState(exam.examName);
  const [description, setDescription] = useState(exam.description ?? "");

  const updateMutation = useMutation({
    mutationFn: () =>
      examsApi.update(exam.examId, {
        examCode: exam.examCode,
        examName,
        description: description || undefined,
        examDate: exam.examDate,
        startTime: exam.startTime,
        endTime: exam.endTime,
      }),
    onSuccess: () => {
      onSaved();
      toast.success("Đã lưu thay đổi.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không lưu được thay đổi.")),
  });

  const deleteMutation = useMutation({
    mutationFn: () => examsApi.remove(exam.examId),
    onSuccess: () => {
      onSaved();
      toast.success("Đã xoá kỳ thi.");
      router.push("/admin/exams");
    },
    onError: (err) => toast.error(errorMessage(err, "Không xoá được kỳ thi.")),
  });

  return (
    <div className="mt-6 space-y-5">
      <div className="space-y-2">
        <Label htmlFor="examName">Tên kỳ thi</Label>
        <Input
          id="examName"
          value={examName}
          disabled={!isDraft}
          onChange={(e) => setExamName(e.target.value)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="description">Mô tả</Label>
        <Textarea
          id="description"
          value={description}
          disabled={!isDraft}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>
      <div className="grid grid-cols-3 gap-4 text-sm">
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
      {isDraft && (
        <div className="flex gap-3">
          <Button
            type="button"
            variant="teal"
            disabled={updateMutation.isPending}
            onClick={() => updateMutation.mutate()}
          >
            {updateMutation.isPending ? "Đang lưu…" : "Lưu thay đổi"}
          </Button>
          <ConfirmButton
            label="Xoá kỳ thi"
            title="Xoá kỳ thi này?"
            description="Chỉ xoá được khi còn ở trạng thái Nháp. Không thể hoàn tác."
            confirmLabel="Xoá"
            onConfirm={() => deleteMutation.mutateAsync()}
          />
        </div>
      )}
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
