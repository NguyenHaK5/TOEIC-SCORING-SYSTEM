// File: frontend/app/admin/rubrics/[rubricSetId]/rubrics/import/page.tsx
"use client";
import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  downloadRubricTemplate, exportRubricsToExcel,
  parseRubricExcel, parseRubricExcelForUpdate,
} from "@/lib/admin/rubric-import-template";
import { bulkCreateRubrics, bulkUpdateRubrics } from "@/lib/admin/rubrics-api";
import { BulkRubricRow, BulkUpdateRubricRow } from "@/types/rubric-import";

// Import thêm các UI components dựa theo page_question.tsx
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type Mode = "create" | "update";

export default function ImportRubricsPage() {
  const { rubricSetId } = useParams<{ rubricSetId: string }>();
  const router = useRouter();
  const [mode, setMode] = useState<Mode>("create");
  const [rows, setRows] = useState<unknown[]>([]);
  const [serverErrors, setServerErrors] = useState<{ rowNumber: number; errors: string[] }[]>([]);
  const [submitting, setSubmitting] = useState(false);

  async function handleFile(file: File) {
    const { rows } = mode === "create" ? await parseRubricExcel(file) : await parseRubricExcelForUpdate(file);
    setRows(rows);
    setServerErrors([]);
  }

  async function handleSubmit() {
    setSubmitting(true);
    try {
        if (mode === "create") {
        const result = await bulkCreateRubrics(rubricSetId, rows as BulkRubricRow[]);
        if (result.success) {
            toast.success(`Đã tạo ${result.createdCount} tiêu chí.`);
            router.push(`/admin/rubrics/${rubricSetId}`);
        } else {
            setServerErrors(result.errors);
            toast.error("Có lỗi — chưa áp dụng thay đổi nào (all-or-nothing).");
        }
        } else {
        const result = await bulkUpdateRubrics(rubricSetId, rows as BulkUpdateRubricRow[]);
        if (result.success) {
            toast.success(`Đã sửa ${result.updatedCount} tiêu chí.`);
            router.push(`/admin/rubrics/${rubricSetId}`);
        } else {
            setServerErrors(result.errors);
            toast.error("Có lỗi — chưa áp dụng thay đổi nào (all-or-nothing).");
        }
        }
    } finally {
        setSubmitting(false);
    }
  }

  return (
    <div className="max-w-xl">
      <div className="mt-6 space-y-5">
        <div className="flex gap-3">
          <Button
            type="button"
            variant={mode === "create" ? "teal" : "outline"}
            onClick={() => { setMode("create"); setRows([]); }}
          >
            Tạo mới hàng loạt
          </Button>
          <Button
            type="button"
            variant={mode === "update" ? "teal" : "outline"}
            onClick={() => { setMode("update"); setRows([]); }}
          >
            Sửa hàng loạt
          </Button>
        </div>

        <div className="space-y-2">
          <Label>Mẫu dữ liệu (Template)</Label>
          <div>
            {mode === "create" ? (
              <Button type="button" variant="outline" onClick={downloadRubricTemplate}>
                Tải template rỗng
              </Button>
            ) : (
              <Button type="button" variant="outline" onClick={() => exportRubricsToExcel(rubricSetId, "rubric-set")}>
                Tải tiêu chí hiện có để sửa
              </Button>
            )}
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="fileInput">Tệp dữ liệu (.xlsx, .xls)</Label>
          <Input 
            id="fileInput" 
            type="file" 
            accept=".xlsx,.xls" 
            onChange={(e) => e.target.files?.[0] && handleFile(e.target.files[0])} 
          />
        </div>

        {rows.length > 0 && (
          <div className="flex gap-3 pt-2">
            <Button 
              type="button" 
              variant="teal" 
              disabled={submitting} 
              onClick={handleSubmit}
            >
              {mode === "create" ? `Tạo ${rows.length} tiêu chí` : `Sửa ${rows.length} tiêu chí`}
            </Button>
          </div>
        )}

        {serverErrors.length > 0 && (
          <div className="mt-2 space-y-1 rounded-sm border border-red-500/30 bg-red-500/5 px-3.5 py-2.5 text-[13px] text-red-600">
            {serverErrors.map((e) => (
              <p key={e.rowNumber}>
                {e.rowNumber === 0 ? "Toàn bộ RubricSet" : `Dòng ${e.rowNumber}`}: {e.errors.join("; ")}
              </p>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}