"use client";

import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { ScoreTrendPoint } from "@/types/candidate-analytics";

const COLORS = {
  listening: "#1c7a72",
  reading: "#b8862e",
  writing: "#29344a",
};

export function ScoreTrendChart({ points }: { points: ScoreTrendPoint[] }) {
  const data = points
    .slice()
    .sort((a, b) => new Date(a.submittedAt).getTime() - new Date(b.submittedAt).getTime())
    .map((p) => ({
      date: new Date(p.submittedAt).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit" }),
      Nghe: p.listeningScore,
      Đọc: p.readingScore,
      Viết: p.writingScore,
    }));

  if (data.length === 0) {
    return <p className="text-sm text-muted">Chưa có đủ dữ liệu để hiển thị xu hướng.</p>;
  }

  return (
    <div className="h-72 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ top: 8, right: 16, bottom: 0, left: -16 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#d3dadd" vertical={false} />
          <XAxis dataKey="date" tick={{ fontSize: 12, fill: "#5c6b73" }} axisLine={{ stroke: "#d3dadd" }} />
          <YAxis tick={{ fontSize: 12, fill: "#5c6b73" }} axisLine={{ stroke: "#d3dadd" }} />
          <Tooltip
            contentStyle={{
              background: "#ffffff",
              border: "1px solid #d3dadd",
              borderRadius: 2,
              fontSize: 13,
            }}
          />
          <Line type="monotone" dataKey="Nghe" stroke={COLORS.listening} strokeWidth={2} dot={{ r: 3 }} connectNulls />
          <Line type="monotone" dataKey="Đọc" stroke={COLORS.reading} strokeWidth={2} dot={{ r: 3 }} connectNulls />
          <Line type="monotone" dataKey="Viết" stroke={COLORS.writing} strokeWidth={2} dot={{ r: 3 }} connectNulls />
        </LineChart>
      </ResponsiveContainer>
      <div className="mt-3 flex gap-5 text-[12px] text-muted">
        <LegendDot color={COLORS.listening} label="Nghe" />
        <LegendDot color={COLORS.reading} label="Đọc" />
        <LegendDot color={COLORS.writing} label="Viết" />
      </div>
    </div>
  );
}

function LegendDot({ color, label }: { color: string; label: string }) {
  return (
    <span className="flex items-center gap-1.5">
      <span className="h-2 w-2 rounded-full" style={{ background: color }} />
      {label}
    </span>
  );
}
