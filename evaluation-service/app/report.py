from __future__ import annotations

import io

import pandas as pd

from .schemas import AiHumanComparisonDto


def build_deviation_cases(
    items: list[AiHumanComparisonDto],
    min_difference: float = 0.0,
    top_n: int = 20,
) -> list[dict]:
    """
    Liet ke cac CandidateAnswer ma AI lech Human nhieu nhat (theo |OverallDifference|),
    kem breakdown lech theo tung Criterion trong cung cau tra loi (sap xep giam dan theo
    |difference| de tieu chi lech nhieu nhat hien len truoc) - phuc vu truc tiep muc 5
    prompt goc: "phan tich nhung truong hop AI cham lech nhieu".

    Dung truc tiep list DTO (khong qua pandas DataFrame) vi can giu nguyen
    criterion_comparisons dang long theo tung item, khong can groupby.
    """
    candidates = [
        it
        for it in items
        if it.overall_difference is not None and abs(it.overall_difference) >= min_difference
    ]
    candidates.sort(key=lambda it: abs(it.overall_difference), reverse=True)
    top = candidates[:top_n]

    cases: list[dict] = []
    for it in top:
        criterion_breakdown = sorted(
            (
                {
                    "criterion": c.criterion,
                    "humanScore": c.human_score,
                    "aiScore": c.ai_score,
                    "difference": c.difference,
                }
                for c in it.criterion_comparisons
                if c.difference is not None
            ),
            key=lambda c: abs(c["difference"]),
            reverse=True,
        )
        cases.append(
            {
                "candidateAnswerId": str(it.candidate_answer_id),
                "questionId": str(it.question_id),
                "questionType": it.question_type,
                "raterId": str(it.rater_id),
                "raterName": it.rater_name,
                "humanOverallScore": it.human_overall_score,
                "aiOverallScore": it.ai_overall_score,
                "overallDifference": it.overall_difference,
                "criterionBreakdown": criterion_breakdown,
            }
        )
    return cases


def dataframe_to_csv_bytes(df: pd.DataFrame) -> bytes:
    """UTF-8 with BOM (utf-8-sig) de Excel mo file tren Windows hien thi dung tieng Viet
    co dau (vd raterName) - CSV thuan UTF-8 khong BOM thuong bi Excel doc sai encoding."""
    buffer = io.StringIO()
    df.to_csv(buffer, index=False)
    return buffer.getvalue().encode("utf-8-sig")
