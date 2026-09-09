/**
 * GIẢ ĐỊNH — xem PHASE7-ISSUE-15. Phase 4 Part 2 mô tả: "dataset cặp (AI,
 * Human) theo Criterion, endpoint `GET /rater/writing-answers/comparison`,
 * sắp xếp theo `|OverallDifference|` giảm dần".
 */
export interface CriterionDifference {
  criterionCode: string;
  aiScore: number;
  humanScore: number;
  difference: number;
}

export interface AiHumanComparisonItem {
  candidateAnswerId: string;
  candidateName: string;
  questionNumber: number;
  questionType: import("./question").QuestionType;
  aiScore: number;
  humanScore: number;
  overallDifference: number;
  criterionDifferences: CriterionDifference[];
}
