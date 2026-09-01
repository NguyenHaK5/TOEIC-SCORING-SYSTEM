from __future__ import annotations

from contextlib import asynccontextmanager
from typing import Optional

from fastapi import FastAPI, HTTPException, Query
from fastapi.responses import StreamingResponse

from .backend_client import BackendApiError, BackendAuthError, BackendClient
from .config import get_settings
from .dataset_loader import build_criterion_dataframe, build_overall_dataframe
from .metrics import (
    compute_criterion_metrics,
    compute_overall_metrics,
    compute_overall_metrics_by_question_type,
)
from .report import build_deviation_cases, dataframe_to_csv_bytes

settings = get_settings()
_backend_client: Optional[BackendClient] = None

# Nguong "dong y" mac dinh cho Agreement Rate: |HumanScore - AIScore| <= threshold duoc
# tinh la 1 cap "dong y". Prompt goc (muc 5) yeu cau Agreement Rate nhung khong dinh nghia
# nguong cu the - 5.0 diem tren thang OverallScore mac dinh [0,100] cua WritingScoringEngine
# (Phase 3 Part 2, targetMaxScore mac dinh = 100) la gia tri khoi diem hop ly, co the doi
# qua query param ?agreementThreshold= moi lan goi ma khong can sua code/redeploy.
DEFAULT_AGREEMENT_THRESHOLD = 5.0

# So case toi da tra ve mac dinh / toi da cho phep o GET /evaluations/report - tranh
# response qua lon neu Dragon quen gioi han topN.
DEFAULT_REPORT_TOP_N = 20
MAX_REPORT_TOP_N = 500


@asynccontextmanager
async def lifespan(app: FastAPI):
    global _backend_client
    _backend_client = BackendClient(settings)
    yield
    await _backend_client.aclose()


app = FastAPI(
    title="TOEIC Evaluation Service",
    description=(
        "Phase 5 - danh gia do tin cay AI Writing Assessment (MAE/RMSE/Correlation/"
        "Agreement Rate) so voi Human Rater. Doc du lieu tu Toeic.Api qua "
        "GET /rater/writing-answers/comparison, khong tu query PostgreSQL truc tiep "
        "(xem PHASE5-ISSUE-01 trong integration guide)."
    ),
    version="0.1.0-part1",
    lifespan=lifespan,
)


def _get_client() -> BackendClient:
    if _backend_client is None:
        raise HTTPException(status_code=503, detail="Backend client chua san sang.")
    return _backend_client


@app.get("/api/v1/health")
async def health() -> dict:
    return {"status": "ok", "backendBaseUrl": settings.backend_base_url}


@app.get("/api/v1/evaluations/dataset")
async def get_dataset(
    question_type: Optional[str] = Query(default=None, alias="questionType"),
    only_with_both_scores: bool = Query(default=True, alias="onlyWithBothScores"),
):
    """
    Part 1 demo endpoint: keo toan bo dataset AI-vs-Human tu Toeic.Api, dung 2
    DataFrame (overall + per-criterion), tra ve tom tat de xac nhan pipeline lay du
    lieu hoat dong dung truoc khi tinh metrics that o Part 2.
    """
    client = _get_client()
    try:
        items = await client.fetch_all_comparisons(
            question_type=question_type, only_with_both_scores=only_with_both_scores
        )
    except BackendAuthError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc
    except BackendApiError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc

    overall_df = build_overall_dataframe(items)
    criterion_df = build_criterion_dataframe(items)

    return {
        "totalPairs": len(overall_df),
        "totalCriterionRows": len(criterion_df),
        "byQuestionType": (
            overall_df["question_type"].value_counts().to_dict() if not overall_df.empty else {}
        ),
        "byCriterion": (
            criterion_df["criterion"].value_counts().to_dict() if not criterion_df.empty else {}
        ),
        "sample": overall_df.head(5).to_dict(orient="records"),
    }


@app.get("/api/v1/evaluations/metrics")
async def get_metrics(
    question_type: Optional[str] = Query(default=None, alias="questionType"),
    agreement_threshold: float = Query(
        default=DEFAULT_AGREEMENT_THRESHOLD, alias="agreementThreshold", gt=0
    ),
):
    """
    Part 2: MAE, RMSE, Pearson/Spearman Correlation, Agreement Rate - tinh o 3 muc:
    - overall: tren OverallScore, toan bo dataset (sau khi loc questionType neu co)
    - byQuestionType: tach rieng PICTURE_SENTENCE / WRITTEN_RESPONSE / OPINION_ESSAY
      (moi loai cau co bo Criterion/trong so khac nhau - gop chung se lam nhieu tin hieu)
    - byCriterion: tach rieng tung tieu chi Rubric (Grammar, Vocabulary, ImageRelevance,
      ...) - day la du lieu phuc vu truc tiep muc tieu nghien cuu chinh cua do an (Phase 0
      ISSUE-03: so sanh AI vs Human theo tung tieu chi).

    Luon goi backend voi onlyWithBothScores=true (metrics khong co y nghia voi cap chi co
    1 ben AI hoac Human).
    """
    client = _get_client()
    try:
        items = await client.fetch_all_comparisons(
            question_type=question_type, only_with_both_scores=True
        )
    except BackendAuthError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc
    except BackendApiError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc

    overall_df = build_overall_dataframe(items)
    criterion_df = build_criterion_dataframe(items)

    return {
        "datasetInfo": {
            "totalPairs": len(overall_df),
            "totalCriterionRows": len(criterion_df),
        },
        "agreementThreshold": agreement_threshold,
        "overall": compute_overall_metrics(overall_df, agreement_threshold),
        "byQuestionType": compute_overall_metrics_by_question_type(overall_df, agreement_threshold),
        "byCriterion": compute_criterion_metrics(criterion_df, agreement_threshold),
    }


@app.get("/api/v1/evaluations/report")
async def get_report(
    question_type: Optional[str] = Query(default=None, alias="questionType"),
    min_difference: float = Query(default=0.0, alias="minDifference", ge=0),
    top_n: int = Query(default=DEFAULT_REPORT_TOP_N, alias="topN", ge=1, le=MAX_REPORT_TOP_N),
):
    """
    Part 3: liet ke top N CandidateAnswer ma AI lech Human nhieu nhat (theo
    |OverallDifference|), kem breakdown lech theo tung Criterion trong cung cau tra loi -
    phuc vu truc tiep muc 5 prompt goc ("phan tich nhung truong hop AI cham lech nhieu").

    minDifference: chi lay cac cap co |OverallDifference| >= gia tri nay (mac dinh 0 -
    lay tat ca, sap theo lech giam dan). topN: gioi han so dong tra ve (mac dinh 20, toi
    da 500 - tranh response qua lon).
    """
    client = _get_client()
    try:
        items = await client.fetch_all_comparisons(
            question_type=question_type, only_with_both_scores=True
        )
    except BackendAuthError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc
    except BackendApiError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc

    total_matching = sum(
        1
        for it in items
        if it.overall_difference is not None and abs(it.overall_difference) >= min_difference
    )
    cases = build_deviation_cases(items, min_difference=min_difference, top_n=top_n)

    return {
        "filters": {
            "questionType": question_type,
            "minDifference": min_difference,
            "topN": top_n,
        },
        "totalPairs": len(items),
        "totalMatchingCases": total_matching,
        "cases": cases,
    }


@app.get("/api/v1/evaluations/report/export")
async def export_report(
    format: str = Query(default="csv", pattern="^(csv|json)$"),
    level: str = Query(default="overall", pattern="^(overall|criterion)$"),
    question_type: Optional[str] = Query(default=None, alias="questionType"),
):
    """
    Part 3: xuat TOAN BO dataset AI-vs-Human (khong gioi han topN, khac /report o tren) de
    Dragon phan tich offline bang Excel/pandas rieng, khong phu thuoc Evaluation Service
    dang chay.

    level=overall: 1 dong / CandidateAnswer (OverallScore). level=criterion: dang long,
    1 dong / (CandidateAnswer, Criterion) - dung khi can phan tich rieng tung tieu chi
    Rubric (Grammar, Vocabulary, ImageRelevance, ...) trong Excel/pandas.
    format=csv: file tai ve truc tiep (UTF-8 BOM, Excel doc dung tieng Viet co dau).
    format=json: tra ve JSON array, tien cho xu ly lai bang code khac.
    """
    client = _get_client()
    try:
        items = await client.fetch_all_comparisons(
            question_type=question_type, only_with_both_scores=True
        )
    except BackendAuthError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc
    except BackendApiError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc

    df = build_overall_dataframe(items) if level == "overall" else build_criterion_dataframe(items)

    if format == "json":
        return df.to_dict(orient="records")

    csv_bytes = dataframe_to_csv_bytes(df)
    filename = f"toeic_ai_human_{level}.csv"
    return StreamingResponse(
        iter([csv_bytes]),
        media_type="text/csv",
        headers={"Content-Disposition": f'attachment; filename="{filename}"'},
    )
