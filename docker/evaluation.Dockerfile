# docker/evaluation.Dockerfile
#
# Build context PHẢI là ROOT của repo. Ví dụ build thủ công (chạy từ root):
#   docker build -f docker/evaluation.Dockerfile -t toeic-evaluation .

FROM python:3.12-slim AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system --gid 1002 evalgrp \
    && useradd --system --uid 1002 --gid evalgrp evaluser

# Copy requirements trước để tận dụng layer cache (giống pattern csproj ở backend)
COPY evaluation-service/requirements.txt ./
RUN pip install --no-cache-dir --break-system-packages -r requirements.txt

COPY evaluation-service/app/ ./app/

RUN chown -R evaluser:evalgrp /app
USER evaluser

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1

EXPOSE 8000

# /health là route MỚI cần thêm (xem PHASE8_PART1_INTEGRATION_GUIDE.md mục
# "Việc BẮT BUỘC #3") — endpoint hiện tại (Phase 5) chưa có route health riêng.
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=6 \
    CMD curl -f http://localhost:8000/health || exit 1

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
