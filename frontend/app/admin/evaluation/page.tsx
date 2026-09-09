"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { evaluationApi } from "@/lib/admin/evaluation-api";
import { errorMessage } from "@/lib/admin/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { MetricValue } from "@/components/admin/metric-value";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { QUESTION_TYPE_LABEL, type QuestionType } from "@/types/question";

export default function EvaluationPage() {
  return (
    <div className="max-w-4xl space-y-10">
      <div>
        <h1 className="font-serif text-2xl font-medium text-ink">Đánh giá AI vs Human</h1>
        <p className="mt-1 text-sm text-muted">
          Dữ liệu từ <code className="rounded-sm bg-white px-1.5 py-0.5 text-[13px]">evaluation-service</code>{" "}
          (Python, Phase 5) — độc lập với backend .NET.
        </p>
      </div>

      <DatasetSection />
      <MetricsSection />
      <ReportSection />
    </div>
  );
}

function DatasetSection() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["evaluation", "dataset"],
    queryFn: () => evaluationApi.dataset(),
  });

  return (
    <section>
      <h2 className="font-serif text-lg font-medium text-ink">Dataset</h2>
      {isLoading && <p className="mt-3 text-sm text-muted">Đang tải…</p>}
      {isError && <ConnectionError service="evaluation-service" />}
      {data && (
        <div className="mt-4 grid grid-cols-2 gap-4">
          <div className="rounded-sm border border-border bg-paper-raised p-5">
            <p className="text-[13px] text-muted">Tổng số cặp AI–Human (overall)</p>
            <p className="mt-1 font-serif text-3xl font-medium text-ink">{data.totalPairs}</p>
            {data.totalPairs === 0 && (
              <p className="mt-2 text-[13px] text-gold">
                Chưa có dữ liệu — cần ít nhất 1 bài vừa được AI chấm vừa được Rater chấm (Phase 4).
              </p>
            )}
          </div>
          <div className="rounded-sm border border-border bg-paper-raised p-5">
            <p className="text-[13px] text-muted">Tổng số dòng theo tiêu chí (Criterion)</p>
            <p className="mt-1 font-serif text-3xl font-medium text-ink">{data.totalCriterionRows}</p>
          </div>
        </div>
      )}
    </section>
  );
}

function MetricsSection() {
  const [questionType, setQuestionType] = useState<QuestionType | "ALL">("ALL");
  const [agreementThreshold, setAgreementThreshold] = useState(5);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["evaluation", "metrics", questionType, agreementThreshold],
    queryFn: () =>
      evaluationApi.metrics({
        questionType: questionType === "ALL" ? undefined : questionType,
        agreementThreshold,
      }),
  });

  return (
    <section>
      <div className="flex items-center justify-between">
        <h2 className="font-serif text-lg font-medium text-ink">Metrics</h2>
        <div className="flex items-center gap-3">
          <Select value={questionType} onValueChange={(v) => setQuestionType(v as QuestionType | "ALL")}>
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">Mọi dạng câu</SelectItem>
              {Object.entries(QUESTION_TYPE_LABEL).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <div className="flex items-center gap-2">
            <Label htmlFor="agreementThreshold" className="text-[12px] whitespace-nowrap">
              Ngưỡng đồng ý
            </Label>
            <Input
              id="agreementThreshold"
              type="number"
              min={0}
              step="0.5"
              value={agreementThreshold}
              onChange={(e) => setAgreementThreshold(Number(e.target.value) || 0)}
              className="w-20"
            />
          </div>
        </div>
      </div>

      {isLoading && <p className="mt-3 text-sm text-muted">Đang tải…</p>}
      {isError && <ConnectionError service="evaluation-service" />}

      {data && (
        <div className="mt-4 space-y-4">
          <div className="rounded-sm border border-border bg-paper-raised p-5">
            <p className="mb-3 text-[13px] font-medium text-ink-soft">
              Tổng thể (n = {data.overall.n})
            </p>
            <div className="grid grid-cols-5 gap-4">
              <MetricValue label="MAE" value={data.overall.mae} />
              <MetricValue label="RMSE" value={data.overall.rmse} />
              <MetricValue label="Pearson" value={data.overall.pearsonR} />
              <MetricValue label="Spearman" value={data.overall.spearmanR} />
              <MetricValue label="Agreement Rate" value={data.overall.agreementRate} decimals={2} />
            </div>
            {data.overall.notes && data.overall.notes.length > 0 && (
              <div className="mt-3 space-y-1">
                {data.overall.notes.map((note, i) => (
                  <p key={i} className="text-[12px] text-muted">
                    {note}
                  </p>
                ))}
              </div>
            )}
          </div>

          {Object.keys(data.byCriterion).length > 0 && (
            <div className="rounded-sm border border-border bg-paper-raised p-5">
              <p className="mb-3 text-[13px] font-medium text-ink-soft">Theo tiêu chí (Criterion)</p>
              <div className="space-y-3">
                {Object.entries(data.byCriterion).map(([criterion, m]) => (
                  <div key={criterion} className="flex items-center justify-between border-t border-border pt-3 first:border-t-0 first:pt-0">
                    <span className="text-sm text-ink-soft">{criterion}</span>
                    <span className="text-[13px] text-muted">
                      MAE {m.mae?.toFixed(2) ?? "—"} · n={m.n}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </section>
  );
}

function ReportSection() {
  const [minDifference, setMinDifference] = useState(10);
  const [topN, setTopN] = useState(20);
  const [exporting, setExporting] = useState(false);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["evaluation", "report", minDifference, topN],
    queryFn: () => evaluationApi.report({ minDifference, topN }),
  });

  async function handleExport(format: "csv" | "json") {
    setExporting(true);
    try {
      await evaluationApi.downloadExport({ format, level: "overall" });
    } catch (err) {
      toast.error(errorMessage(err, "Không tải được file export."));
    } finally {
      setExporting(false);
    }
  }

  return (
    <section>
      <div className="flex items-center justify-between">
        <h2 className="font-serif text-lg font-medium text-ink">Report — top lệch điểm</h2>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled={exporting} onClick={() => handleExport("csv")}>
            Xuất CSV
          </Button>
          <Button variant="outline" size="sm" disabled={exporting} onClick={() => handleExport("json")}>
            Xuất JSON
          </Button>
        </div>
      </div>

      <div className="mt-3 flex items-center gap-4">
        <div className="flex items-center gap-2">
          <Label htmlFor="minDifference" className="text-[12px] whitespace-nowrap">
            Lệch tối thiểu
          </Label>
          <Input
            id="minDifference"
            type="number"
            min={0}
            value={minDifference}
            onChange={(e) => setMinDifference(Number(e.target.value) || 0)}
            className="w-20"
          />
        </div>
        <div className="flex items-center gap-2">
          <Label htmlFor="reportTopN" className="text-[12px] whitespace-nowrap">
            Top
          </Label>
          <Input
            id="reportTopN"
            type="number"
            min={1}
            max={500}
            value={topN}
            onChange={(e) => setTopN(Number(e.target.value) || 20)}
            className="w-20"
          />
        </div>
      </div>

      {isLoading && <p className="mt-4 text-sm text-muted">Đang tải…</p>}
      {isError && <ConnectionError service="evaluation-service" />}

      {data && (
        <div className="mt-4 overflow-hidden rounded-sm border border-border bg-paper-raised">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-border bg-paper text-[13px] text-muted">
              <tr>
                <th className="px-4 py-3 font-medium">Thí sinh</th>
                <th className="px-4 py-3 font-medium">Câu</th>
                <th className="px-4 py-3 font-medium">AI</th>
                <th className="px-4 py-3 font-medium">Human</th>
                <th className="px-4 py-3 font-medium">Lệch</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data.cases.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted">
                    Không có case nào vượt ngưỡng lệch đã chọn.
                  </td>
                </tr>
              ) : (
                data.cases.map((item) => (
                  <tr key={item.candidateAnswerId} className="transition-colors hover:bg-paper">
                    <td className="px-4 py-3 text-ink-soft">{item.raterName}</td>
                    <td className="px-4 py-3 text-ink-soft">Câu {item.questionNumber}</td>
                    <td className="px-4 py-3 text-ink-soft">{item.aiOverallScore}</td>
                    <td className="px-4 py-3 text-ink-soft">{item.humanOverallScore}</td>
                    <td className="px-4 py-3 font-medium text-danger">
                      {item.overallDifference > 0 ? "+" : ""}
                      {item.overallDifference}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
          {data.totalMatching > data.cases.length && (
            <p className="border-t border-border px-4 py-2 text-[12px] text-muted">
              Hiển thị {data.cases.length}/{data.totalMatching} case vượt ngưỡng.
            </p>
          )}
        </div>
      )}
    </section>
  );
}

function ConnectionError({ service }: { service: string }) {
  return (
    <p className="mt-3 rounded-sm border border-danger/30 bg-danger/5 px-3.5 py-2.5 text-sm text-danger">
      Không tải được dữ liệu từ {service}. Kiểm tra biến môi trường{" "}
      <code className="rounded-sm bg-white px-1 py-0.5">EVALUATION_SERVICE_URL</code> và đảm bảo
      service đang chạy.
    </p>
  );
}
