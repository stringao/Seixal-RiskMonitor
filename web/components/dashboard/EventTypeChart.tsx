"use client";

import { useMemo } from "react";
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import type { PieLabelRenderProps } from "recharts/types/polar/Pie";
import type { GeoEvent } from "@/lib/types/event";
import { EVENT_TYPE_LABELS } from "@/lib/map/config";

const EVENT_TYPE_COLORS: Record<string, string> = {
  Fire: "#f97316",
  Flood: "#3b82f6",
  Storm: "#eab308",
  Landslide: "#a855f7",
  Industrial: "#6b7280",
  Heatwave: "#ef4444",
  Other: "#64748b",
};

interface ChartDataItem {
  key: string;
  name: string;
  value: number;
  color: string;
  percent: number;
}

interface EventTypeChartProps {
  events: GeoEvent[];
}

function renderPercentLabel(props: PieLabelRenderProps) {
  const cx = Number(props.cx ?? 0);
  const cy = Number(props.cy ?? 0);
  const midAngle = Number(props.midAngle ?? 0);
  const outerRadius = Number(props.outerRadius ?? 0);
  const percent = Number(props.percent ?? 0);

  const RADIAN = Math.PI / 180;
  const radius = outerRadius + 18;
  const x = cx + radius * Math.cos(-midAngle * RADIAN);
  const y = cy + radius * Math.sin(-midAngle * RADIAN);

  if (percent < 0.05) return null;

  return (
    <text
      x={x}
      y={y}
      fill="#94a3b8"
      textAnchor="middle"
      dominantBaseline="central"
      fontSize={11}
    >
      {`${(percent * 100).toFixed(0)}%`}
    </text>
  );
}

export function EventTypeChart({ events }: EventTypeChartProps) {
  const data = useMemo(() => {
    const counts: Record<string, number> = {};
    const total = events.length;

    for (const e of events) {
      counts[e.eventType] = (counts[e.eventType] ?? 0) + 1;
    }

    return Object.entries(counts)
      .map(([key, value]) => ({
        key,
        name: EVENT_TYPE_LABELS[key] ?? key,
        value,
        color: EVENT_TYPE_COLORS[key] ?? "#64748b",
        percent: total > 0 ? value / total : 0,
      }))
      .sort((a, b) => b.value - a.value);
  }, [events]);

  const totalCount = events.length;

  if (data.length === 0) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 text-center text-slate-500 h-64 flex items-center justify-center">
        Sem dados para exibir
      </div>
    );
  }

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4">
      <h3 className="text-sm font-semibold text-slate-200 mb-2">
        Distribuição por Tipo de Evento
      </h3>
      <div className="grid grid-cols-2 gap-x-4 gap-y-1.5 mb-3">
        {data.map((item: ChartDataItem) => (
          <div key={item.key} className="flex items-center gap-2 text-xs">
            <span
              className="w-2 h-2 rounded-full shrink-0"
              style={{ backgroundColor: item.color }}
            />
            <span className="text-slate-300 truncate">
              {item.name}{" "}
              <span className="text-slate-500">
                ({item.value})
              </span>
            </span>
          </div>
        ))}
      </div>
      <ResponsiveContainer width="100%" height={180}>
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius={50}
            outerRadius={80}
            dataKey="value"
            strokeWidth={2}
            label={renderPercentLabel}
            labelLine={false}
          >
            {data.map((entry: ChartDataItem) => (
              <Cell key={entry.key} fill={entry.color} />
            ))}
            <text
              x="50%"
              y="47%"
              textAnchor="middle"
              dominantBaseline="central"
              fill="#e2e8f0"
              fontSize={24}
              fontWeight={700}
            >
              {totalCount}
            </text>
            <text
              x="50%"
              y="60%"
              textAnchor="middle"
              dominantBaseline="central"
              fill="#64748b"
              fontSize={11}
            >
              eventos
            </text>
          </Pie>
          <Tooltip
            contentStyle={{
              backgroundColor: "#1e293b",
              border: "1px solid #334155",
              borderRadius: "8px",
              color: "#e2e8f0",
            }}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  );
}
