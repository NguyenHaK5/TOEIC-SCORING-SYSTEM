/**
 * Field name khớp ví dụ `curl` thật trong README.md gốc: `testCode`,
 * `testName`, `version`, `listeningDurationSeconds`, `readingDurationSeconds`,
 * `writingDurationSeconds` — dùng đúng nguyên văn (đơn vị giây, không phải
 * phút như mô tả tiếng Việt "phút/giây" mập mờ ở PHASE0 mục E.2.4).
 *
 * ⚠️ ĐÃ SỬA sau khi chạy thật: khóa chính là `testId`, không phải `id`
 * chung chung — cùng lỗi/lý do với `Exam.examId` (xem types/exam.ts).
 */

export type TestStatus = "DRAFT" | "PUBLISHED" | "ARCHIVED";

export interface Test {
  testId: string;
  testCode: string;
  testName: string;
  version: string;
  description: string | null;
  listeningDurationSeconds: number;
  readingDurationSeconds: number;
  writingDurationSeconds: number;
  status: TestStatus;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateTestPayload {
  testCode: string;
  testName: string;
  version: string;
  description?: string;
  listeningDurationSeconds: number;
  readingDurationSeconds: number;
  writingDurationSeconds: number;
}

export type UpdateTestPayload = CreateTestPayload;

/**
 * README.md: "`Test.Publish()` validate toàn bộ câu hỏi trước khi cho
 * publish (liệt kê đầy đủ lỗi, không dừng ở lỗi đầu tiên)". GIẢ ĐỊNH shape
 * lỗi trả về khi publish thất bại — chưa thấy ví dụ JSON thật.
 */
export interface PublishValidationError {
  questionId?: string;
  questionNumber?: number;
  message: string;
}

export interface PublishTestResult {
  success: boolean;
  errors: PublishValidationError[];
}
