import { NextResponse, type NextRequest } from "next/server";
import { applyDevTlsWorkaround, env } from "@/lib/env";

applyDevTlsWorkaround();

/**
 * Proxy tối giản tới `evaluation-service` (Python/FastAPI, Phase 5) —
 * xem PHASE7-ISSUE-03 (PHASE0 mục H) và PHASE7-ISSUE-16
 * (PHASE7_PART6_INTEGRATION_GUIDE.md).
 *
 * Lý do có proxy này thay vì gọi thẳng từ browser:
 * 1. `evaluation-service` là service độc lập, base URL khác — không muốn
 *    lộ ra trình duyệt (đổi hạ tầng sau này không ảnh hưởng Frontend).
 * 2. `evaluation-service` không phải lúc nào cũng bật CORS cho origin của
 *    Next.js (nó được thiết kế để gọi nội bộ, xem Phase 5 Part 1).
 *
 * QUAN TRỌNG (PHASE7-ISSUE-16, GIẢ ĐỊNH cần Dragon xác nhận): tài liệu
 * Phase 5 chỉ mô tả `evaluation-service` tự đăng nhập vào Toeic.Api bằng
 * tài khoản RATER `eval-service` (để LẤY dữ liệu) — không nói rõ các
 * endpoint HTTP của chính `evaluation-service` (`/evaluations/*`) có đòi
 * hỏi Authorization từ phía gọi (ở đây là Next.js) hay không. Route này
 * yêu cầu tối thiểu 1 `Authorization: Bearer <token>` hợp lệ (accessToken
 * ADMIN đang đăng nhập) mới cho qua — không verify chữ ký JWT (đó là việc
 * của Toeic.Api), chỉ đảm bảo không phải request nặc danh trực tiếp vào
 * proxy. Nếu `evaluation-service` thật có yêu cầu auth riêng, thêm header
 * tương ứng ở hàm `forward()` bên dưới.
 */

async function forward(request: NextRequest, path: string[]) {
  const authHeader = request.headers.get("authorization");
  if (!authHeader) {
    return NextResponse.json({ title: "Thiếu Authorization." }, { status: 401 });
  }

  const targetPath = path.join("/");
  const search = request.nextUrl.search;
  const targetUrl = `${env.evaluationServiceUrl}/api/v1/evaluations/${targetPath}${search}`;

  let upstream: Response;
  try {
    upstream = await fetch(targetUrl, {
      method: "GET",
      headers: { Accept: "application/json, text/csv" },
      cache: "no-store",
    });
  } catch (err) {
    console.error("[api/evaluation] không kết nối được evaluation-service", err);
    return NextResponse.json(
      { title: "Không thể kết nối tới evaluation-service." },
      { status: 502 }
    );
  }

  const contentType = upstream.headers.get("content-type") ?? "application/json";
  const contentDisposition = upstream.headers.get("content-disposition");
  const body = await upstream.arrayBuffer();

  const headers = new Headers({ "content-type": contentType });
  if (contentDisposition) headers.set("content-disposition", contentDisposition);

  return new NextResponse(body, { status: upstream.status, headers });
}

export async function GET(request: NextRequest, { params }: { params: Promise<{ path: string[] }> }) {
  const { path } = await params;
  return forward(request, path);
}
