"use client";

import { useMemo } from "react";
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";
import { Flame, TrendingUp, TrendingDown, Minus } from "lucide-react";
import type { SeasonalStatisticsResponse, TrendAnalysisResponse } from "@/lib/types/fireReport";

interface SeasonalChartProps {
  statistics: SeasonalStatisticsResponse[];
  trend?: TrendAnalysisResponse | null;
  loading?: boolean;
}

const MONTHS_PT = [
  "Jan",
  "Fev",
  "Mar",
  "Abr",
  "Mai",
  "Jun",
  "Jul",
  "Ago",
  "Set",
  "Out",
  "Nov",
  "Dez",
];

export function SeasonalChart({
  statistics,
  trend,
  loading,
}: SeasonalChartProps) {
  const chartData = useMemo(() => {
    // Filter to current year monthly data
    const currentYear = new Date().getFullYear();
    const monthlyData = statistics
      .filter((s) => s.year === currentYear && s.month > 0)
      .sort((a, b) => a.month - b.month)
      .map((s) => ({
        name: MONTHS_PT[s.month - 1],
        month: s.month,
        fires: s.totalFires,
        area: s.totalAreaHa,
        avgFwi: s.averageFwi,
      }));

    // Ensure all 12 months are present
    const fullYearData = Array.from({ length: 12 }, (_, i) => {
      const found = monthlyData.find((d) => d.month === i + 1);
      return found || { name: MONTHS_PT[i], month: i + 1, fires: 0, area: 0, avgFwi: 0 };
    });

    return fullYearData;
  }, [statistics]);

  const trendIcon = useMemo(() => {
    if (!trend) return null;
    switch (trend.trendDirection) {
      case "increasing":
        return <TrendingUp className="w-4 h-4 text-red-400" />;
      case "decreasing":
        return <TrendingDown className="w-4 h-4 text-emerald-400" />;
      default:
        return <Minus className="w-4 h-4 text-slate-400" />;
    }
  }, [trend]);

  if (loading) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 animate-pulse">
        <div className="h-6 w-48 bg-slate-700/60 rounded mb-4" />
        <div className="h-64 bg-slate-700/60 rounded" />
      </div>
    );
  }

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl overflow-hidden">
      {/* Header */}
      <div className="px-4 py-3 border-b border-slate-700/50 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Flame className="w-4 h-4 text-orange-400" />
          <h3 className="text-sm font-semibold text-slate-200">
            Estatísticas Sazonais
          </h3>
        </div>
        {trend && (
          <div className="flex items-center gap-2 text-xs">
            {trendIcon}
            <span className="text-slate-400">{trend.trendDescription}</span>
          </div>
        )}
      </div>

      {/* Chart Tabs */}
      <div className="px-4 pt-3 flex gap-4 border-b border-slate-700/50">
        <TabButton active={true}>Incêndios</TabButton>
        <TabButton active={false}>Área (ha)</TabButton>
        <TabButton active={false}>FWI Médio</TabButton>
      </div>

      {/* Chart */}
      <div className="p-4">
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart
              data={chartData}
              margin={{ top: 10, right: 10, left: -10, bottom: 0 }}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
              <XAxis
                dataKey="name"
                tick={{ fill: "#94a3b8", fontSize: 11 }}
                axisLine={{ stroke: "#334155" }}
                tickLine={false}
              />
              <YAxis
                tick={{ fill: "#94a3b8", fontSize: 11 }}
                axisLine={false}
                tickLine={false}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: "#1e293b",
                  border: "1px solid #334155",
                  borderRadius: "8px",
                  fontSize: "12px",
                }}
                labelStyle={{ color: "#e2e8f0" }}
                itemStyle={{ color: "#94a3b8" }}
              />
              <Bar
                dataKey="fires"
                fill="#f97316"
                radius={[4, 4, 0, 0]}
                name="Incêndios"
              />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Trend Summary */}
      {trend && (
        <div className="px-4 py-3 bg-slate-900/30 border-t border-slate-700/50">
          <div className="grid grid-cols-2 gap-4 text-xs">
            <div>
              <span className="text-slate-500">Média Anual</span>
              <p className="text-white font-medium">
                {trend.averageFiresPerYear.toFixed(1)} incêndios/ano
              </p>
            </div>
            <div>
              <span className="text-slate-500">Área Média</span>
              <p className="text-white font-medium">
                {trend.averageAreaPerYear.toFixed(1)} ha/ano
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function TabButton({ active, children }: { active: boolean; children: React.ReactNode }) {
  return (
    <button
      className={`pb-2 text-xs font-medium transition-colors ${
        active
          ? "text-emerald-400 border-b-2 border-emerald-400"
          : "text-slate-500 hover:text-slate-300"
      }`}
    >
      {children}
    </button>
  );
}

// Standalone chart component for trend display
interface TrendChartProps {
  trend: TrendAnalysisResponse;
}

export function TrendChart({ trend }: TrendChartProps) {
  const data = useMemo(() => {
    return trend.years.map((year, i) => ({
      year,
      fires: trend.firesPerYear[i],
      area: trend.areaPerYear[i],
    }));
  }, [trend]);

  return (
    <div className="h-64">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart
          data={data}
          margin={{ top: 10, right: 10, left: -10, bottom: 0 }}
        >
          <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
          <XAxis
            dataKey="year"
            tick={{ fill: "#94a3b8", fontSize: 11 }}
            axisLine={{ stroke: "#334155" }}
            tickLine={false}
          />
          <YAxis
            yAxisId="left"
            tick={{ fill: "#94a3b8", fontSize: 11 }}
            axisLine={false}
            tickLine={false}
          />
          <YAxis
            yAxisId="right"
            orientation="right"
            tick={{ fill: "#94a3b8", fontSize: 11 }}
            axisLine={false}
            tickLine={false}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: "#1e293b",
              border: "1px solid #334155",
              borderRadius: "8px",
              fontSize: "12px",
            }}
            labelStyle={{ color: "#e2e8f0" }}
          />
          <Legend />
          <Line
            yAxisId="left"
            type="monotone"
            dataKey="fires"
            stroke="#f97316"
            strokeWidth={2}
            dot={{ fill: "#f97316", strokeWidth: 0 }}
            name="Incêndios"
          />
          <Line
            yAxisId="right"
            type="monotone"
            dataKey="area"
            stroke="#22c55e"
            strokeWidth={2}
            dot={{ fill: "#22c55e", strokeWidth: 0 }}
            name="Área (ha)"
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
