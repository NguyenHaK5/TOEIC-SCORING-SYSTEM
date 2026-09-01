from uuid import uuid4

import pandas as pd

from app.report import build_deviation_cases, dataframe_to_csv_bytes
from app.schemas import AiHumanComparisonDto, CriterionComparisonDto


def _make_item(human_score, ai_score, criteria=None, question_type="OPINION_ESSAY"):
    return AiHumanComparisonDto(
        candidateAnswerId=uuid4(),
        questionId=uuid4(),
        questionType=question_type,
        humanAssessmentId=uuid4(),
        raterId=uuid4(),
        raterName="Rater A",
        isFinalHuman=True,
        humanOverallScore=human_score,
        writingAssessmentId=uuid4(),
        aiOverallScore=ai_score,
        overallDifference=human_score - ai_score,
        criterionComparisons=criteria or [],
    )


def test_sorts_cases_by_absolute_difference_descending():
    items = [
        _make_item(80.0, 78.0),  # diff = 2
        _make_item(60.0, 90.0),  # diff = -30 -> |diff| = 30
        _make_item(70.0, 65.0),  # diff = 5
    ]
    cases = build_deviation_cases(items, min_difference=0.0, top_n=10)
    assert [c["overallDifference"] for c in cases] == [-30.0, 5.0, 2.0]


def test_filters_out_cases_below_min_difference():
    items = [_make_item(80.0, 78.0), _make_item(60.0, 90.0)]
    cases = build_deviation_cases(items, min_difference=10.0, top_n=10)
    assert len(cases) == 1
    assert cases[0]["overallDifference"] == -30.0


def test_respects_top_n_limit():
    items = [_make_item(80.0, 80.0 - i) for i in range(1, 6)]
    cases = build_deviation_cases(items, min_difference=0.0, top_n=2)
    assert len(cases) == 2
    # 2 case lech nhieu nhat phai la diff=5 va diff=4 (i=5,4)
    assert [c["overallDifference"] for c in cases] == [5.0, 4.0]


def test_excludes_items_with_no_overall_difference():
    item_without_diff = _make_item(80.0, 78.0)
    item_without_diff.overall_difference = None
    items = [item_without_diff, _make_item(60.0, 90.0)]
    cases = build_deviation_cases(items, min_difference=0.0, top_n=10)
    assert len(cases) == 1
    assert cases[0]["overallDifference"] == -30.0


def test_criterion_breakdown_sorted_by_absolute_difference_desc():
    criteria = [
        CriterionComparisonDto(criterion="Grammar", humanScore=8.0, aiScore=7.5, difference=0.5),
        CriterionComparisonDto(criterion="Vocabulary", humanScore=8.0, aiScore=4.0, difference=4.0),
    ]
    items = [_make_item(80.0, 70.0, criteria=criteria)]
    cases = build_deviation_cases(items, min_difference=0.0, top_n=10)
    breakdown = cases[0]["criterionBreakdown"]
    assert breakdown[0]["criterion"] == "Vocabulary"
    assert breakdown[1]["criterion"] == "Grammar"


def test_criterion_breakdown_empty_when_no_criterion_comparisons():
    items = [_make_item(80.0, 70.0, criteria=[])]
    cases = build_deviation_cases(items, min_difference=0.0, top_n=10)
    assert cases[0]["criterionBreakdown"] == []


def test_empty_items_returns_empty_case_list():
    assert build_deviation_cases([], min_difference=0.0, top_n=10) == []


def test_dataframe_to_csv_bytes_has_header_and_utf8_sig_bom():
    df = pd.DataFrame({"a": [1, 2], "b": ["x", "y"]})
    csv_bytes = dataframe_to_csv_bytes(df)
    # utf-8-sig bat dau bang BOM \xef\xbb\xbf
    assert csv_bytes.startswith(b"\xef\xbb\xbf")
    text = csv_bytes.decode("utf-8-sig")
    lines = text.splitlines()
    assert lines[0] == "a,b"
    assert "1,x" in lines[1]


def test_dataframe_to_csv_bytes_preserves_vietnamese_diacritics():
    df = pd.DataFrame({"raterName": ["Nguyễn Văn A"]})
    csv_bytes = dataframe_to_csv_bytes(df)
    text = csv_bytes.decode("utf-8-sig")
    assert "Nguyễn Văn A" in text
