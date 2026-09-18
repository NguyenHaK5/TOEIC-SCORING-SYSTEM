// File: frontend/lib/admin/rubric-import-template.ts — MỚI
import { BulkRubricRow, BulkUpdateRubricRow } from "@/types/rubric-import";
import * as XLSX from "xlsx";


const CREATE_HEADERS = ["Criterion", "Level", "MinScore", "MaxScore", "Description", "ScoringRule", "Weight"];
const UPDATE_HEADERS = ["RubricId", ...CREATE_HEADERS];

export function downloadRubricTemplate() {
  const sample = [
    { Criterion: "Grammar", Level: 1, MinScore: 0, MaxScore: 25, Description: "Nhiều lỗi ngữ pháp nghiêm trọng, gây khó hiểu.", ScoringRule: "score = analysis.grammar.errorRate mapped to band", Weight: 0.25 },
    { Criterion: "Grammar", Level: 2, MinScore: 26, MaxScore: 50, Description: "Một số lỗi ngữ pháp nhưng không cản trở việc hiểu.", ScoringRule: "", Weight: 0.25 },
    { Criterion: "Grammar", Level: 3, MinScore: 51, MaxScore: 75, Description: "Ít lỗi ngữ pháp, câu văn rõ ràng.", ScoringRule: "", Weight: 0.25 },
    { Criterion: "Grammar", Level: 4, MinScore: 76, MaxScore: 100, Description: "Hầu như không có lỗi ngữ pháp.", ScoringRule: "", Weight: 0.25 },
  ];
  const ws = XLSX.utils.json_to_sheet(sample, { header: CREATE_HEADERS });
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, "Rubrics");
  XLSX.writeFile(wb, "toeic-rubric-template.xlsx");
}

export function parseRubricExcel(file: File): Promise<{
  rows: BulkRubricRow[];
  parseErrors: string[];
}> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const wb = XLSX.read(e.target?.result, { type: "binary" });
        const sheet = wb.Sheets[wb.SheetNames[0]];
        const raw = XLSX.utils.sheet_to_json<Record<string, unknown>>(sheet, { defval: "" });

        const rows = raw.map((r, idx) => ({
          rowNumber: idx + 2,
          criterion: String(r.Criterion ?? "").trim(),
          level: Number(r.Level),
          minScore: Number(r.MinScore),
          maxScore: Number(r.MaxScore),
          description: String(r.Description ?? "") || undefined,
          scoringRule: String(r.ScoringRule ?? "") || undefined,
          weight: Number(r.Weight),
        }));

        resolve({ rows, parseErrors: [] });
      } catch {
        reject(new Error("Không đọc được file Excel — kiểm tra đúng định dạng template."));
      }
    };
    reader.readAsBinaryString(file);
  });
}

export async function exportRubricsToExcel(rubricSetId: string, rubricSetName: string) {
  const { fetchRubricsForExport } = await import("./rubrics-api");
  const rows = await fetchRubricsForExport(rubricSetId);

  const data = rows.map((r) => ({
    RubricId: r.rubricId,
    Criterion: r.criterion,
    Level: r.level,
    MinScore: r.minScore,
    MaxScore: r.maxScore,
    Description: r.description ?? "",
    ScoringRule: r.scoringRule ?? "",
    Weight: r.weight,
  }));

  const ws = XLSX.utils.json_to_sheet(data, { header: UPDATE_HEADERS });
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, "Rubrics");
  XLSX.writeFile(wb, `${rubricSetName}-rubrics.xlsx`);
}

export function parseRubricExcelForUpdate(file: File): Promise<{
  rows: BulkUpdateRubricRow[];
  parseErrors: string[];
}> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const wb = XLSX.read(e.target?.result, { type: "binary" });
        const sheet = wb.Sheets[wb.SheetNames[0]];
        const raw = XLSX.utils.sheet_to_json<Record<string, unknown>>(sheet, { defval: "" });
        const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

        const parseErrors: string[] = [];
        const rows = raw.map((r, idx) => {
          const rowNumber = idx + 2;
          const rubricId = String(r.RubricId ?? "").trim();
          if (!GUID_RE.test(rubricId)) {
            parseErrors.push(`Dòng ${rowNumber}: thiếu hoặc sai cột RubricId.`);
          }
          return {
            rowNumber,
            rubricId,
            criterion: String(r.Criterion ?? "").trim(),
            level: Number(r.Level),
            minScore: Number(r.MinScore),
            maxScore: Number(r.MaxScore),
            description: String(r.Description ?? "") || undefined,
            scoringRule: String(r.ScoringRule ?? "") || undefined,
            weight: Number(r.Weight),
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