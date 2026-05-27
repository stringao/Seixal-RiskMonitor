"use client";

import { useMemo } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
  ReferenceArea,
} from "recharts";
import { TrendingUp } from "lucide-react";

interface AirQualityChartProps {
  data?: {
    timestamp: string;
    aqi: number;
    pollen?: number;
  }[];
  loading?: boolean;
}

// Mock 5-day forecast data for demonstration
function generateMockForecast(): { timestamp: string; aqi: number; pollen?: number }[] {
  const now = new Date();
  const data = [];
  for (let i = 0; i < 5; i++) {
    const date = new Date(now);
    date.setDate(date.getDate() + i);
    data.push({
      timestamp: date.toLocaleDateString("pt-BR", { weekday: "short", day: "numeric" }),
      aqi: Math.floor(Math.random() * 3) + 2, // 2-4
      pollen: Math.floor(Math.random() * 3) + 1, // 1-3
    });
  }
  return data;
}

const CHART_COLORS = {
  aqi: "#00E400",
  pollen: "#FF7E00",
  grid: "#334155",
  text: "#94a3b8",
  greenZone: "rgba(0, 228, 0, 0.1)",
  yellowZone: "rgba(255, 255, 0, 0.1)",
  orangeZone: "rgba(255, 126, 0, 0.1)",
  redZone: "rgba(255, 0, 0, 0.1)",
};

export function AirQualityChart({ data, loading }: AirQualityChartProps) {
  const chartData = useMemo(() => {
    return data && data.length > 0 ? data : generateMockForecast();
  }, [data]);

  const CustomTooltip = ({ active, payload, label }: { active?: boolean; payload?: Array<{ value: number; dataKey: string; color: string }>; label?: string }) => {
    if (!active || !payload?.length) return null;
    return (
      <div className="bg-slate-800 border border-slate-700 rounded-lg p-3 shadow-xl">
        <p className="text-xs text-slate-400 mb-1">{label}</p>
        {payload.map((entry, index) => (
          <div key={index} className="flex items-center gap-2 text-sm">
            <div
              className="w-2 h-2 rounded-full"
              style={{ backgroundColor: entry.color }}
            />
            <span className="text-slate-300">
              {entry.dataKey === "aqi" ? "Qualidade do Ar" : "Polén"}:
            </span>
            <span className="font-medium text-white">{entry.value}/5</span>
          </div>
        ))}
      </div>
    );
  };

  if (loading) {
    return (
      <div className="bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-5">
        <div className="flex items-center gap-2 mb-4">
          <div className="w-5 h-5 bg-slate-700 rounded animate-pulse" />
          <div className="h-5 w-40 bg-slate-700 rounded animate-pulse" />
        </div>
        <div className="h-48 bg-slate-700/60 rounded-xl animate-pulse" />
      </div>
    );
  }

  return (
    <div className="bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-5">
      <div className="flex items-center gap-2 mb-4">
        <TrendingUp className="w-5 h-5 text-emerald-400" />
        <span className="text-sm font-medium text-slate-300">
          Previsão 5 Dias
        </span>
      </div>

      <div className="h-48">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData} margin={{ top: 5, right: 5, left: -20, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />

            {/* Background zones */}
            <ReferenceArea y1={1} y2={2} fill={CHART_COLORS.greenZone} />
            <ReferenceArea y1={2} y2={3} fill={CHART_COLORS.yellowZone} />
            <ReferenceArea y1={3} y2={4} fill={CHART_COLORS.orangeZone} />
            <ReferenceArea y1={4} y2={5} fill={CHART_COLORS.redZone} />

            {/* Reference lines */}
            <ReferenceLine y={2} stroke="#00E400" strokeDasharray="3 3" strokeOpacity={0.5} />
            <ReferenceLine y={3} stroke="#FFFF00" strokeDasharray="3 3" strokeOpacity={0.5} />
            <ReferenceLine y={4} stroke="#FF7E00" strokeDasharray="3 3" strokeOpacity={0.5} />

            <XAxis
              dataKey="timestamp"
              tick={{ fill: CHART_COLORS.text, fontSize: 11 }}
              tickLine={{ stroke: CHART_COLORS.grid }}
              axisLine={{ stroke: CHART_COLORS.grid }}
            />
            <YAxis
              domain={[1, 5]}
              ticks={[1, 2, 3, 4, 5]}
              tick={{ fill: CHART_COLORS.text, fontSize: 11 }}
              tickLine={{ stroke: CHART_COLORS.grid }}
              axisLine={{ stroke: CHART_COLORS.grid }}
            />
            <Tooltip content={<CustomTooltip />} />

            <Line
              type="monotone"
              dataKey="aqi"
              stroke={CHART_COLORS.aqi}
              strokeWidth={2}
              dot={{ fill: CHART_COLORS.aqi, strokeWidth: 0, r: 4 }}
              activeDot={{ r: 6, fill: CHART_COLORS.aqi }}
              name="aqi"
            />
            {chartData[0]?.pollen !== undefined && (
              <Line
                type="monotone"
                dataKey="pollen"
                stroke={CHART_COLORS.pollen}
                strokeWidth={2}
                strokeDasharray="5 5"
                dot={{ fill: CHART_COLORS.pollen, strokeWidth: 0, r: 4 }}
                activeDot={{ r: 6, fill: CHART_COLORS.pollen }}
                name="pollen"
              />
            )}
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Legend */}
      <div className="flex items-center justify-center gap-6 mt-3">
        <div className="flex items-center gap-2">
          <div className="w-3 h-0.5 bg-[#00E400]" />
          <span className="text-xs text-slate-400">Qualidade do Ar</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-3 h-0.5 bg-[#FF7E00]" style={{ borderStyle: "dashed" }} />
          <span className="text-xs text-slate-400">Polén</span>
        </div>
      </div>
    </div>
  );
}