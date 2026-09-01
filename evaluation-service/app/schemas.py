from __future__ import annotations

from typing import Optional
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field


class CriterionComparisonDto(BaseModel):
    """Mirror cua CriterionComparisonDto (Toeic.Application.HumanRating.Dtos,
    AiHumanComparisonDtos.cs, Phase 4 Part 2)."""

    model_config = ConfigDict(populate_by_name=True)

    criterion: str = Field(alias="criterion")
    human_score: Optional[float] = Field(default=None, alias="humanScore")
    ai_score: Optional[float] = Field(default=None, alias="aiScore")
    difference: Optional[float] = Field(default=None, alias="difference")


class AiHumanComparisonDto(BaseModel):
    """Mirror cua AiHumanComparisonDto (Toeic.Application.HumanRating.Dtos,
    AiHumanComparisonDtos.cs, Phase 4 Part 2). Neu property backend thuc te khac ten,
    sua alias tuong ung trong file nay (chi 1 cho de sua)."""

    model_config = ConfigDict(populate_by_name=True)

    candidate_answer_id: UUID = Field(alias="candidateAnswerId")
    question_id: UUID = Field(alias="questionId")
    question_type: str = Field(alias="questionType")
    human_assessment_id: UUID = Field(alias="humanAssessmentId")
    rater_id: UUID = Field(alias="raterId")
    rater_name: str = Field(alias="raterName")
    is_final_human: bool = Field(alias="isFinalHuman")
    human_overall_score: float = Field(alias="humanOverallScore")
    writing_assessment_id: Optional[UUID] = Field(default=None, alias="writingAssessmentId")
    ai_overall_score: Optional[float] = Field(default=None, alias="aiOverallScore")
    overall_difference: Optional[float] = Field(default=None, alias="overallDifference")
    criterion_comparisons: list[CriterionComparisonDto] = Field(
        default_factory=list, alias="criterionComparisons"
    )


class PagedResult(BaseModel):
    """Mirror cua PagedResult<AiHumanComparisonDto> (Toeic.Application.Common.Dtos,
    Phase 1 Part 3)."""

    model_config = ConfigDict(populate_by_name=True)

    items: list[AiHumanComparisonDto] = Field(alias="items")
    total_count: int = Field(alias="totalCount")
    page_number: int = Field(alias="page")
    page_size: int = Field(alias="pageSize")
