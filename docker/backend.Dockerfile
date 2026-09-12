# docker/backend.Dockerfile
#
# Build context PHẢI là ROOT của repo (toeic-scoring-system/), không phải docker/.
# Ví dụ build thủ công (chạy từ root):
#   docker build -f docker/backend.Dockerfile -t toeic-backend .
#
# Lý do multi-stage: stage "build" có SDK đầy đủ (~800MB) chỉ dùng để restore/publish,
# stage "runtime" chỉ chứa ASP.NET runtime (~220MB) + output đã publish — không leak
# source code/SDK vào image chạy production, đúng best practice Docker cho .NET.

# ---------- Stage 1: Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + csproj trước để tận dụng Docker layer cache: nếu chỉ code
# nghiệp vụ đổi (không đổi dependency), bước "dotnet restore" không phải chạy lại.
COPY backend/Toeic.sln ./
COPY backend/src/Toeic.Domain/Toeic.Domain.csproj src/Toeic.Domain/
COPY backend/src/Toeic.Application/Toeic.Application.csproj src/Toeic.Application/
COPY backend/src/Toeic.Infrastructure/Toeic.Infrastructure.csproj src/Toeic.Infrastructure/
COPY backend/src/Toeic.Api/Toeic.Api.csproj src/Toeic.Api/

RUN dotnet restore src/Toeic.Api/Toeic.Api.csproj

# Copy toàn bộ source thật rồi publish
COPY backend/src/ src/
RUN dotnet publish src/Toeic.Api/Toeic.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Stage 2: Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl: cần cho HEALTHCHECK gọi GET /api/v1/health (endpoint đã có sẵn từ Phase 1 Part 1,
# xem README.md "Health check: GET /api/v1/health -> { status: ok, database: connected }").
# aspnet:8.0 base image (Debian) không có sẵn curl.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Chạy bằng user không phải root — nguyên tắc Secure by Design (mục 8 prompt gốc)
RUN groupadd --system --gid 1000 appgroup \
    && useradd --system --uid 1000 --gid appgroup appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Docker \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=6 \
    CMD curl -f http://localhost:8080/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "Toeic.Api.dll"]
