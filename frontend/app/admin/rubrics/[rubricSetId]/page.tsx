"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { rubricsApi } from "@/lib/admin/rubrics-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { StatusBadge } from "@/components/admin/status-badge";
import { ConfirmButton } from "@/components/admin/confirm-button";
import { QUESTION_TYPE_LABEL } from "@/types/question";
import type { CreateRubricPayload, Rubric, RubricSet } from "@/types/rubric";
import Link from "next/link";

export default function RubricSetDetailPage() {
  const { rubricSetId } = useParams<{ rubricSetId: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: rubricSet, isLoading } = useQuery({
    queryKey: ["admin", "rubric-sets", rubricSetId],
    queryFn: () => rubricsApi.get(rubricSetId),
  });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ["admin", "rubric-sets"] });
    queryClient.invalidateQueries({ queryKey: ["admin", "rubric-sets", rubricSetId] });
  }


  const activateMutation = useMutation({
    mutationFn: () => rubricsApi.activate(rubricSetId),
    onSuccess: () => {
      invalidate();
      toast.success("Đã kích hoạt bộ rubric (bộ Active trước đó cho cùng dạng câu đã tự lưu trữ).");
    },
    onError: (err) => toast.error(errorMessage(err, "Không kích hoạt được.")),
  });

  const archiveMutation = useMutation({
    mutationFn: () => rubricsApi.archive(rubricSetId),
    onSuccess: () => {
      invalidate();
      toast.success("Đã lưu trữ bộ rubric.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không lưu trữ được.")),
  });

  const deleteMutation = useMutation({
    mutationFn: () => rubricsApi.remove(rubricSetId),
    onSuccess: () => {
      invalidate();
      toast.success("Đã xoá bộ rubric.");
      router.push("/admin/rubrics");
    },
    onError: (err) => toast.error(errorMessage(err, "Không xoá được bộ rubric.")),
  });

  if (isLoading || !rubricSet) {
    return <p className="text-sm text-muted">Đang tải…</p>;
  }

  const isDraft = rubricSet.status === "DRAFT";
  const isActive = rubricSet.status === "ACTIVE";
  const totalWeight = rubricSet.rubrics.reduce((sum, r) => sum + r.weight, 0);

  return (
    <div className="max-w-3xl">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-muted">
            {QUESTION_TYPE_LABEL[rubricSet.questionType]} · v{rubricSet.version}
          </p>
          <h1 className="mt-0.5 font-serif text-2xl font-medium text-ink">{rubricSet.name}</h1>
        </div>
        <StatusBadge status={rubricSet.status} />
      </div>

      <div className="mt-6 flex flex-wrap gap-2 border-b border-border pb-6">
        {isDraft && (
          <ConfirmButton
            label="Kích hoạt (Active)"
            title="Kích hoạt bộ rubric này?"
            description="Bộ Active hiện tại (nếu có) cho cùng dạng câu sẽ tự động chuyển sang Lưu trữ."
            variant="teal"
            onConfirm={() => activateMutation.mutateAsync()}
          />
        )}
        {isActive && (
          <ConfirmButton
            label="Lưu trữ (Archive)"
            title="Lưu trữ bộ rubric này?"
            description="Bài Writing mới sẽ không còn được chấm theo bộ rubric này."
            onConfirm={() => archiveMutation.mutateAsync()}
          />
        )}
      </div>

      <RubricSetBasicInfoForm key={rubricSet.rubricSetId} rubricSet={rubricSet} isDraft={isDraft} onSaved={invalidate} />

      {isDraft && (
        <div className="mt-3">
          <ConfirmButton
            label="Xoá bộ rubric"
            title="Xoá bộ rubric này?"
            description="Chỉ xoá được khi còn ở trạng thái Nháp."
            confirmLabel="Xoá"
            onConfirm={() => deleteMutation.mutateAsync()}
          />
        </div>
      )}

      <div className="mt-10 border-t border-border pt-6">
        <div className="flex items-center justify-between">
          <h2 className="font-serif text-lg font-medium text-ink">
            Tiêu chí ({rubricSet.rubrics.length})
          </h2>
          <div className="flex items-center gap-3">
            <span
              className={
                Math.abs(totalWeight - 1) < 0.001
                  ? "text-[13px] text-success"
                  : "text-[13px] text-gold"
              }
            >
              Tổng trọng số: {totalWeight.toFixed(2)} {Math.abs(totalWeight - 1) >= 0.001 && "(cần = 1.00)"}
            </span>
            {isDraft && (
              <Link href={`/admin/rubrics/${rubricSetId}/rubric-sets/import`}>
                <Button type="button" variant="outline" size="sm">
                  Nhập từ Excel
                </Button>
              </Link>
            )}
          </div>
        </div>

        {rubricSet.rubrics.length === 0 ? (
          <p className="mt-4 text-sm text-muted">Chưa có tiêu chí nào.</p>
        ) : (
          <ul className="mt-4 space-y-2">
            {rubricSet.rubrics
              .slice()
              .sort((a, b) => a.criterion.localeCompare(b.criterion) || a.level - b.level)
              .map((rubric) => (
                <RubricRow
                  key={rubric.rubricId}
                  rubric={rubric}
                  isDraft={isDraft}
                  onChanged={invalidate}
                />
              ))}
          </ul>
        )}

        {isDraft && <AddRubricForm rubricSetId={rubricSetId} onAdded={invalidate} />}
      </div>
    </div>
  );
}

const emptyNewRubric: CreateRubricPayload = {
  criterion: "",
  level: 1,
  minScore: 0,
  maxScore: 100,
  description: "",
  scoringRule: "",
  weight: 0.2,
};

function RubricSetBasicInfoForm({
  rubricSet,
  isDraft,
  onSaved,
}: {
  rubricSet: RubricSet;
  isDraft: boolean;
  onSaved: () => void;
}) {
  const [name, setName] = useState(rubricSet.name);
  const [description, setDescription] = useState(rubricSet.description ?? "");

  const updateMutation = useMutation({
    mutationFn: () =>
      rubricsApi.update(rubricSet.rubricSetId, {
        name,
        questionType: rubricSet.questionType,
        description: description || undefined,
        version: rubricSet.version,
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
        <Label htmlFor="name">Tên bộ rubric</Label>
        <Input
          id="name"
          value={name}
          disabled={!isDraft}
          onChange={(e) => setName(e.target.value)}
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
      {isDraft && (
        <Button
          type="button"
          variant="teal"
          disabled={updateMutation.isPending}
          onClick={() => updateMutation.mutate()}
        >
          {updateMutation.isPending ? "Đang lưu…" : "Lưu thay đổi"}
        </Button>
      )}
    </div>
  );
}

function AddRubricForm({ rubricSetId, onAdded }: { rubricSetId: string; onAdded: () => void }) {
  const [form, setForm] = useState<CreateRubricPayload>(emptyNewRubric);

  const addMutation = useMutation({
    mutationFn: () => rubricsApi.addRubric(rubricSetId, form),
    onSuccess: () => {
      onAdded();
      setForm(emptyNewRubric);
      toast.success("Đã thêm tiêu chí.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không thêm được tiêu chí.")),
  });

  function update<K extends keyof CreateRubricPayload>(key: K, value: CreateRubricPayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  return (
    <div className="mt-4 space-y-3 rounded-sm border border-dashed border-border p-4">
      <p className="text-[13px] font-medium text-ink-soft">Thêm tiêu chí mới</p>
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1.5">
          <Label className="text-[12px]">Tiêu chí</Label>
          <Input
            placeholder="Grammar, Vocabulary, Image Relevance…"
            value={form.criterion}
            onChange={(e) => update("criterion", e.target.value)}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-[12px]">Mức độ (Level)</Label>
          <Input
            type="number"
            min={1}
            value={form.level}
            onChange={(e) => update("level", Number(e.target.value))}
          />
        </div>
      </div>
      <div className="grid grid-cols-3 gap-3">
        <div className="space-y-1.5">
          <Label className="text-[12px]">Điểm tối thiểu</Label>
          <Input
            type="number"
            value={form.minScore}
            onChange={(e) => update("minScore", Number(e.target.value))}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-[12px]">Điểm tối đa</Label>
          <Input
            type="number"
            value={form.maxScore}
            onChange={(e) => update("maxScore", Number(e.target.value))}
          />
        </div>
        <div className="space-y-1.5">
          <Label className="text-[12px]">Trọng số (0–1)</Label>
          <Input
            type="number"
            min={0}
            max={1}
            step="0.05"
            value={form.weight}
            onChange={(e) => update("weight", Number(e.target.value))}
          />
        </div>
      </div>
      <div className="space-y-1.5">
        <Label className="text-[12px]">Mô tả mức độ đạt</Label>
        <Textarea value={form.description} onChange={(e) => update("description", e.target.value)} />
      </div>
      <div className="space-y-1.5">
        <Label className="text-[12px]">Quy tắc ánh xạ điểm (scoringRule)</Label>
        <Input value={form.scoringRule} onChange={(e) => update("scoringRule", e.target.value)} />
      </div>
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={!form.criterion || addMutation.isPending}
        onClick={() => addMutation.mutate()}
      >
        Thêm tiêu chí
      </Button>
    </div>
  );
}

function RubricRow({
  rubric,
  isDraft,
  onChanged,
}: {
  rubric: Rubric;
  isDraft: boolean;
  onChanged: () => void;
}) {
  const [form, setForm] = useState<CreateRubricPayload>({
    criterion: rubric.criterion,
    level: rubric.level,
    minScore: rubric.minScore,
    maxScore: rubric.maxScore,
    description: rubric.description ?? "",
    scoringRule: rubric.scoringRule ?? "",
    weight: rubric.weight,
  });
  const dirty = JSON.stringify(form) !== JSON.stringify({
    criterion: rubric.criterion,
    level: rubric.level,
    minScore: rubric.minScore,
    maxScore: rubric.maxScore,
    description: rubric.description ?? "",
    scoringRule: rubric.scoringRule ?? "",
    weight: rubric.weight,
  });

  const saveMutation = useMutation({
    mutationFn: () => rubricsApi.updateRubric(rubric.rubricId, form),
    onSuccess: () => {
      onChanged();
      toast.success("Đã lưu tiêu chí.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không lưu được tiêu chí.")),
  });

  const deleteMutation = useMutation({
    mutationFn: () => rubricsApi.removeRubric(rubric.rubricId),
    onSuccess: () => {
      onChanged();
      toast.success("Đã xoá tiêu chí.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không xoá được tiêu chí.")),
  });

  function update<K extends keyof CreateRubricPayload>(key: K, value: CreateRubricPayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  return (
    <li className="rounded-sm border border-border bg-paper-raised p-3">
      <div className="grid grid-cols-[1fr_auto_auto_auto] items-end gap-3">
        <div className="space-y-1.5">
          <Label className="text-[12px]">Tiêu chí</Label>
          <Input
            value={form.criterion}
            disabled={!isDraft}
            onChange={(e) => update("criterion", e.target.value)}
          />
        </div>
        <div className="w-20 space-y-1.5">
          <Label className="text-[12px]">Level</Label>
          <Input
            type="number"
            disabled={!isDraft}
            value={form.level}
            onChange={(e) => update("level", Number(e.target.value))}
          />
        </div>
        <div className="w-24 space-y-1.5">
          <Label className="text-[12px]">Điểm (min–max)</Label>
          <div className="flex gap-1">
            <Input
              type="number"
              disabled={!isDraft}
              value={form.minScore}
              onChange={(e) => update("minScore", Number(e.target.value))}
              className="w-12 px-1.5"
            />
            <Input
              type="number"
              disabled={!isDraft}
              value={form.maxScore}
              onChange={(e) => update("maxScore", Number(e.target.value))}
              className="w-12 px-1.5"
            />
          </div>
        </div>
        <div className="w-20 space-y-1.5">
          <Label className="text-[12px]">Trọng số</Label>
          <Input
            type="number"
            step="0.05"
            disabled={!isDraft}
            value={form.weight}
            onChange={(e) => update("weight", Number(e.target.value))}
          />
        </div>
      </div>
      {isDraft && (
        <div className="mt-2 flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={!dirty || saveMutation.isPending}
            onClick={() => saveMutation.mutate()}
          >
            Lưu
          </Button>
          <ConfirmButton
            label="Xoá"
            title={`Xoá tiêu chí "${rubric.criterion}"?`}
            onConfirm={() => deleteMutation.mutateAsync()}
          />
        </div>
      )}
    </li>
  );
}
