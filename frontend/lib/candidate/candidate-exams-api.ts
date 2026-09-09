import { apiClient } from "@/lib/api-client";
import type { CandidateExamSummary } from "@/types/candidate-exam";

/** Xem PHASE7-ISSUE-10 (PHASE7_PART3_INTEGRATION_GUIDE.md) — path giả định. */
export const candidateExamsApi = {
  listAvailable: () => apiClient.get<CandidateExamSummary[]>(`/exams/available`),
};
