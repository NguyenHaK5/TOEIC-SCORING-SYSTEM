/**
 * Field name của `ExamSessionSummary` khớp trực tiếp ví dụ `curl` thật
 * trong README.md gốc (mục "Part 4 — ExamSession + CandidateAnswer"):
 * `examSessionId`, `expiresAt`, `remainingSeconds`.
 *
 * README xác nhận: KHÔNG có trạng thái trung gian `NOT_STARTED` — session
 * bắt đầu ngay ở `IN_PROGRESS`. `ExpiresAt` chốt đúng 1 lần khi tạo, không
 * bao giờ tính lại — nghĩa là chỉ có DUY NHẤT 1 đồng hồ đếm ngược cho toàn
 * bộ phiên thi (Listening+Reading+Writing gộp chung), không tách timer
 * riêng từng phần.
 */
export type ExamSessionStatus = "IN_PROGRESS" | "SUBMITTED" | "ABSENT" | "CANCELLED";

export interface ExamSessionSummary {
  examSessionId: string;
  examId: string;
  testId?: string;
  status: ExamSessionStatus;
  startedAt: string;
  expiresAt: string;
  submittedAt?: string | null;
  /** Chỉ có trong response `POST /exam-sessions` lúc bắt đầu/resume — xem README. */
  remainingSeconds?: number;
}

/**
 * GIẢ ĐỊNH (Medium) — README không show JSON đầy đủ của
 * `GET /exam-sessions/{id}/questions`, chỉ mô tả bằng lời: "Câu hỏi (ẩn
 * Answer Key) + câu trả lời đã lưu". `CandidateVisibleOptionDto` (nhắc tên
 * trong README) được giả định KHÔNG có field `isCorrect` — đúng theo thiết
 * kế đã chốt. Câu trả lời đã lưu được giả định nằm ngay trên từng Question
 * (`selectedOptionId`/`answerText`) thay vì một mảng answers tách rời.
 */
export interface CandidateVisibleOption {
  questionOptionId: string;
  optionLabel: string;
  optionText: string;
}

export interface ExamSessionQuestion {
  questionId: string;
  section: import("./question").QuestionSection;
  questionType: import("./question").QuestionType;
  questionNumber: number;
  questionText: string;
  imageUrl: string | null;
  audioUrl: string | null;
  requiredWords: string | null;
  options: CandidateVisibleOption[];
  /** Câu trả lời đã lưu trước đó (nếu có) — phục hồi khi reload trang. */
  selectedOptionId: string | null;
  answerText: string | null;
}

/**
 * GIẢ ĐỊNH: body `POST /exam-sessions/{id}/answers` cho câu trắc nghiệm đã
 * có bằng chứng trực tiếp từ README (`{"questionId":"...","selectedOptionId":"..."}`).
 * Với câu Writing (`answerText`), suy ra tương tự — cùng 1 endpoint, chỉ
 * khác field nào được gửi.
 */
export interface SaveAnswerPayload {
  questionId: string;
  selectedOptionId?: string;
  answerText?: string;
}

export interface StartExamSessionPayload {
  examId: string;
}
