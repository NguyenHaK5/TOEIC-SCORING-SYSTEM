"use client";

import { useState } from "react";
import { downloadQuestionTemplate, parseQuestionExcel } from "@/lib/admin/question-import-template";
import { bulkCreateQuestions } from "@/lib/admin/questions-api";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import Link from "next/link";

export default function ImportQuestionsPage() {
  const { testId } = useParams<{ testId: string }>();
  const router = useRouter();
  const [rows, setRows] = useState<any[]>([]); // Bạn có thể thay 'any' bằng type BulkQuestionRow của bạn
  const [clientErrors, setClientErrors] = useState<string[]>([]);
  const [serverErrors, setServerErrors] = useState<{ rowNumber: number; errors: string[] }[]>([]);
  const [submitting, setSubmitting] = useState(false);

  async function handleFile(file: File) {
    // Reset state trước khi parse file mới
    setRows([]);
    setClientErrors([]);
    setServerErrors([]);
    
    try {
      const { rows, parseErrors } = await parseQuestionExcel(file);
      setRows(rows);
      setClientErrors(parseErrors);
    } catch (error) {
      toast.error("Lỗi khi đọc file Excel.");
    }
  }

  async function handleSubmit() {
    setSubmitting(true);
    try {
      const result = await bulkCreateQuestions(testId, rows);
      if (result.success) {
        toast.success(`Đã tạo ${result.createdCount} câu hỏi thành công.`);
        router.push(`/admin/tests/${testId}`);
      } else {
        setServerErrors(result.errors);
        toast.error("Có lỗi ở một số dòng — chưa tạo câu hỏi nào (all-or-nothing).");
      }
    } catch (error) {
      toast.error("Lỗi kết nối tới máy chủ.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="max-w-4xl space-y-6">
      {/* Header & Nút quay lại */}
      <div className="flex items-center gap-4 border-b border-border pb-4">
        <Button variant="outline" size="sm" asChild>
          <Link href={`/admin/tests/${testId}`}>Quay lại</Link>
        </Button>
        <h1 className="font-serif text-2xl font-medium text-ink">Nhập câu hỏi từ Excel</h1>
      </div>

      {/* Khu vực Upload */}
      <div className="rounded-sm border border-border bg-paper-raised p-6 shadow-sm space-y-6">
        <div className="flex items-end gap-4">
          <div className="space-y-2 flex-1">
            <label className="text-sm font-medium text-ink">Chọn file Excel (.xlsx)</label>
            <Input 
              type="file" 
              accept=".xlsx,.xls" 
              onChange={(e) => {
                if (e.target.files?.[0]) handleFile(e.target.files[0]);
              }} 
            />
          </div>
          <Button variant="secondary" onClick={downloadQuestionTemplate}>
            Tải Template Mẫu
          </Button>
        </div>

        {/* Lỗi từ file Excel (Client) */}
        {clientErrors.length > 0 && (
          <div className="rounded-md bg-red-50 p-4 border border-red-200">
            <h3 className="text-sm font-semibold text-red-800 mb-2">Lỗi định dạng file:</h3>
            <ul className="list-disc pl-5 text-sm text-red-700 space-y-1">
              {clientErrors.map((e, i) => (
                <li key={i}>{e}</li>
              ))}
            </ul>
          </div>
        )}

        {/* Lỗi từ Backend trả về (Server) */}
        {serverErrors.length > 0 && (
          <div className="rounded-md bg-red-50 p-4 border border-red-200">
            <h3 className="text-sm font-semibold text-red-800 mb-2">Lỗi từ máy chủ (Chưa lưu):</h3>
            <ul className="list-disc pl-5 text-sm text-red-700 space-y-1">
              {serverErrors.map((e) => (
                <li key={e.rowNumber}><strong>Dòng {e.rowNumber}:</strong> {e.errors.join("; ")}</li>
              ))}
            </ul>
          </div>
        )}

        {/* Trạng thái sẵn sàng */}
        {rows.length > 0 && clientErrors.length === 0 && (
          <div className="border-t border-border pt-4 flex items-center justify-between">
            <p className="text-sm text-muted">Hợp lệ: <strong className="text-teal-700">{rows.length}</strong> câu hỏi sẵn sàng tạo.</p>
            <Button 
              disabled={submitting} 
              onClick={handleSubmit}
              className="bg-teal-700 hover:bg-teal-800 text-white"
            >
              {submitting ? "Đang xử lý..." : `Tạo ${rows.length} câu hỏi`}
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}