import { redirect } from "next/navigation";

/**
 * Middleware (xem middleware.ts) luôn redirect "/" trước khi request tới
 * được đây (dựa vào cookie httpOnly). Route Handler/Server Component này
 * chỉ là fallback an toàn — không nên render trong điều kiện thường.
 */
export default function RootPage() {
  redirect("/login");
}
