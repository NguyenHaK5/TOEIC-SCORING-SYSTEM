/**
 * GIẢ ĐỊNH cần đối chiếu (Medium — ảnh hưởng mọi trang danh sách Admin):
 * `phase-tracker.md` (mục Phase 6 "Việc tiếp theo") đã ghi nhận đây là một
 * giả định mở từ trước: "Xác nhận shape PagedResult<T> khớp với giả định".
 * File `Toeic.Application/Common/Dtos/PagedResult.cs` (Phase 1 Part 3) chưa
 * từng được sandbox này nhìn thấy nội dung thật — đây là convention .NET
 * phổ biến nhất nên dùng làm giả định, nhưng CẦN Dragon đối chiếu.
 * Nếu field name thật khác (vd `data`/`total` thay vì `items`/`totalCount`),
 * chỉ cần sửa type này + hàm `unwrapPagedResult` bên dưới, không ảnh hưởng
 * phần còn lại của Part 2.
 */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export function emptyPagedResult<T>(): PagedResult<T> {
  return { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
}
