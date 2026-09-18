// File: frontend/types/bulk-common.ts — MỚI (dùng chung cho Bulk Question và Bulk Rubric, khớp
// BulkRowErrorDto phía backend — nếu file này đã tồn tại từ lúc làm Bulk Question, KHÔNG tạo lại,
// chỉ cần đổi import ở rubric.ts phía trên trỏ đúng vào file cũ)
export interface BulkRowError {
  rowNumber: number;
  errors: string[];
}