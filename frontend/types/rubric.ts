/**
 * ⚠️ GIẢ ĐỊNH LỚN NHẤT của Part 2 — xem PHASE7-ISSUE-07 trong
 * PHASE7_PART2_INTEGRATION_GUIDE.md. Khác với Exam/Test (có ví dụ `curl`
 * thật trong README.md), Rubric Management (Phase 3 Part 1) chỉ được nhắc
 * tên trong `phase-tracker.md` ("Endpoint: `/rubric-sets/**`,
 * `/rubrics/{id}`") — sandbox KHÔNG có nội dung
 * `PHASE3_PART1_INTEGRATION_GUIDE.md` thật để đối chiếu field name. Toàn bộ
 * type + endpoint bên dưới là suy luận hợp lý từ schema đã chốt ở
 * PHASE0 mục E.2.10/E.2.11 — CẦN Dragon đối chiếu trước khi tin tưởng.
 *
 * ⚠️ ĐÃ SỬA sau khi chạy thật: khóa chính là `rubricId`/`rubricSetId`,
 * không phải `id` chung chung — cùng lỗi/lý do với `Exam.examId`.
 */

export type RubricSetStatus = "DRAFT" | "ACTIVE" | "ARCHIVED";

export interface Rubric {
  rubricId: string;
  rubricSetId: string;
  criterion: string;
  level: number;
  minScore: number;
  maxScore: number;
  description: string | null;
  scoringRule: string | null;
  weight: number;
}

export interface RubricSet {
  rubricSetId: string;
  name: string;
  questionType: import("./question").QuestionType;
  description: string | null;
  version: string;
  status: RubricSetStatus;
  createdAt: string;
  updatedAt: string | null;
  rubrics: Rubric[];
}

export interface CreateRubricSetPayload {
  name: string;
  questionType: import("./question").QuestionType;
  description?: string;
  version: string;
}

export type UpdateRubricSetPayload = CreateRubricSetPayload;

export interface CreateRubricPayload {
  criterion: string;
  level: number;
  minScore: number;
  maxScore: number;
  description?: string;
  scoringRule?: string;
  weight: number;
}

export type UpdateRubricPayload = CreateRubricPayload;
