/**
 * GIẢ ĐỊNH — xem PHASE7-ISSUE-17 trong PHASE7_PART6_INTEGRATION_GUIDE.md.
 * `phase-tracker.md` Phase 6 Part 3 mô tả: "Tổng quan hệ thống + summary
 * stats điểm từng phần" và "Top lỗi Writing phổ biến nhất". Không có ví dụ
 * JSON. PHASE6-ISSUE-07 (đã ghi trong tracker): không dùng histogram điểm
 * cố định vì chưa có TOEIC scaled score (5-495) — dùng summary stats
 * (Min/Max/Average/Median) thay thế.
 */

export interface ScoreSummaryStats {
  sampleSize: number; // SỬA TỪ count -> sampleSize
  min: number | null;
  max: number | null;
  average: number | null;
  median: number | null;
}

export interface AdminDashboardOverview {
  totalExams: number;
  totalCandidates: number;
  totalExamSessions: number; // SỬA TỪ totalSubmittedSessions -> totalExamSessions
  submittedSessionsCount: number; // THÊM TRƯỜNG NÀY
  inProgressSessionsCount: number; // THÊM TRƯỜNG NÀY
  listeningScoreSummary: ScoreSummaryStats; // SỬA TỪ listening
  readingScoreSummary: ScoreSummaryStats; // SỬA TỪ reading
  writingScoreSummary: ScoreSummaryStats; // SỬA TỪ writing
}

/**
 * ✅ ĐÃ XÁC NHẬN bằng JSON response thật (06/09/2026) — không còn là giả
 * định. Field thật: `topErrors` (không phải `items`), mỗi lỗi có kèm
 * `questionType`/`criterion` (hiện tại luôn `"UNSPECIFIED"` cho criterion —
 * đúng PHASE6-ISSUE-06, parser chưa map được criterion cụ thể từ
 * `ErrorsJson`), `totalAssessmentsAnalyzed`, và
 * `unparseableErrorsJsonCount` (không phải `unparseableCount`).
 */
export interface WritingErrorItem {
  questionType: string;
  criterion: string;
  errorType: string;
  count: number;
}

export interface WritingErrorAnalytics {
  topErrors: WritingErrorItem[];
  totalAssessmentsAnalyzed: number;
  unparseableErrorsJsonCount: number;
}
