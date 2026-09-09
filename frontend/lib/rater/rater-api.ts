import { apiClient } from "@/lib/api-client";
import { buildQuery } from "@/lib/admin/query";
import type { PagedResult } from "@/types/pagination";
import type {
  RaterFilterStatus,
  SubmitHumanScorePayload,
  WritingAnswerDetail,
  WritingAnswerListItem,
} from "@/types/rater";
import type { AiHumanComparisonItem } from "@/types/rater-comparison";
import type { QuestionType } from "@/types/question";

export const raterApi = {
  list: (
    params: {
      status?: RaterFilterStatus;
      questionType?: QuestionType;
      page?: number;
      pageSize?: number;
    } = {}
  ) => apiClient.get<PagedResult<WritingAnswerListItem>>(`/rater/writing-answers${buildQuery(params)}`),

  get: (candidateAnswerId: string) =>
    apiClient.get<WritingAnswerDetail>(`/rater/writing-answers/${candidateAnswerId}`),

  submitScore: (candidateAnswerId: string, payload: SubmitHumanScorePayload) =>
    apiClient.post<WritingAnswerDetail>(`/rater/writing-answers/${candidateAnswerId}/score`, payload),

  comparison: (
    params: { questionType?: QuestionType; page?: number; pageSize?: number } = {}
  ) =>
    apiClient.get<PagedResult<AiHumanComparisonItem>>(
      `/rater/writing-answers/comparison${buildQuery(params)}`
    ),
};
