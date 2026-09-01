from __future__ import annotations

import pandas as pd

from .schemas import AiHumanComparisonDto

OVERALL_COLUMNS = [
    "candidate_answer_id",
    "question_id",
    "question_type",
    "rater_id",
    "rater_name",
    "is_final_human",
    "human_overall_score",
    "ai_overall_score",
    "overall_difference",
]

CRITERION_COLUMNS = [
    "candidate_answer_id",
    "question_type",
    "rater_id",
    "criterion",
    "human_score",
    "ai_score",
    "difference",
]


def build_overall_dataframe(items: list[AiHumanComparisonDto]) -> pd.DataFrame:
    """1 dong = 1 cap (HumanAssessment, AI hien hanh) cho 1 CandidateAnswer - dung cho
    metrics o muc OverallScore (MAE/RMSE/Correlation/Agreement tren diem tong)."""
    rows = [
        {
            "candidate_answer_id": str(it.candidate_answer_id),
            "question_id": str(it.question_id),
            "question_type": it.question_type,
            "rater_id": str(it.rater_id),
            "rater_name": it.rater_name,
            "is_final_human": it.is_final_human,
            "human_overall_score": it.human_overall_score,
            "ai_overall_score": it.ai_overall_score,
            "overall_difference": it.overall_difference,
        }
        for it in items
    ]
    if not rows:
        # Giu dung schema cot du dataset rong, de code downstream (metrics, Part 2) khong
        # phai kiem tra rong o nhieu noi.
        return pd.DataFrame(columns=OVERALL_COLUMNS)
    return pd.DataFrame(rows, columns=OVERALL_COLUMNS)


def build_criterion_dataframe(items: list[AiHumanComparisonDto]) -> pd.DataFrame:
    """Dang long: 1 dong = 1 (CandidateAnswer, Criterion) - dung cho metrics theo tung
    tieu chi Rubric (Grammar, Vocabulary, ImageRelevance, ...)."""
    rows = []
    for it in items:
        for c in it.criterion_comparisons:
            rows.append(
                {
                    "candidate_answer_id": str(it.candidate_answer_id),
                    "question_type": it.question_type,
                    "rater_id": str(it.rater_id),
                    "criterion": c.criterion,
                    "human_score": c.human_score,
                    "ai_score": c.ai_score,
                    "difference": c.difference,
                }
            )
    if not rows:
        return pd.DataFrame(columns=CRITERION_COLUMNS)
    return pd.DataFrame(rows, columns=CRITERION_COLUMNS)
