/**
 * ⚠️ GIẢ ĐỊNH cho danh sách (`WritingAnswerListItem`, list endpoint) —
 * chưa có ví dụ JSON, xem PHASE7-ISSUE-15.
 *
 * ✅ `WritingAnswerDetail` ĐÃ XÁC NHẬN bằng JSON response thật (06/09/2026)
 * cho endpoint `GET /rater/writing-answers/{id}`. Khác giả định ban đầu:
 * - Mảng lịch sử Human tên thật là `humanAssessments` (không phải
 *   `humanHistory`) — đây chính là field gây crash `.length` trước đó.
 * - KHÔNG có `questionNumber` — chỉ có `questionId`.
 * - KHÔNG có `candidateCode` — chỉ có `candidateId` (GUID) + `candidateName`.
 * - `AiCriterionScore`/tiêu chí trong Rubric dùng field `criterion`
 *   (không phải `criterionCode`), có thêm `weight` (thường `null` ở AI).
 */

export type RaterFilterStatus = "pending" | "rated" | "all";

export interface WritingAnswerListItem {
  candidateAnswerId: string;
  examSessionId: string;
  candidateName: string;
  candidateCode: string;
  questionNumber: number;
  questionType: import("./question").QuestionType;
  answeredAt: string;
  /** Điểm AI hiện hành (WritingAssessment.IsCurrent), null nếu AI chưa chấm xong. */
  aiScore: number | null;
  /** Điểm Human chính thức (HumanAssessment.IsFinal), null nếu chưa Rater nào chấm. */
  humanScore: number | null;
  isRated: boolean;
}

export interface AiCriterionScore {
  criterion: string;
  score: number;
  weight: number | null;
  reasoning: string | null;
}

export interface AiAssessmentSummary {
  writingAssessmentId: string;
  overallScore: number;
  feedback: string | null;
  modelName: string | null;
  promptVersion?: string | null;
  strengths?: string[];
  weaknesses?: string[];
  criterionScores: AiCriterionScore[];
}

export interface RubricCriterionDefinition {
  rubricId: string;
  criterion: string;
  level?: number;
  minScore: number;
  maxScore: number;
  weight: number;
  description: string | null;
}

export interface ActiveRubricSummary {
  rubricSetId: string;
  name: string;
  version?: string;
  criteria: RubricCriterionDefinition[];
}

/** ⚠️ Vẫn là giả định — ví dụ thật có `humanAssessments: []` (rỗng), nên
 * shape từng phần tử bên trong CHƯA được xác nhận. Field tên mảng
 * (`humanAssessments`) thì đã chắc chắn đúng. */
export interface HumanCriterionScore {
  criterion: string;
  score: number;
}

export interface HumanAssessmentHistoryItem {
  humanAssessmentId: string;
  raterName: string;
  overallScore: number;
  feedback: string | null;
  isFinal: boolean;
  createdAt: string;
  criterionScores: HumanCriterionScore[];
}

export interface WritingAnswerDetail {
  candidateAnswerId: string;
  examSessionId: string;
  candidateId: string;
  candidateName: string;
  questionId: string;
  questionType: import("./question").QuestionType;
  questionText: string;
  imageUrl: string | null;
  requiredWords: string | null;
  answerText: string;
  aiAssessment: AiAssessmentSummary | null;
  activeRubric: ActiveRubricSummary | null;
  humanAssessments: HumanAssessmentHistoryItem[];
}

export interface SubmitHumanScorePayload {
  criterionScores: { criterion: string; score: number }[];
  feedback?: string;
}
