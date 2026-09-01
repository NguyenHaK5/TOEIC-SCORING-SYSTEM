import pandas as pd

from app.metrics import (
    compute_criterion_metrics,
    compute_overall_metrics,
    compute_overall_metrics_by_question_type,
)


def test_perfect_agreement_gives_zero_error_and_correlation_one():
    df = pd.DataFrame(
        {
            "human_overall_score": [80.0, 70.0, 90.0, 60.0],
            "ai_overall_score": [80.0, 70.0, 90.0, 60.0],
            "question_type": ["OPINION_ESSAY"] * 4,
        }
    )
    result = compute_overall_metrics(df, agreement_threshold=5.0)
    assert result["n"] == 4
    assert result["mae"] == 0.0
    assert result["rmse"] == 0.0
    assert result["pearsonR"] == 1.0
    assert result["spearmanR"] == 1.0
    assert result["agreementRate"] == 1.0
    assert result["notes"] == []


def test_known_mae_and_rmse_values():
    # Diff co dinh = 5 o ca 4 dong -> MAE = 5, RMSE = sqrt(mean(5^2)) = 5.
    df = pd.DataFrame(
        {
            "human_overall_score": [80.0, 70.0, 90.0, 60.0],
            "ai_overall_score": [85.0, 65.0, 95.0, 55.0],
            "question_type": ["OPINION_ESSAY"] * 4,
        }
    )
    result = compute_overall_metrics(df, agreement_threshold=5.0)
    assert result["mae"] == 5.0
    assert result["rmse"] == 5.0
    assert result["agreementRate"] == 1.0  # |diff|=5 <= threshold 5.0


def test_agreement_rate_threshold_boundary():
    df = pd.DataFrame(
        {
            "human_overall_score": [80.0, 70.0],
            "ai_overall_score": [86.0, 71.0],  # diff = 6 (fail) va 1 (pass) voi threshold=5
            "question_type": ["OPINION_ESSAY"] * 2,
        }
    )
    result = compute_overall_metrics(df, agreement_threshold=5.0)
    assert result["agreementRate"] == 0.5


def test_rows_missing_ai_score_are_dropped_before_computing():
    df = pd.DataFrame(
        {
            "human_overall_score": [80.0, 70.0, 90.0],
            "ai_overall_score": [80.0, None, 90.0],
            "question_type": ["OPINION_ESSAY"] * 3,
        }
    )
    result = compute_overall_metrics(df, agreement_threshold=5.0)
    assert result["n"] == 2
    assert result["mae"] == 0.0


def test_empty_dataset_returns_none_metrics_with_explanatory_note():
    df = pd.DataFrame(columns=["human_overall_score", "ai_overall_score", "question_type"])
    result = compute_overall_metrics(df, agreement_threshold=5.0)
    assert result["n"] == 0
    assert result["mae"] is None
    assert result["pearsonR"] is None
    assert result["agreementRate"] is None
    assert len(result["notes"]) == 1


def test_single_pair_has_error_but_no_correlation():
    df = pd.DataFrame(
        {
            "human_overall_score": [80.0],
            "ai_overall_score": [82.0],
            "question_type": ["OPINION_ESSAY"],
        }
    )
    result = compute_overall_metrics(df, agreement_threshold=5.0)
    assert result["n"] == 1
    assert result["mae"] == 2.0
    assert result["pearsonR"] is None
    assert result["spearmanR"] is None
    assert "n < 2" in result["notes"][0]


def test_zero_variance_side_disables_correlation_but_keeps_error_metrics():
    # Human cham deu 80 diem ca 3 bai (khong bien thien) -> Pearson/Spearman undefined,
    # nhung MAE/RMSE/Agreement Rate van tinh duoc binh thuong.
    df = pd.DataFrame(
        {
            "human_overall_score": [80.0, 80.0, 80.0],
            "ai_overall_score": [75.0, 78.0, 82.0],
            "question_type": ["OPINION_ESSAY"] * 3,
        }
    )
    result = compute_overall_metrics(df, agreement_threshold=5.0)
    assert result["pearsonR"] is None
    assert result["spearmanR"] is None
    assert result["mae"] is not None
    assert any("variance" in note for note in result["notes"])


def test_criterion_metrics_are_computed_independently_per_criterion():
    df = pd.DataFrame(
        {
            "candidate_answer_id": ["a", "a", "b", "b"],
            "question_type": ["OPINION_ESSAY"] * 4,
            "rater_id": ["r1"] * 4,
            "criterion": ["Grammar", "Vocabulary", "Grammar", "Vocabulary"],
            "human_score": [8.0, 7.0, 9.0, 6.0],
            "ai_score": [8.0, 6.0, 9.0, 6.0],
            "difference": [0.0, 1.0, 0.0, 0.0],
        }
    )
    result = compute_criterion_metrics(df, agreement_threshold=1.0)
    assert set(result.keys()) == {"Grammar", "Vocabulary"}
    assert result["Grammar"]["mae"] == 0.0
    assert result["Grammar"]["n"] == 2
    assert result["Vocabulary"]["mae"] == 0.5
    assert result["Vocabulary"]["n"] == 2


def test_criterion_metrics_on_empty_dataframe_returns_empty_dict():
    df = pd.DataFrame(columns=["candidate_answer_id", "criterion", "human_score", "ai_score"])
    result = compute_criterion_metrics(df, agreement_threshold=5.0)
    assert result == {}


def test_metrics_by_question_type_are_split_correctly():
    df = pd.DataFrame(
        {
            "human_overall_score": [80.0, 70.0, 60.0, 65.0],
            "ai_overall_score": [80.0, 70.0, 55.0, 70.0],
            "question_type": [
                "PICTURE_SENTENCE",
                "PICTURE_SENTENCE",
                "OPINION_ESSAY",
                "OPINION_ESSAY",
            ],
        }
    )
    result = compute_overall_metrics_by_question_type(df, agreement_threshold=5.0)
    assert set(result.keys()) == {"PICTURE_SENTENCE", "OPINION_ESSAY"}
    assert result["PICTURE_SENTENCE"]["mae"] == 0.0
    assert result["OPINION_ESSAY"]["mae"] == 5.0
