/**
 * Hoạ tiết trang trí gợi phiếu trả lời trắc nghiệm (bubble sheet) — bám sát
 * chủ đề "chấm điểm khách quan bằng Answer Key" của chính hệ thống, thay vì
 * gradient/blob trang trí chung chung. Thuần SVG, không phụ thuộc ảnh ngoài.
 */
export function AnswerSheetPattern({ className }: { className?: string }) {
  const rows = 10;
  const cols = 4;
  const cellSize = 34;
  const filled = new Set(["1-1", "3-2", "5-0", "6-3", "8-1"]);

  const circles = [];
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const key = `${r}-${c}`;
      circles.push(
        <circle
          key={key}
          cx={c * cellSize + cellSize / 2}
          cy={r * cellSize + cellSize / 2}
          r={7}
          fill={filled.has(key) ? "currentColor" : "none"}
          stroke="currentColor"
          strokeWidth={filled.has(key) ? 0 : 1.2}
          opacity={filled.has(key) ? 0.9 : 0.35}
        />
      );
    }
  }

  return (
    <svg
      viewBox={`0 0 ${cols * cellSize} ${rows * cellSize}`}
      className={className}
      aria-hidden="true"
    >
      {circles}
    </svg>
  );
}
