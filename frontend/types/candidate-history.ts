/**
 * GIẢ ĐỊNH — xem PHASE7-ISSUE-14. `phase-tracker.md` Phase 6 Part 1 chỉ mô
 * tả: "Danh sách phiên thi đã nộp, phân trang, kèm 3 điểm số". Field name
 * suy ra từ `ExamSessions`/`Exams` schema đã chốt (PHASE0 mục E.2.3/E.2.8).
 * `writingScore` giả định `null` khi Writing chưa chấm xong (self-healing
 * background job vẫn đang chạy).
 */
export interface ExamHistoryItem {
  examSessionId: string;
  examId: string;
  examCode: string;
  examName: string;
  submittedAt: string;
  listeningScore: number | null;
  readingScore: number | null;
  writingScore: number | null;
}
