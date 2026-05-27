"use client";

import { useMemo } from "react";
import { format, subDays, parseISO, startOfDay } from "date-fns";
import { pt } from "date-fns/locale";
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";
import type { GeoEvent, RiskLevel } from "@/lib/types/event";

interface EventsTimeChartProps {
  events: GeoEvent[];
}

interface DayBucket {
  date: string;
  Baixo: number;
  Médio: number;
  Alto: number;
  Crítico: number;
}

const SEVERITY_MAP: Record<RiskLevel, keyof Omit<DayBucket, "date">> = {
  Low: "Baixo",
  Medium: "Médio",
  High: "Alto",
  Critical: "Crítico",
};

const AREA_CONFIG: { key: keyof Omit<DayBucket, "date">; color: string }[] = [
  { key: "Baixo", color: "#22c55e" },
  { key: "Médio", color: "#eab308" },
  { key: "Alto", color: "#f97316" },
  { key: "Crítico", color: "#ef4444" },
];

export function EventsTimeChart({ events }: EventsTimeChartProps) {
  const data = useMemo(() => {
    const today = startOfDay(new Date());
    const buckets = new Map<string, DayBucket>();

    for (let i = 29; i >= 0; i--) {
      const day = subDays(today, i);
      const key = format(day, "yyyy-MM-dd");
      buckets.set(key, {
        date: format(day, "dd MMM", { locale: pt }),
        Baixo: 0,
        Médio: 0,
        Alto: 0,
        Crítico: 0,
      });
    }

    for (const event of events) {
      const dayKey = format(parseISO(event.occurredAt), "yyyy-MM-dd");
      const bucket = buckets.get(dayKey);
      if (bucket) {
        const field = SEVERITY_MAP[event.severity];
        bucket[field] += 1;
      }
    }

    return Array.from(buckets.values());
  }, [events]);

  if (events.length === 0) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 text-center text-slate-500 h-[336px] flex items-center justify-center">
        Sem dados para exibir
      </div>
    );
  }

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-semibold text-slate-200">
          Eventos ao Longo do Tempo
        </h3>
        <div className="flex items-center gap-3 text-xs">
          <div className="flex items-center gap-1.5">
            <span className="w-3 h-2 rounded-sm bg-emerald-500/30 border border-emerald-500/50" />
            <span className="text-slate-400">Baixo</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="w-3 h-2 rounded-sm bg-yellow-500/30 border border-yellow-500/50" />
            <span className="text-slate-400">Médio</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="w-3 h-2 rounded-sm bg-orange-500/30 border border-orange-500/50" />
            <span className="text-slate-400">Alto</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="w-3 h-2 rounded-sm bg-red-500/30 border border-red-500/50" />
            <span className="text-slate-400">Crítico</span>
          </div>
        </div>
      </div>
      <ResponsiveContainer width="100%" height={280}>
        <AreaChart data={data} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
          <XAxis
            dataKey="date"
            tick={{ fill: "#94a3b8", fontSize: 12 }}
            interval="preserveStartEnd"
          />
          <YAxis
            tick={{ fill: "#94a3b8", fontSize: 12 }}
            allowDecimals={false}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: "#1e293b",
              border: "1px solid #334155",
              borderRadius: "8px",
              color: "#e2e8f0",
            }}
          />
          <Legend wrapperStyle={{ fontSize: "12px", color: "#94a3b8" }} />
          {AREA_CONFIG.map(({ key, color }) => (
            <Area
              key={key}
              type="monotone"
              dataKey={key}
              stackId="1"
              stroke={color}
              fill={color}
              fillOpacity={0.3}
            />
          ))}
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
