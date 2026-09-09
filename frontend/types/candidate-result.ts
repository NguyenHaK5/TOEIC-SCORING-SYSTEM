/**
 * ✅ ĐÃ XÁC NHẬN bằng JSON response thật (06/09/2026) — không còn là giả
 * định. Khác khá nhiều so với thiết kế ban đầu:
 * - KHÔNG có object lồng `listening`/`reading` — điểm nằm phẳng ở top-level
 *   (`listeningScore`, `listeningCorrectCount`, `listeningTotalCount`...).
 *   Backend KHÔNG phân biệt "sai" vs "bỏ trống" ở response này (chỉ có
 *   `TotalCount`/`CorrectCount`), nên Frontend hiển thị "chưa đúng" =
 *   total - correct thay vì tách riêng "sai"/"bỏ trống".
 * - Trạng thái Writing là chuỗi PascalCase `"Pending"`/`"Scored"`/
 *   `"NotAnswered"` (không phải SCREAMING_CASE như giả định ban đầu).
 * - Mỗi câu Writing KHÔNG có `questionNumber` — chỉ có `questionId`.
 *   Frontend dùng số thứ tự trong mảng làm nhãn hiển thị tạm thời.
 * - `weaknesses`/`strengths`/`corrections` là các mảng chuỗi đi kèm
 *   `feedback` — hiển thị thêm để tận dụng dữ liệu đã có.
 * - Có `totalScore` ở top-level nhưng CỐ Ý KHÔNG hiển thị (đúng
 *   PHASE3-ISSUE-07/PHASE6-ISSUE-02 — Listening/Reading và Writing khác
 *   thang điểm, không gộp).
 * - KHÔNG có `examName` trong response — bỏ khỏi UI.
 */

export type WritingScoreStatus = "Pending" | "Scored" | "NotAnswered";

export interface WritingCriterionScore {
  criterion: string;
  score: number;
  weight: number;
}

export interface WritingQuestionResult {
  questionId: string;
  questionType: import("./question").QuestionType;
  status: WritingScoreStatus;
  overallScore: number | null;
  feedback: string | null;
  strengths: string[];
  weaknesses: string[];
  corrections: string[];
  criterionScores: WritingCriterionScore[];
}

export interface WritingSectionResult {
  questionsTotal: number;
  questionsScored: number;
  overallScore: number | null;
  questions: WritingQuestionResult[];
}

export interface ExamSessionResult {
  examSessionId: string;
  status: string;
  submittedAt: string;
  listeningScore: number;
  listeningCorrectCount: number;
  listeningTotalCount: number;
  readingScore: number;
  readingCorrectCount: number;
  readingTotalCount: number;
  writingScore: number | null;
  /** Có trong response nhưng cố ý KHÔNG hiển thị — xem comment đầu file. */
  totalScore: number | null;
  writing: WritingSectionResult;
}

export function writingOverallStatus(writing: WritingSectionResult): WritingScoreStatus {
  const statuses = writing.questions.map((q) => q.status);
  if (statuses.some((s) => s === "Pending")) return "Pending";
  if (statuses.length === 0 || statuses.every((s) => s === "NotAnswered")) return "NotAnswered";
  return "Scored";
}
