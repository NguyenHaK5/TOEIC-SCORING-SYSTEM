/**
 * GIẢ ĐỊNH (Medium) — README.md gốc KHÔNG có ví dụ `curl` cho việc tạo
 * Question/QuestionOption (chỉ nói "ví dụ" rồi nhảy thẳng tới publish).
 * Field name dưới đây suy ra trực tiếp từ bảng schema đã chốt ở
 * PHASE0_Project_Review_Final_Architecture.md mục E.2.6/E.2.7. Nếu backend
 * thật dùng tên khác, chỉ cần sửa `lib/admin/questions-api.ts` + type ở
 * đây.
 *
 * ⚠️ ĐÃ SỬA sau khi chạy thật (cùng lỗi với Exam/Test/RubricSet): khóa
 * chính là `questionId`/`questionOptionId`, không phải `id` chung chung.
 */

export type QuestionSection = "LISTENING" | "READING" | "WRITING";

export type QuestionType =
  | "MULTIPLE_CHOICE"
  | "PICTURE_SENTENCE"
  | "WRITTEN_RESPONSE"
  | "OPINION_ESSAY";

export const QUESTION_TYPE_LABEL: Record<QuestionType, string> = {
  MULTIPLE_CHOICE: "Trắc nghiệm",
  PICTURE_SENTENCE: "Mô tả tranh (Q1–5)",
  WRITTEN_RESPONSE: "Viết thư/phản hồi (Q6–7)",
  OPINION_ESSAY: "Bài luận ý kiến (Q8)",
};

export const SECTION_LABEL: Record<QuestionSection, string> = {
  LISTENING: "Nghe (Listening)",
  READING: "Đọc (Reading)",
  WRITING: "Viết (Writing)",
};

export interface QuestionOption {
  questionOptionId: string;
  questionId: string;
  optionLabel: string;
  optionText: string;
  isCorrect: boolean;
}

export interface Question {
  questionId: string;
  testId: string;
  section: QuestionSection;
  questionType: QuestionType;
  questionNumber: number;
  questionText: string;
  imageUrl: string | null;
  audioUrl: string | null;
  requiredWords: string | null;
  points: number | null;
  options: QuestionOption[];
}

export interface CreateQuestionPayload {
  section: QuestionSection;
  questionType: QuestionType;
  questionNumber: number;
  questionText: string;
  imageUrl?: string;
  audioUrl?: string;
  requiredWords?: string;
  points?: number;
}

export type UpdateQuestionPayload = CreateQuestionPayload;

export interface CreateQuestionOptionPayload {
  optionLabel: string;
  optionText: string;
  isCorrect: boolean;
}

export type UpdateQuestionOptionPayload = CreateQuestionOptionPayload;
