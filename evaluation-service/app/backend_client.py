from __future__ import annotations

import time
from typing import Optional

import httpx

from .config import Settings
from .schemas import AiHumanComparisonDto, PagedResult


class BackendAuthError(RuntimeError):
    """Dang nhap vao Toeic.Api that bai (sai tai khoan service, hoac backend khong phan hoi)."""


class BackendApiError(RuntimeError):
    """Backend tra ve loi khi goi endpoint comparison (khac loi auth)."""


class BackendClient:
    """
    Client goi sang Toeic.Api (.NET) de lay dataset AI-vs-Human.

    PHASE5-ISSUE-01 (xem PHASE5_PART1_INTEGRATION_GUIDE.md): Evaluation Service KHONG tu
    query PostgreSQL truc tiep va tu JOIN lai logic so sanh AI/Human o day - ma goi thang
    GET /api/v1/rater/writing-answers/comparison da co san tu Phase 4 Part 2. Ly do: logic
    ghep AI/Human theo CandidateAnswerId + Criterion da ton tai dung 1 noi
    (HumanRatingService.GetAiHumanComparisonAsync) - viet lai bang SQL tho o day se tao ra
    2 noi cung dinh nghia "cap AI-Human hop le", dung rui ro ma IWritingScoreResolver
    (Phase 4 Part 2) da duoc tao ra de tranh cho phan "diem chinh thuc".

    Auth: dung 1 tai khoan RATER rieng cho service. KHONG trien khai lai vong doi
    refresh-token rotation o phia Python (YAGNI cho mot service noi bo goi API dinh
    ky/theo yeu cau) - don gian hon la tu login lai bang username/password moi khi access
    token gan het han hoac bi 401.
    """

    def __init__(self, settings: Settings):
        self._settings = settings
        self._client = httpx.AsyncClient(
            base_url=settings.backend_base_url,
            verify=settings.backend_verify_ssl,
            timeout=settings.backend_request_timeout_seconds,
        )
        self._access_token: Optional[str] = None
        self._access_token_expires_at: float = 0.0

    async def aclose(self) -> None:
        await self._client.aclose()

    async def _login(self) -> None:
        response = await self._client.post(
            "/api/v1/auth/login",
            json={
                "usernameOrEmail": self._settings.eval_service_username,
                "password": self._settings.eval_service_password,
            },
        )
        if response.status_code != 200:
            raise BackendAuthError(
                f"Login vao Toeic.Api that bai (status={response.status_code}). "
                "Kiem tra EVAL_SERVICE_USERNAME/PASSWORD va tai khoan RATER tuong ung."
            )
        body = response.json()
        self._access_token = body["accessToken"]
        # Khong parse JWT de lay 'exp' chinh xac (tranh them dependency PyJWT chi cho viec
        # nay) - dung thoi han cau hinh phia backend (mac dinh 30 phut,
        # Jwt:AccessTokenMinutes) tru hao 60 giay an toan.
        self._access_token_expires_at = time.monotonic() + (30 * 60) - 60

    async def _ensure_token(self) -> None:
        if self._access_token is None or time.monotonic() >= self._access_token_expires_at:
            await self._login()

    async def _authorized_get(self, url: str, params: dict) -> httpx.Response:
        await self._ensure_token()
        response = await self._client.get(
            url, params=params, headers={"Authorization": f"Bearer {self._access_token}"}
        )
        if response.status_code == 401:
            # Token co the da bi revoke/het han som hon du kien -> thu login lai dung 1 lan.
            await self._login()
            response = await self._client.get(
                url, params=params, headers={"Authorization": f"Bearer {self._access_token}"}
            )
        return response

    async def fetch_comparison_page(
        self,
        page_number: int,
        page_size: int,
        question_type: Optional[str] = None,
        only_with_both_scores: bool = True,
    ) -> PagedResult:
        params: dict = {
            "pageNumber": page_number,
            "pageSize": page_size,
            "onlyWithBothScores": str(only_with_both_scores).lower(),
        }
        if question_type:
            params["questionType"] = question_type

        response = await self._authorized_get(
            "/api/v1/rater/writing-answers/comparison", params
        )
        if response.status_code != 200:
            raise BackendApiError(
                f"GET /rater/writing-answers/comparison that bai "
                f"(status={response.status_code}): {response.text[:500]}"
            )
        return PagedResult.model_validate(response.json())

    async def fetch_all_comparisons(
        self,
        question_type: Optional[str] = None,
        only_with_both_scores: bool = True,
    ) -> list[AiHumanComparisonDto]:
        """Lap qua toan bo trang cho toi khi lay du TotalCount (don gian, du dung cho quy
        mo do an - khong can streaming/cursor pagination)."""
        page_size = self._settings.backend_page_size
        page_number = 1
        all_items: list[AiHumanComparisonDto] = []

        first_page = await self.fetch_comparison_page(
            page_number, page_size, question_type, only_with_both_scores
        )
        all_items.extend(first_page.items)
        total_count = first_page.total_count

        while len(all_items) < total_count:
            page_number += 1
            page = await self.fetch_comparison_page(
                page_number, page_size, question_type, only_with_both_scores
            )
            if not page.items:
                break  # an toan: tranh vong lap vo han neu total_count khong khop thuc te
            all_items.extend(page.items)

        return all_items
