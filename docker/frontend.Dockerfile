# docker/frontend.Dockerfile
#
# Build context PHẢI là ROOT của repo. Ví dụ build thủ công (chạy từ root):
#   docker build -f docker/frontend.Dockerfile -t toeic-frontend .
#
# ⚠️ GIẢ ĐỊNH (PHASE8-ISSUE-04 phần frontend): sandbox này chưa từng thấy
# frontend/next.config.ts thật. Dockerfile dưới đây dùng chế độ build/start
# tiêu chuẩn (`next build` + `next start`), AN TOÀN với mọi cấu hình Next.js
# hiện có (không yêu cầu output: 'standalone'). Nếu Dragon muốn image nhẹ hơn,
# có thể bật `output: 'standalone'` trong next.config.ts rồi đổi stage cuối
# sang copy .next/standalone — xem ghi chú "Tối ưu thêm (tuỳ chọn)" trong
# PHASE8_PART1_INTEGRATION_GUIDE.md. Không bắt buộc phải đổi.

# ---------- Stage 1: Dependencies ----------
FROM node:20-bookworm-slim AS deps
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci

# ---------- Stage 2: Build ----------
FROM node:20-bookworm-slim AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY frontend/ .

# ⚠️ QUAN TRỌNG (sửa sau lỗi thật 09/09/2026 — xem PHASE8_PART1_HOTFIX.md):
# NEXT_PUBLIC_* được Next.js INLINE VÀO BUNDLE JS NGAY LÚC "next build",
# không phải lúc container chạy. Đặt nó ở docker-compose.yml "environment:"
# (runtime) là VÔ TÁC DỤNG với các biến NEXT_PUBLIC_* — code JS đã build
# xong từ trước. Phải nhận qua ARG rồi set thành ENV TRƯỚC khi "npm run build".
#
# Các biến server-side thật khác (BACKEND_URL, EVALUATION_SERVICE_URL, JWT
# cookie secret...) vẫn đọc lúc runtime bởi Route Handler (Node.js server)
# như bình thường — không cần ARG, không đổi.
ARG NEXT_PUBLIC_BACKEND_URL
ENV NEXT_PUBLIC_BACKEND_URL=${NEXT_PUBLIC_BACKEND_URL}

RUN npm run build

# ---------- Stage 3: Runtime ----------
FROM node:20-bookworm-slim AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system --gid 1001 nodegrp \
    && useradd --system --uid 1001 --gid nodegrp nodeusr

COPY --from=build /app/public ./public
COPY --from=build /app/.next ./.next
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/package.json ./package.json

RUN chown -R nodeusr:nodegrp /app
USER nodeusr

ENV NODE_ENV=production \
    PORT=3000 \
    HOSTNAME=0.0.0.0

EXPOSE 3000

# /api/health là route MỚI cần thêm (xem PHASE8_PART1_INTEGRATION_GUIDE.md mục
# "Việc BẮT BUỘC #2") — trả 200 ngay lập tức, không phụ thuộc backend .NET, để
# healthcheck phản ánh đúng "process Next.js đã sẵn sàng nhận request".
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=6 \
    CMD curl -f http://localhost:3000/api/health || exit 1

CMD ["npm", "start"]
