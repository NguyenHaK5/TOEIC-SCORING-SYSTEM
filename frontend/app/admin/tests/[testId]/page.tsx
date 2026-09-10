"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { testsApi } from "@/lib/admin/tests-api";
import { questionsApi } from "@/lib/admin/questions-api";
import { errorMessage } from "@/lib/admin/error";
import { ApiError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { StatusBadge } from "@/components/admin/status-badge";
import { ConfirmButton } from "@/components/admin/confirm-button";
import { SECTION_LABEL, QUESTION_TYPE_LABEL } from "@/types/question";
import type { PublishValidationError, Test } from "@/types/test";

export default function TestDetailPage() {
  const { testId } = useParams<{ testId: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: test, isLoading } = useQuery({
    queryKey: ["admin", "tests", testId],
    queryFn: () => testsApi.get(testId),
  });

  const { data: questions } = useQuery({
    queryKey: ["admin", "tests", testId, "questions"],
    queryFn: () => questionsApi.listByTest(testId),
  });

  const [publishErrors, setPublishErrors] = useState<PublishValidationError[]>([]);

  function invalidateList() {
    queryClient.invalidateQueries({ queryKey: ["admin", "tests"] });
  }


  const publishMutation = useMutation({
    mutationFn: () => testsApi.publish(testId),
    onSuccess: (result) => {
      if (result && result.success === false && result.errors?.length) {
        setPublishErrors(result.errors);
        toast.error(`Còn ${result.errors.length} lỗi cần sửa trước khi publish.`);
        return;
      }
      setPublishErrors([]);
      queryClient.invalidateQueries({ queryKey: ["admin", "tests", testId] });
      invalidateList();
      toast.success("Đã publish bộ đề.");
    },
    onError: (err) => {
      // GIẢ ĐỊNH: backend có thể trả 400 kèm { errors: [...] } thay vì 200 success:false.
      if (err instanceof ApiError && Array.isArray((err.detail as { errors?: unknown })?.errors)) {
        setPublishErrors((err.detail as { errors: PublishValidationError[] }).errors);
        toast.error("Còn lỗi cần sửa trước khi publish.");
        return;
      }
      toast.error(errorMessage(err, "Không publish được bộ đề."));
    },
  });

  const archiveMutation = useMutation({
    mutationFn: () => testsApi.archive(testId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "tests", testId] });
      invalidateList();
      toast.success("Đã lưu trữ bộ đề.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không lưu trữ được bộ đề.")),
  });

  const deleteQuestionMutation = useMutation({
    mutationFn: (questionId: string) => questionsApi.remove(questionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "tests", testId, "questions"] });
      toast.success("Đã xoá câu hỏi.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không xoá được câu hỏi.")),
  });

  if (isLoading || !test) {
    return <p className="text-sm text-muted">Đang tải…</p>;
  }

  const isDraft = test.status === "DRAFT";
  const isPublished = test.status === "PUBLISHED";

  return (
    <div className="max-w-3xl">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-muted">
            {test.testCode} · v{test.version}
          </p>
          <h1 className="mt-0.5 font-serif text-2xl font-medium text-ink">{test.testName}</h1>
        </div>
        <StatusBadge status={test.status} />
      </div>

      <div className="mt-6 flex flex-wrap gap-2 border-b border-border pb-6">
        {isDraft && (
          <Button
            type="button"
            variant="teal"
            size="sm"
            disabled={publishMutation.isPending}
            onClick={() => publishMutation.mutate()}
          >
            {publishMutation.isPending ? "Đang publish…" : "Publish"}
          </Button>
        )}
        {isPublished && (
          <ConfirmButton
            label="Lưu trữ (Archive)"
            title="Lưu trữ bộ đề này?"
            description="Bộ đề đã lưu trữ không thể chỉnh sửa hay gán mới cho kỳ thi."
            onConfirm={() => archiveMutation.mutateAsync()}
          />
        )}
      </div>

      {publishErrors.length > 0 && (
        <div className="mt-6 rounded-sm border border-danger/30 bg-danger/5 p-4">
          <p className="text-sm font-medium text-danger">
            {publishErrors.length} lỗi cần sửa trước khi publish:
          </p>
          <ul className="mt-2 list-inside list-disc space-y-1 text-sm text-danger">
            {publishErrors.map((e, i) => (
              <li key={i}>
                {e.questionNumber ? `Câu ${e.questionNumber}: ` : ""}
                {e.message}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Basic info */}
      <TestBasicInfoForm key={test.testId} test={test} isDraft={isDraft} onSaved={invalidateList} />

      {/* Questions */}
      <div className="mt-10 border-t border-border pt-6">
        <div className="flex items-center justify-between">
          <h2 className="font-serif text-lg font-medium text-ink">
            Câu hỏi ({questions?.length ?? 0})
          </h2>
          {isDraft && (
            <div className="flex items-center gap-2">
              <Button asChild variant="outline" size="sm">
                <Link href={`/admin/tests/${testId}/questions/import`}>Nhập từ Excel</Link>
              </Button>
              <Button asChild variant="default" size="sm">
                <Link href={`/admin/tests/${testId}/questions/new`}>Thêm câu hỏi</Link>
              </Button>
            </div>
          )}
        </div>

        {!questions || questions.length === 0 ? (
          <p className="mt-4 text-sm text-muted">Chưa có câu hỏi nào.</p>
        ) : (
          <ul className="mt-4 divide-y divide-border rounded-sm border border-border bg-paper-raised">
            {questions
              .slice()
              .sort((a, b) => a.questionNumber - b.questionNumber)
              .map((q) => (
                <li key={q.questionId} className="flex items-center justify-between px-4 py-3">
                  <div>
                    <p className="text-sm font-medium text-ink">
                      Câu {q.questionNumber} — {SECTION_LABEL[q.section]}
                    </p>
                    <p className="mt-0.5 line-clamp-1 text-sm text-muted">
                      {QUESTION_TYPE_LABEL[q.questionType]} · {q.questionText}
                    </p>
                  </div>
                  <div className="flex items-center gap-3">
                    <Link
                      href={`/admin/tests/${testId}/questions/${q.questionId}`}
                      className="text-sm font-medium text-teal hover:underline"
                    >
                      {isDraft ? "Sửa" : "Xem"}
                    </Link>
                    {isDraft && (
                      <ConfirmButton
                        label="Xoá"
                        title={`Xoá câu ${q.questionNumber}?`}
                        onConfirm={() => deleteQuestionMutation.mutateAsync(q.questionId)}
                      />
                    )}
                  </div>
                </li>
              ))}
          </ul>
        )}
      </div>

      <div className="mt-8">
        <Button type="button" variant="ghost" size="sm" onClick={() => router.push("/admin/tests")}>
          ← Về danh sách bộ đề
        </Button>
      </div>
    </div>
  );
}

function TestBasicInfoForm({
  test,
  isDraft,
  onSaved,
}: {
  test: Test;
  isDraft: boolean;
  onSaved: () => void;
}) {
  const [testName, setTestName] = useState(test.testName);
  const [description, setDescription] = useState(test.description ?? "");

  const updateMutation = useMutation({
    mutationFn: () =>
      testsApi.update(test.testId, {
        testCode: test.testCode,
        testName,
        version: test.version,
        description: description || undefined,
        listeningDurationSeconds: test.listeningDurationSeconds,
        readingDurationSeconds: test.readingDurationSeconds,
        writingDurationSeconds: test.writingDurationSeconds,
      }),
    onSuccess: () => {
      onSaved();
      toast.success("Đã lưu thay đổi.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không lưu được thay đổi.")),
  });

  return (
    <div className="mt-6 space-y-5">
      <div className="space-y-2">
        <Label htmlFor="testName">Tên bộ đề</Label>
        <Input
          id="testName"
          value={testName}
          disabled={!isDraft}
          onChange={(e) => setTestName(e.target.value)}
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
          <p className="text-muted">Listening</p>
          <p className="mt-1 text-ink-soft">{test.listeningDurationSeconds}s</p>
        </div>
        <div>
          <p className="text-muted">Reading</p>
          <p className="mt-1 text-ink-soft">{test.readingDurationSeconds}s</p>
        </div>
        <div>
          <p className="text-muted">Writing</p>
          <p className="mt-1 text-ink-soft">{test.writingDurationSeconds}s</p>
        </div>
      </div>
      {isDraft && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={updateMutation.isPending}
          onClick={() => updateMutation.mutate()}
        >
          {updateMutation.isPending ? "Đang lưu…" : "Lưu thay đổi"}
        </Button>
      )}
    </div>
  );
}
