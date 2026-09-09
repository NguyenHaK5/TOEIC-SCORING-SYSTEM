/**
 * ⚠️ Đã sửa sau khi chạy thật: field có thể là `undefined` (field vắng
 * mặt trong JSON) chứ không chỉ `null` như giả định ban đầu — dùng
 * `value == null` (so sánh lỏng, bắt cả 2 trường hợp) thay vì
 * `value === null`.
 */
export function MetricValue({
  label,
  value,
  decimals = 3,
}: {
  label: string;
  value: number | null | undefined;
  decimals?: number;
}) {
  return (
    <div>
      <p className="text-[12px] text-muted">{label}</p>
      <p className="mt-0.5 font-serif text-xl text-ink">
        {value == null ? (
          <span className="text-[14px] text-muted">Không đủ dữ liệu</span>
        ) : (
          value.toFixed(decimals)
        )}
      </p>
    </div>
  );
}
