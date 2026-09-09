/**
 * Field name khớp trực tiếp với ví dụ `curl` thật trong README.md gốc
 * (mục "Part 3 — Exam Management CRUD") — KHÔNG phải giả định, đây là phần
 * đã có bằng chứng cụ thể: `examCode`, `examName`, `examDate`, `startTime`,
 * `endTime` dùng đúng nguyên văn trong ví dụ tạo Exam.
 *
 * ⚠️ ĐÃ SỬA sau khi chạy thật (Dragon báo lỗi `/admin/exams/undefined`):
 * khóa chính KHÔNG phải field `id` chung chung — backend dùng convention
 * `{Entity}Id` nhất quán (đã thấy rõ ở `examSessionId`, `candidateAnswerId`,
 * `humanAssessmentId`...). Exam dùng `examId`.
 */

export type ExamStatus = "DRAFT" | "SCHEDULED" | "ONGOING" | "COMPLETED" | "CANCELLED";

/** Chuyển trạng thái hợp lệ theo state machine đã chốt ở PHASE0 mục E (Exam). */
export const EXAM_STATUS_TRANSITIONS: Record<ExamStatus, ExamStatus[]> = {
  DRAFT: ["SCHEDULED", "CANCELLED"],
  SCHEDULED: ["ONGOING", "DRAFT", "CANCELLED"],
  ONGOING: ["COMPLETED", "CANCELLED"],
  COMPLETED: [],
  CANCELLED: [],
};

export interface ExamTestAssignment {
  testId: string;
  testCode: string;
  testName: string;
  isActive: boolean;
}

export interface Exam {
  examId: string;
  examCode: string;
  examName: string;
  description: string | null;
  examDate: string; // ISO date (yyyy-MM-dd)
  startTime: string; // ISO datetime
  endTime: string; // ISO datetime
  status: ExamStatus;
  createdAt: string;
  updatedAt: string | null;
  /**
   * GIẢ ĐỊNH: response chi tiết `GET /exams/{id}` có kèm danh sách Test đã
   * gán (ExamTests) — hợp lý vì trang chi tiết cần hiển thị để gỡ/gán, và
   * README không show ví dụ JSON đầy đủ. Nếu backend thật KHÔNG trả field
   * này, `admin/exams/[examId]/page.tsx` cần gọi thêm 1 endpoint riêng (nếu
   * có) — chỉ cần sửa 1 chỗ trong `lib/admin/exams-api.ts`.
   */
  assignedTests?: ExamTestAssignment[];
}

export interface CreateExamPayload {
  examCode: string;
  examName: string;
  description?: string;
  examDate: string;
  startTime: string;
  endTime: string;
}

export type UpdateExamPayload = CreateExamPayload;
