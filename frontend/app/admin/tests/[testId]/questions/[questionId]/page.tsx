"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { questionsApi } from "@/lib/admin/questions-api";
import { testsApi } from "@/lib/admin/tests-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import { ConfirmButton } from "@/components/admin/confirm-button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  QUESTION_TYPE_LABEL,
  SECTION_LABEL,
  type CreateQuestionPayload,
  type Question,
  type QuestionOption,
  type QuestionSection,
  type QuestionType,
} from "@/types/question";

export default function QuestionDetailPage() {
  const { testId, questionId } = useParams<{ testId: string; questionId: string }>();
  const queryClient = useQueryClient();

  const { data: question, isLoading } = useQuery({
    queryKey: ["admin", "questions", questionId],
    queryFn: () => questionsApi.get(questionId),
  });

  const { data: test } = useQuery({
    queryKey: ["admin", "tests", testId],
    queryFn: () => testsApi.get(testId),
  });

  function invalidateQuestion() {
    queryClient.invalidateQueries({ queryKey: ["admin", "questions", questionId] });
    queryClient.invalidateQueries({ queryKey: ["admin", "tests", testId, "questions"] });
  }

  if (isLoading || !question) {
    return <p className="text-sm text-muted">Đang tải…</p>;
  }

  const isDraft = test ? test.status === "DRAFT" : false;

  return (
    <QuestionEditor
      key={question.questionId}
      question={question}
      testId={testId}
      isDraft={isDraft}
      onChanged={invalidateQuestion}
    />
  );
}

function QuestionEditor({
  question,
  testId,
  isDraft,
  onChanged,
}: {
  question: Question;
  testId: string;
  isDraft: boolean;
  onChanged: () => void;
}) {
  const router = useRouter();
  const queryClient = useQueryClient();

  // Khởi tạo trực tiếp từ prop — component chỉ mount sau khi `question` đã
  // load, và `key={question.questionId}` ở nơi gọi đảm bảo remount đúng khi đổi câu.
  const [form, setForm] = useState<CreateQuestionPayload>({
    section: question.section,
    questionType: question.questionType,
    questionNumber: question.questionNumber,
    questionText: question.questionText,
    imageUrl: question.imageUrl ?? "",
    audioUrl: question.audioUrl ?? "",
    requiredWords: question.requiredWords ?? "",
    points: question.points ?? undefined,
  });

  const updateMutation = useMutation({
    mutationFn: () => questionsApi.update(question.questionId, form),
    onSuccess: () => {
      onChanged();
      toast.success("Đã lưu câu hỏi.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không lưu được câu hỏi.")),
  });

  const deleteMutation = useMutation({
    mutationFn: () => questionsApi.remove(question.questionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "tests", testId, "questions"] });
      toast.success("Đã xoá câu hỏi.");
      router.push(`/admin/tests/${testId}`);
    },
    onError: (err) => toast.error(errorMessage(err, "Không xoá được câu hỏi.")),
  });

  function update<K extends keyof CreateQuestionPayload>(key: K, value: CreateQuestionPayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }


  const showImage = form.questionType === "PICTURE_SENTENCE";
  const showAudio = form.section === "LISTENING";
  const showRequiredWords = form.questionType === "PICTURE_SENTENCE";
  const isMultipleChoice = form.questionType === "MULTIPLE_CHOICE";

  return (
    <div className="max-w-xl">
      <p className="text-sm text-muted">
        <button
          type="button"
          onClick={() => router.push(`/admin/tests/${testId}`)}
          className="hover:underline"
        >
          ← Về bộ đề
        </button>
      </p>
      <h1 className="mt-2 font-serif text-2xl font-medium text-ink">
        Câu {question.questionNumber}
      </h1>
      {!isDraft && (
        <p className="mt-2 rounded-sm border border-gold/30 bg-gold/5 px-3.5 py-2.5 text-[13px] text-gold">
          Bộ đề đã publish — nội dung bị khoá, chỉ xem.
        </p>
      )}

      <div className="mt-6 space-y-5">
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>Phần thi</Label>
            <Select
              value={form.section}
              disabled={!isDraft}
              onValueChange={(v) => update("section", v as QuestionSection)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(SECTION_LABEL).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>Dạng câu hỏi</Label>
            <Select
              value={form.questionType}
              disabled={!isDraft}
              onValueChange={(v) => update("questionType", v as QuestionType)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(QUESTION_TYPE_LABEL).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="questionNumber">Số thứ tự</Label>
            <Input
              id="questionNumber"
              type="number"
              min={1}
              disabled={!isDraft}
              value={form.questionNumber}
              onChange={(e) => update("questionNumber", Number(e.target.value))}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="points">Điểm tham chiếu</Label>
            <Input
              id="points"
              type="number"
              min={0}
              step="0.1"
              disabled={!isDraft}
              value={form.points ?? ""}
              onChange={(e) => update("points", e.target.value ? Number(e.target.value) : undefined)}
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="questionText">Nội dung câu hỏi / Prompt</Label>
          <Textarea
            id="questionText"
            disabled={!isDraft}
            value={form.questionText}
            onChange={(e) => update("questionText", e.target.value)}
          />
        </div>

        {showAudio && (
          <div className="space-y-2">
            <Label htmlFor="audioUrl">Audio URL</Label>
            <Input
              id="audioUrl"
              disabled={!isDraft}
              value={form.audioUrl}
              onChange={(e) => update("audioUrl", e.target.value)}
            />
          </div>
        )}

        {showImage && (
          <div className="space-y-2">
            <Label htmlFor="imageUrl">Image URL</Label>
            <Input
              id="imageUrl"
              disabled={!isDraft}
              value={form.imageUrl}
              onChange={(e) => update("imageUrl", e.target.value)}
            />
            {form.imageUrl && (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={form.imageUrl}
                alt="Xem trước"
                className="mt-2 max-h-48 rounded-sm border border-border object-contain"
              />
            )}
          </div>
        )}

        {showRequiredWords && (
          <div className="space-y-2">
            <Label htmlFor="requiredWords">Từ/cụm từ bắt buộc</Label>
            <Input
              id="requiredWords"
              disabled={!isDraft}
              value={form.requiredWords}
              onChange={(e) => update("requiredWords", e.target.value)}
            />
          </div>
        )}

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
              label="Xoá câu hỏi"
              title="Xoá câu hỏi này?"
              onConfirm={() => deleteMutation.mutateAsync()}
            />
          </div>
        )}
      </div>

      {isMultipleChoice && (
        <OptionsManager
          questionId={question.questionId}
          options={question.options}
          isDraft={isDraft}
          onChanged={onChanged}
        />
      )}
    </div>
  );
}

function OptionsManager({
  questionId,
  options,
  isDraft,
  onChanged,
}: {
  questionId: string;
  options: QuestionOption[];
  isDraft: boolean;
  onChanged: () => void;
}) {
  const nextLabel = suggestNextLabel(options);
  const [newLabel, setNewLabel] = useState(nextLabel);
  const [newText, setNewText] = useState("");
  const [newCorrect, setNewCorrect] = useState(false);

  const addMutation = useMutation({
    mutationFn: () =>
      questionsApi.addOption(questionId, {
        optionLabel: newLabel,
        optionText: newText,
        isCorrect: newCorrect,
      }),
    onSuccess: () => {
      onChanged();
      setNewText("");
      setNewCorrect(false);
      toast.success("Đã thêm phương án.");
    },
    onError: (err) => toast.error(errorMessage(err, "Không thêm được phương án.")),
  });

  return (
    <div className="mt-10 border-t border-border pt-6">
      <h2 className="font-serif text-lg font-medium text-ink">
        Phương án trả lời ({options.length})
      </h2>
      <p className="mt-1 text-[13px] text-muted">
        Hệ thống sẽ báo lỗi khi Publish nếu không có đúng 1 phương án đúng.
      </p>

      {options.length === 0 ? (
        <p className="mt-4 text-sm text-muted">Chưa có phương án nào.</p>
      ) : (
        <ul className="mt-4 space-y-2">
          {options
            .slice()
            .sort((a, b) => a.optionLabel.localeCompare(b.optionLabel))
            .map((opt) => (
              <OptionRow
                key={opt.questionOptionId}
                option={opt}
                isDraft={isDraft}
                onChanged={onChanged}
              />
            ))}
        </ul>
      )}

      {isDraft && (
        <div className="mt-4 flex items-end gap-3 rounded-sm border border-dashed border-border p-3">
          <div className="w-16 space-y-1.5">
            <Label htmlFor="newLabel" className="text-[12px]">
              Ký hiệu
            </Label>
            <Input
              id="newLabel"
              value={newLabel}
              maxLength={1}
              onChange={(e) => setNewLabel(e.target.value.toUpperCase())}
            />
          </div>
          <div className="flex-1 space-y-1.5">
            <Label htmlFor="newText" className="text-[12px]">
              Nội dung phương án
            </Label>
            <Input id="newText" value={newText} onChange={(e) => setNewText(e.target.value)} />
          </div>
          <div className="flex items-center gap-2 pb-2.5">
            <Checkbox checked={newCorrect} onCheckedChange={(v) => setNewCorrect(v === true)} />
            <span className="text-[13px] text-ink-soft">Đúng</span>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={!newLabel || !newText || addMutation.isPending}
            onClick={() => addMutation.mutate()}
          >
            Thêm
          </Button>
        </div>
      )}
    </div>
  );
}

function OptionRow({
  option,
  isDraft,
  onChanged,
}: {
  option: QuestionOption;
  isDraft: boolean;
  onChanged: () => void;
}) {
  const [text, setText] = useState(option.optionText);
  const [correct, setCorrect] = useState(option.isCorrect);
  const dirty = text !== option.optionText || correct !== option.isCorrect;

  const saveMutation = useMutation({
    mutationFn: () =>
      questionsApi.updateOption(option.questionOptionId, {
        optionLabel: option.optionLabel,
        optionText: text,
        isCorrect: correct,
      }),
    onSuccess: () => {
      onChanged();
      toast.success(`Đã lưu phương án ${option.optionLabel}.`);
    },
    onError: (err) => toast.error(errorMessage(err, "Không lưu được phương án.")),
  });

  const deleteMutation = useMutation({
    mutationFn: () => questionsApi.removeOption(option.questionOptionId),
    onSuccess: () => {
      onChanged();
      toast.success(`Đã xoá phương án ${option.optionLabel}.`);
    },
    onError: (err) => toast.error(errorMessage(err, "Không xoá được phương án.")),
  });

  return (
    <li className="flex items-center gap-3 rounded-sm border border-border bg-paper-raised p-3">
      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-border font-serif text-sm text-ink-soft">
        {option.optionLabel}
      </span>
      <Input
        value={text}
        disabled={!isDraft}
        onChange={(e) => setText(e.target.value)}
        className="flex-1"
      />
      <label className="flex shrink-0 items-center gap-2 text-[13px] text-ink-soft">
        <Checkbox checked={correct} disabled={!isDraft} onCheckedChange={(v) => setCorrect(v === true)} />
        Đúng
      </label>
      {isDraft && (
        <div className="flex shrink-0 gap-2">
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
            title={`Xoá phương án ${option.optionLabel}?`}
            onConfirm={() => deleteMutation.mutateAsync()}
          />
        </div>
      )}
    </li>
  );
}

function suggestNextLabel(options: QuestionOption[]): string {
  const used = new Set(options.map((o) => o.optionLabel.toUpperCase()));
  for (const letter of ["A", "B", "C", "D", "E", "F"]) {
    if (!used.has(letter)) return letter;
  }
  return "";
}
