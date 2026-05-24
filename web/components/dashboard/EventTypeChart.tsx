"use client";

import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, Legend } from "recharts";
import type { GeoEvent } from "@/lib/types/event";
import { EVENT_TYPE_LABELS } from "@/lib/map/config";
import { useMemo } from "react";

const COLORS = ["#f97316", "#3b82f6", "#eab308", "#a855f7", "#ef4444", "#ec4899", "#6b7280"];

interface EventTypeChartProps {
  events: GeoEvent[];
}

export function EventTypeChart({ events }: EventTypeChartProps) {
  const data = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const e of events) {
      counts[e.eventType] = (counts[e.eventType] ?? 0) + 1;
    }
    return Object.entries(counts).map(([name, value]) => ({
      name: EVENT_TYPE_LABELS[name] ?? name,
      value,
    }));
  }, [events]);

  if (data.length === 0) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 text-center text-slate-500 h-64 flex items-center justify-center">
        Sem dados para exibir
      </div>
    );
  }

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4">
      <h3 className="text-sm font-semibold text-slate-200 mb-3">Distribuição por Tipo</h3>
      <ResponsiveContainer width="100%" height={200}>
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius={50}
            outerRadius={80}
            dataKey="value"
            strokeWidth={2}
          >
            {data.map((_, i) => (
              <Cell key={i} fill={COLORS[i % COLORS.length]} />
            ))}
          </Pie>
          <Tooltip
            contentStyle={{
              backgroundColor: "#1e293b",
              border: "1px solid #334155",
              borderRadius: "8px",
              color: "#e2e8f0",
            }}
          />
          <Legend wrapperStyle={{ fontSize: "12px", color: "#94a3b8" }} />
        </PieChart>
      </ResponsiveContainer>
    </div>
  );
}