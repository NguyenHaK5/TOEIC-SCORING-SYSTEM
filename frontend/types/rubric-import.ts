// File: frontend/types/rubric-import.ts — MỚI
export interface BulkRubricRow {
  rowNumber: number;
  criterion: string;
  level: number;
  minScore: number;
  maxScore: number;
  description?: string;
  scoringRule?: string;
  weight: number;
}

export interface BulkUpdateRubricRow extends BulkRubricRow {
  rubricId: string;
}