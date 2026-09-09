/**
 * Metrics: ✅ ĐÃ XÁC NHẬN bằng JSON response thật (06/09/2026). Dataset:
 * suy ra từ `datasetInfo` lồng bên trong response Metrics (cùng field
 * `totalPairs`/`totalCriterionRows`) — bản thân endpoint `GET /dataset`
 * độc lập CHƯA có ví dụ riêng, nhưng nhiều khả năng cùng shape. Report:
 * vẫn CHƯA có ví dụ JSON — xem PHASE7-ISSUE-17, còn là giả định.
 */

export interface DatasetSummary {
  totalPairs: number;
  totalCriterionRows: number;
}

/**
 * `null` cho mae/rmse/pearsonR/spearmanR/agreementRate nghĩa là không đủ
 * dữ liệu để tính (n=0, n=1, hoặc phương sai=0 khiến correlation
 * undefined) — Phase 5 Part 2 cố ý trả `null` kèm `notes` (mảng chuỗi,
 * KHÔNG phải 1 chuỗi đơn) giải thích thay vì suy diễn "tương quan = 0",
 * Frontend PHẢI hiển thị "Không đủ dữ liệu" thay vì số 0 trong trường
 * hợp này.
 */
export interface MetricSet {
  n: number;
  mae: number | null;
  rmse: number | null;
  pearsonR: number | null;
  pearsonPValue: number | null;
  spearmanR: number | null;
  spearmanPValue: number | null;
  agreementRate: number | null;
  agreementThreshold: number;
  notes: string[] | null;
}

export interface MetricsResult {
  datasetInfo: DatasetSummary;
  agreementThreshold: number;
  overall: MetricSet;
  byQuestionType: Record<string, MetricSet>;
  byCriterion: Record<string, MetricSet>;
}

/** ⚠️ GIẢ ĐỊNH — chưa có ví dụ JSON thật cho endpoint Report/Export. */
export interface ReportCriterionDifference {
  criterion: string;
  aiScore: number;
  humanScore: number;
  difference: number;
}

export interface ReportCase {
  candidateAnswerId: string;
  questionId: string; // Thêm mới
  questionType: string;
  raterId: string; // Thêm mới
  raterName: string; // Sửa từ candidateName
  questionNumber: number;
  humanOverallScore: number; // Sửa từ humanScore
  aiOverallScore: number; // Sửa từ aiScore
  overallDifference: number;
  criterionBreakdown: ReportCriterionDifference[]; // Sửa từ criterionDifferences
}

export interface ReportResult {
  filters?: {
    questionType: string | null;
    minDifference: number;
    topN: number;
  };
  totalPairs: number;
  totalMatchingCases: number; // Sửa từ totalMatching
  cases: ReportCase[]; // Sửa từ items
}
