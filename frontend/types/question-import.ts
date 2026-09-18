import { BulkRowError } from "./bulk-common";

export interface BulkQuestionOption {
  optionLabel: string;
  optionText: string;
  isCorrect: boolean;
}

export interface BulkQuestionRow {
  rowNumber: number;
  questionNumber: number;
  section: string;
  questionType: string;
  questionText: string;
  imageUrl?: string | null;
  audioUrl?: string | null;
  requiredWords?: string | null;
  points?: number | null;
  options: BulkQuestionOption[];
}

// export interface BulkRowError {
//   rowNumber: number;
//   errors: string[];
// }

export interface BulkCreateQuestionsResult {
  success: boolean;
  totalRows: number;
  createdCount: number;
  errors: BulkRowError[];
}

// KHÔNG extends BulkQuestionRow vì C# QuestionExportRowDto không có RowNumber
export interface QuestionExportRow {
  questionId: string;
  questionNumber: number;
  section: string;
  questionType: string;
  questionText: string;
  imageUrl?: string | null;
  audioUrl?: string | null;
  requiredWords?: string | null;
  points?: number | null;
  options: BulkQuestionOption[];
}

// Kế thừa BulkQuestionRow vì C# BulkUpdateQuestionRowDto CÓ RowNumber
export interface BulkUpdateQuestionRow extends BulkQuestionRow {
  questionId: string;
}

export interface BulkUpdateQuestionsResult {
  success: boolean;
  totalRows: number;
  updatedCount: number;
  errors: BulkRowError[];
}