# TOEIC Evaluation Service (Phase 5 — Part 1)

Python/FastAPI service độc lập với backend .NET (đúng PHASE0 mục J/F: "Không ép Python
vào Backend .NET"). Part 1 mới có **data ingestion** — kéo dataset AI-vs-Human từ
Toeic.Api, chưa tính MAE/RMSE/Correlation (Part 2).

## Cách chạy

```bash
cd evaluation-service
python -m venv .venv
source .venv/bin/activate        # Windows: .venv\Scripts\activate
pip install -r requirements.txt

cp .env.example .env
# Sửa .env: BACKEND_BASE_URL, EVAL_SERVICE_USERNAME, EVAL_SERVICE_PASSWORD

uvicorn app.main:app --reload --port 8001
```

## Trước khi chạy — cần 1 tài khoản RATER riêng cho service

```bash
ADMIN_TOKEN="<access token ADMIN>"
curl -X POST https://localhost:<port>/api/v1/auth/register-staff -k \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"username":"eval-service","fullName":"Evaluation Service","email":"eval-service@internal.local","password":"<mat khau manh, dat trung voi .env>","role":"RATER"}'
```

Dùng RATER (không phải ADMIN) vì chỉ cần đọc endpoint
`GET /rater/writing-answers/comparison`, theo nguyên tắc least privilege.

## Thử nhanh

```bash
curl http://localhost:8001/api/v1/health

curl "http://localhost:8001/api/v1/evaluations/dataset?onlyWithBothScores=true"
# -> { totalPairs, totalCriterionRows, byQuestionType, byCriterion, sample: [...] }
```

Nếu `totalPairs = 0`: bình thường nếu Phase 4 Part 1 chưa có bài nào được Rater chấm
`setAsFinal=true`, hoặc dataset Phase 3/4 chưa build/test thật (xem cảnh báo đầu
`phase-tracker.md`) — không phải lỗi của Part 1 này.

Swagger UI (tự sinh bởi FastAPI): `http://localhost:8001/docs`.
