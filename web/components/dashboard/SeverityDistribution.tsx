"use client";

import { useMemo } from "react";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Cell,
  LabelList,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import type { GeoEvent, RiskLevel } from "@/lib/types/event";

interface SeverityDistributionProps {
  events: GeoEvent[];
}

interface SeverityRow {
  name: string;
  count: number;
  fill: string;
}

const SEVERITY_ORDER: { level: RiskLevel; label: string; color: string }[] = [
  { level: "Low", label: "Baixo", color: "#22c55e" },
  { level: "Medium", label: "Médio", color: "#eab308" },
  { level: "High", label: "Alto", color: "#f97316" },
  { level: "Critical", label: "Crítico", color: "#ef4444" },
];

export function SeverityDistribution({ events }: SeverityDistributionProps) {
  const data = useMemo(() => {
    const counts = new Map<RiskLevel, number>();
    for (const event of events) {
      counts.set(event.severity, (counts.get(event.severity) ?? 0) + 1);
    }

    return SEVERITY_ORDER.map(({ level, label, color }) => ({
      name: label,
      count: counts.get(level) ?? 0,
      fill: color,
    }));
  }, [events]);

  if (events.length === 0) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 text-center text-slate-500 h-[256px] flex items-center justify-center">
        Sem dados para exibir
      </div>
    );
  }

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4">
      <h3 className="text-sm font-semibold text-slate-200 mb-4">
        Distribuição por Severidade
      </h3>
      <div className="flex flex-wrap gap-3 mb-4">
        {SEVERITY_ORDER.map(({ label, color }) => (
          <div key={label} className="flex items-center gap-1.5">
            <span className="w-3 h-3 rounded-sm" style={{ backgroundColor: color }} />
            <span className="text-xs text-slate-400">{label}</span>
          </div>
        ))}
      </div>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart
          data={data}
          layout="vertical"
          margin={{ top: 0, right: 40, left: 0, bottom: 0 }}
        >
          <XAxis type="number" tick={{ fill: "#94a3b8", fontSize: 12 }} allowDecimals={false} />
          <YAxis
            type="category"
            dataKey="name"
            tick={{ fill: "#94a3b8", fontSize: 12 }}
            width={60}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: "#1e293b",
              border: "1px solid #334155",
              borderRadius: "8px",
              color: "#e2e8f0",
            }}
          />
          <Bar dataKey="count" barSize={24} radius={[0, 4, 4, 0]}>
            {data.map((row) => (
              <Cell key={row.name} fill={row.fill} />
            ))}
            <LabelList
              dataKey="count"
              position="right"
              style={{ fill: "#ffffff", fontSize: 12, fontWeight: 600 }}
            />
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
