"use client";

import { Brain, TrendingUp, TrendingDown, Minus, AlertTriangle } from "lucide-react";
import type { SituationReportResponse } from "@/lib/types/dashboard";

const RISK_COLORS: Record<string, string> = {
  Low: "#22c55e",
  Medium: "#eab308",
  High: "#f97316",
  Critical: "#ef4444",
};

const TREND_ICONS: Record<string, React.ReactNode> = {
  increasing: <TrendingUp className="w-4 h-4 text-red-400" />,
  decreasing: <TrendingDown className="w-4 h-4 text-emerald-400" />,
  stable: <Minus className="w-4 h-4 text-slate-400" />,
};

interface AiSituationCardProps {
  situation: SituationReportResponse | null;
  loading?: boolean;
}

export function AiSituationCard({ situation, loading }: AiSituationCardProps) {
  if (loading) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4 animate-pulse">
        <div className="flex items-center gap-2 mb-3">
          <div className="w-8 h-8 bg-slate-700 rounded-lg" />
          <div className="w-32 h-4 bg-slate-700 rounded" />
        </div>
        <div className="space-y-2">
          <div className="w-3/4 h-4 bg-slate-700 rounded" />
          <div className="w-1/2 h-4 bg-slate-700 rounded" />
        </div>
      </div>
    );
  }

  if (!situation) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4">
        <div className="flex items-center gap-2 mb-3">
          <Brain className="w-6 h-6 text-emerald-400" />
          <span className="text-sm font-semibold text-slate-200">Situação AI</span>
        </div>
        <p className="text-sm text-slate-400">Carregando análise...</p>
      </div>
    );
  }

  const riskColor = RISK_COLORS[situation.overallRiskLevel] ?? "#64748b";
  const eventCount = situation.activeEvents?.length ?? 0;
  const hotspotCount = situation.hotspots?.length ?? 0;

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl overflow-hidden">
      <div
        className="px-4 py-3 border-b border-slate-700"
        style={{ borderLeftWidth: 3, borderLeftColor: riskColor }}
      >
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Brain className="w-5 h-5 text-emerald-400" />
            <span className="text-sm font-semibold text-slate-200">Análise AI em Tempo Real</span>
          </div>
          <div
            className="px-2 py-0.5 rounded text-xs font-medium"
            style={{ backgroundColor: `${riskColor}20`, color: riskColor }}
          >
            {situation.overallRiskLevel}
          </div>
        </div>
      </div>

      <div className="p-4 space-y-4">
        {/* Current FWI */}
        <div className="flex items-center justify-between">
          <div className="text-sm text-slate-400">FWI Atual</div>
          <div className="flex items-center gap-2">
            <span className="text-lg font-bold text-white">{situation.currentFwi?.value?.toFixed(1) ?? "N/A"}</span>
            {TREND_ICONS[situation.currentFwi?.trend ?? "stable"]}
          </div>
        </div>

        {/* Quick Stats */}
        <div className="grid grid-cols-2 gap-3">
          <div className="bg-slate-900/50 rounded-lg p-2">
            <div className="text-xs text-slate-500">Eventos 24h</div>
            <div className="text-lg font-semibold text-white">{eventCount}</div>
          </div>
          <div className="bg-slate-900/50 rounded-lg p-2">
            <div className="text-xs text-slate-500">Hotspots</div>
            <div className="text-lg font-semibold text-white">{hotspotCount}</div>
          </div>
        </div>

        {/* Weather Trend */}
        <div className="flex items-center gap-2 text-sm">
          <span className="text-slate-400">Tendência:</span>
          <span className="text-slate-200">{situation.weatherTrend}</span>
        </div>

        {/* Active Events */}
        {situation.activeEvents && situation.activeEvents.length > 0 && (
          <div>
            <div className="text-xs text-slate-500 mb-1">Eventos Recentes</div>
            <div className="space-y-1">
              {situation.activeEvents.slice(0, 3).map((event) => (
                <div
                  key={event.id}
                  className="flex items-center justify-between text-xs"
                >
                  <span className="text-slate-300">{event.title}</span>
                  <span
                    className="px-1.5 py-0.5 rounded"
                    style={{
                      backgroundColor: `${RISK_COLORS[event.severity] ?? "#64748b"}20`,
                      color: RISK_COLORS[event.severity] ?? "#64748b",
                    }}
                  >
                    {event.severity}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Recommendations */}
        {situation.immediateRecommendations && situation.immediateRecommendations.length > 0 && (
          <div className="border-t border-slate-700 pt-3">
            <div className="flex items-center gap-1 mb-2">
              <AlertTriangle className="w-4 h-4 text-yellow-400" />
              <span className="text-xs font-medium text-slate-300">Recomendações</span>
            </div>
            <ul className="space-y-1">
              {situation.immediateRecommendations.slice(0, 2).map((rec, idx) => (
                <li key={idx} className="text-xs text-slate-400">
                  • {rec}
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Generated timestamp */}
        <div className="text-xs text-slate-600">
          Atualizado: {new Date(situation.generatedAt).toLocaleTimeString("pt-PT")}
        </div>
      </div>
    </div>
  );
}
