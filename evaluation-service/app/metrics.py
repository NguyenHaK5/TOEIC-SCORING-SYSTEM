from __future__ import annotations

import numpy as np
import pandas as pd
from scipy import stats
from sklearn.metrics import mean_absolute_error, mean_squared_error


def _pairwise_metrics(human: pd.Series, ai: pd.Series, agreement_threshold: float) -> dict:
    """
    Tinh MAE/RMSE/Pearson/Spearman/Agreement Rate cho 1 cap Series (Human, AI) da align
    va da loai bo NaN o tang goi ham nay.

    Khong nem exception khi khong du du lieu de tinh mot thong ke nao do (n qua nho,
    hoac phuong sai = 0 lam Pearson/Spearman vo nghia ve mat toan hoc) - tra ve None cho
    dung thong ke do kem 'notes' giai thich ly do, vi day la tinh huong du lieu thuc te
    hoan toan co the xay ra (dac biet giai doan dau du an khi chua co nhieu bai duoc ca
    AI va Human cham) chu khong phai loi code.
    """
    n = len(human)
    result: dict = {
        "n": n,
        "mae": None,
        "rmse": None,
        "pearsonR": None,
        "pearsonPValue": None,
        "spearmanR": None,
        "spearmanPValue": None,
        "agreementRate": None,
        "agreementThreshold": agreement_threshold,
        "notes": [],
    }

    if n == 0:
        result["notes"].append("Khong co du lieu (n=0).")
        return result

    diff = (human - ai).abs()
    result["mae"] = float(mean_absolute_error(human, ai))
    result["rmse"] = float(np.sqrt(mean_squared_error(human, ai)))
    result["agreementRate"] = float((diff <= agreement_threshold).mean())

    if n < 2:
        result["notes"].append(
            "n < 2: khong du du lieu de tinh Pearson/Spearman correlation."
        )
        return result

    if human.nunique() < 2 or ai.nunique() < 2:
        result["notes"].append(
            "Human hoac AI score khong co bien thien (variance = 0) trong tap nay: "
            "khong tinh duoc Pearson/Spearman correlation (undefined ve mat toan hoc)."
        )
        return result

    pearson_r, pearson_p = stats.pearsonr(human, ai)
    spearman_r, spearman_p = stats.spearmanr(human, ai)
    result["pearsonR"] = float(pearson_r)
    result["pearsonPValue"] = float(pearson_p)
    result["spearmanR"] = float(spearman_r)
    result["spearmanPValue"] = float(spearman_p)
    return result


def compute_overall_metrics(overall_df: pd.DataFrame, agreement_threshold: float) -> dict:
    """Metrics tren OverallScore - 1 dong = 1 CandidateAnswer (dataset tu overall_df,
    Part 1 dataset_loader)."""
    valid = overall_df.dropna(subset=["human_overall_score", "ai_overall_score"])
    return _pairwise_metrics(
        valid["human_overall_score"], valid["ai_overall_score"], agreement_threshold
    )


def compute_metrics_by_group(
    df: pd.DataFrame,
    group_col: str,
    human_col: str,
    ai_col: str,
    agreement_threshold: float,
) -> dict[str, dict]:
    """Metrics tinh rieng cho tung gia tri cua group_col (vd Criterion, QuestionType)."""
    result: dict[str, dict] = {}
    if df.empty:
        return result
    valid = df.dropna(subset=[human_col, ai_col])
    for group_value, group_df in valid.groupby(group_col):
        result[str(group_value)] = _pairwise_metrics(
            group_df[human_col], group_df[ai_col], agreement_threshold
        )
    return result


def compute_overall_metrics_by_question_type(
    overall_df: pd.DataFrame, agreement_threshold: float
) -> dict[str, dict]:
    return compute_metrics_by_group(
        overall_df,
        "question_type",
        "human_overall_score",
        "ai_overall_score",
        agreement_threshold,
    )


def compute_criterion_metrics(criterion_df: pd.DataFrame, agreement_threshold: float) -> dict[str, dict]:
    return compute_metrics_by_group(
        criterion_df, "criterion", "human_score", "ai_score", agreement_threshold
    )
