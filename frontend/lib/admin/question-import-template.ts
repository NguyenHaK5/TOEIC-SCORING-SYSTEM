// File: frontend/lib/admin/question-import-template.ts
import * as XLSX from "xlsx";
import { fetchQuestionsForExport } from "@/lib/admin/questions-api";

const TEMPLATE_HEADERS = [
  "QuestionNumber", "Section", "QuestionType", "QuestionText",
  "ImageUrl", "AudioUrl", "RequiredWords", "Points",
  "OptionA", "OptionB", "OptionC", "OptionD", "CorrectOption", // "A"|"B"|"C"|"D"
];

const EXPORT_HEADERS = [
  "QuestionId", "QuestionNumber", "Section", "QuestionType", "QuestionText",
  "ImageUrl", "AudioUrl", "RequiredWords", "Points",
  "OptionA", "OptionB", "OptionC", "OptionD", "CorrectOption",
];

export function downloadQuestionTemplate() {
  const sample = [
    {
      QuestionNumber: 1, Section: "LISTENING", QuestionType: "MULTIPLE_CHOICE",
      QuestionText: "Look at the picture and select the best description.",
      ImageUrl: "https://.../img1.jpg", AudioUrl: "https://.../q1.mp3",
      RequiredWords: "", Points: 1,
      OptionA: "A man is reading a book.", OptionB: "A man is driving a car.",
      OptionC: "A woman is cooking.", OptionD: "A woman is swimming.",
      CorrectOption: "A",
    },
  ];
  const ws = XLSX.utils.json_to_sheet(sample, { header: TEMPLATE_HEADERS });
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, "Questions");
  XLSX.writeFile(wb, "toeic-question-template.xlsx");
}

export function parseQuestionExcel(file: File): Promise<{
  rows: import("@/types/question-import").BulkQuestionRow[];
  parseErrors: string[];
}> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const wb = XLSX.read(e.target?.result, { type: "binary" });
        const sheet = wb.Sheets[wb.SheetNames[0]];
        const raw = XLSX.utils.sheet_to_json<Record<string, unknown>>(sheet, { defval: "" });

        const parseErrors: string[] = [];
        const rows = raw.map((r, idx) => {
          const rowNumber = idx + 2; // +2: header là dòng 1, data bắt đầu dòng 2
          const options = ["A", "B", "C", "D"]
            .filter((label) => String(r[`Option${label}`] ?? "").trim() !== "")
            .map((label) => ({
              optionLabel: label,
              optionText: String(r[`Option${label}`]),
              isCorrect: String(r.CorrectOption).trim().toUpperCase() === label,
            }));

          if (options.length > 0 && !options.some((o) => o.isCorrect)) {
            parseErrors.push(`Dòng ${rowNumber}: CorrectOption không khớp option nào.`);
          }

          return {
            rowNumber,
            questionNumber: Number(r.QuestionNumber),
            section: String(r.Section).trim().toUpperCase(),
            questionType: String(r.QuestionType).trim().toUpperCase(),
            questionText: String(r.QuestionText ?? ""),
            imageUrl: String(r.ImageUrl ?? "") || undefined,
            audioUrl: String(r.AudioUrl ?? "") || undefined,
            requiredWords: String(r.RequiredWords ?? "") || undefined,
            points: r.Points ? Number(r.Points) : undefined,
            options,
          };
        });

        resolve({ rows, parseErrors });
      } catch {
        reject(new Error("Không đọc được file Excel — kiểm tra đúng định dạng template."));
      }
    };
    reader.readAsBinaryString(file);
  });
}



export async function exportQuestionsToExcel(testId: string, testCode: string) {
  const rows = await fetchQuestionsForExport(testId);

  const data = rows.map((q) => {
    const optionByLabel = Object.fromEntries(q.options.map((o) => [o.optionLabel, o]));
    const correct = q.options.find((o) => o.isCorrect);
    return {
      QuestionId: q.questionId, // giữ nguyên — KHÔNG được sửa/xoá cột này khi chỉnh sửa file
      QuestionNumber: q.questionNumber,
      Section: q.section,
      QuestionType: q.questionType,
      QuestionText: q.questionText,
      ImageUrl: q.imageUrl ?? "",
      AudioUrl: q.audioUrl ?? "",
      RequiredWords: q.requiredWords ?? "",
      Points: q.points ?? "",
      OptionA: optionByLabel["A"]?.optionText ?? "",
      OptionB: optionByLabel["B"]?.optionText ?? "",
      OptionC: optionByLabel["C"]?.optionText ?? "",
      OptionD: optionByLabel["D"]?.optionText ?? "",
      CorrectOption: correct?.optionLabel ?? "",
    };
  });

  const ws = XLSX.utils.json_to_sheet(data, { header: EXPORT_HEADERS });
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, "Questions");
  XLSX.writeFile(wb, `${testCode}-questions.xlsx`);
}

export function parseQuestionExcelForUpdate(file: File): Promise<{
  rows: import("@/types/question-import").BulkUpdateQuestionRow[];
  parseErrors: string[];
}> {
  // Giống parseQuestionExcel (ISSUE-19), CHỈ khác: đọc thêm bắt buộc cột QuestionId,
  // báo lỗi client-side ngay nếu dòng nào thiếu/không phải GUID hợp lệ.
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const wb = XLSX.read(e.target?.result, { type: "binary" });
        const sheet = wb.Sheets[wb.SheetNames[0]];
        const raw = XLSX.utils.sheet_to_json<Record<string, unknown>>(sheet, { defval: "" });

        const parseErrors: string[] = [];
        const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

        const rows = raw.map((r, idx) => {
          const rowNumber = idx + 2;
          const questionId = String(r.QuestionId ?? "").trim();
          if (!GUID_RE.test(questionId)) {
            parseErrors.push(`Dòng ${rowNumber}: thiếu hoặc sai cột QuestionId — không được xoá/sửa cột này.`);
          }

          const options = ["A", "B", "C", "D"]
            .filter((label) => String(r[`Option${label}`] ?? "").trim() !== "")
            .map((label) => ({
              optionLabel: label,
              optionText: String(r[`Option${label}`]),
              isCorrect: String(r.CorrectOption).trim().toUpperCase() === label,
            }));

          return {
            rowNumber,
            questionId,
            questionNumber: Number(r.QuestionNumber),
            section: String(r.Section).trim().toUpperCase(),
            questionType: String(r.QuestionType).trim().toUpperCase(),
            questionText: String(r.QuestionText ?? ""),
            imageUrl: String(r.ImageUrl ?? "") || undefined,
            audioUrl: String(r.AudioUrl ?? "") || undefined,
            requiredWords: String(r.RequiredWords ?? "") || undefined,
            points: r.Points ? Number(r.Points) : undefined,
            options,
          };
        });

        resolve({ rows, parseErrors });
      } catch {
        reject(new Error("Không đọc được file Excel."));
      }
    };
    reader.readAsBinaryString(file);
  });
}