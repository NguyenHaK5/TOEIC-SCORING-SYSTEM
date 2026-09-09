/**
 * ✅ Tên field top-level ĐÃ XÁC NHẬN bằng JSON response thật (06/09/2026):
 * `objectiveWeakness` (không phải `listeningReadingAccuracy`),
 * `writingWeakness` (không phải `writingCriteriaAverages`) với field con
 * `sampleSize` (không phải `sampleCount`). `scoreTrend` khớp đúng giả định
 * ban đầu.
 *
 * ⚠️ Vẫn còn giả định: mảng `objectiveWeakness` trong ví dụ thật đang RỖNG
 * (chưa có Listening/Reading nào được chấm ở dữ liệu mẫu), nên shape từng
 * phần tử bên trong (`QuestionTypeAccuracy`) CHƯA được xác nhận — giữ
 * nguyên giả định cũ, cần Dragon gửi thêm ví dụ khi có dữ liệu Listening/
 * Reading thật. PHASE6-ISSUE-05 (đã ghi trong tracker) vẫn áp dụng.
 */

export interface QuestionTypeAccuracy {
  section: import("./question").QuestionSection;
  questionType: string;
  correctCount: number;
  totalCount: number;
  /** 0–1 */
  accuracy: number;
}

export interface WritingCriterionAverage {
  criterionCode: string;
  averageScore: number;
  sampleSize: number;
}

export interface ScoreTrendPoint {
  examSessionId: string;
  submittedAt: string;
  listeningScore: number | null;
  readingScore: number | null;
  writingScore: number | null;
}

export interface CandidateAnalytics {
  objectiveWeakness: QuestionTypeAccuracy[];
  writingWeakness: WritingCriterionAverage[];
  scoreTrend: ScoreTrendPoint[];
}
