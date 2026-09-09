import { decodeJwt } from "jose";

/**
 * Đọc claim `exp` từ accessToken để chủ động lên lịch refresh trước khi hết
 * hạn (tránh việc request nghiệp vụ đầu tiên sau khi hết hạn bị 401 rồi mới
 * refresh). KHÔNG dùng để xác thực chữ ký — đó là việc của backend .NET.
 * Trả về mili-giây còn lại, hoặc `null` nếu không đọc được.
 */
export function msUntilExpiry(accessToken: string): number | null {
  try {
    const claims = decodeJwt(accessToken);
    if (typeof claims.exp !== "number") return null;
    return claims.exp * 1000 - Date.now();
  } catch {
    return null;
  }
}
