"use client";

import { Brain, RefreshCw } from "lucide-react";
import { useAiDashboard } from "@/hooks/useAiDashboard";
import { AiSituationCard } from "./AiSituationCard";
import { RiskTrendBadge } from "./RiskTrendBadge";
import { KeyFactorsList } from "./KeyFactorsList";
import { AigRecommendationAlert } from "./AigRecommendationAlert";

export function AiInsightPanel() {
  const { dashboard, situation, weekly, comparison, loading, error, refresh } = useAiDashboard();

  const handleRefresh = () => {
    refresh();
  };

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Brain className="w-5 h-5 text-emerald-400" />
          <h3 className="text-sm font-semibold text-slate-200">Insights AI</h3>
        </div>
        <button
          onClick={handleRefresh}
          className="p-1.5 hover:bg-slate-700/50 rounded-lg transition-colors"
          disabled={loading}
        >
          <RefreshCw className={`w-4 h-4 text-slate-400 ${loading ? "animate-spin" : ""}`} />
        </button>
      </div>

      {/* Error State */}
      {error && (
        <div className="bg-red-950/30 border border-red-500/30 rounded-lg p-3">
          <p className="text-sm text-red-400">{error}</p>
        </div>
      )}

      {/* Main Situation Card */}
      <AiSituationCard situation={situation} loading={loading} />

      {/* Risk Summary with Trend */}
      {dashboard && (
        <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4">
          <div className="flex items-center justify-between mb-3">
            <div className="text-sm font-medium text-slate-200">Resumo de Risco</div>
            <RiskTrendBadge trend={dashboard.trend as "increasing" | "decreasing" | "stable"} />
          </div>

          {/* Key Factors */}
          <KeyFactorsList factors={dashboard.keyFactors} maxItems={4} />

          {/* Summary */}
          {dashboard.summary && (
            <p className="mt-3 text-xs text-slate-400 border-t border-slate-700 pt-3">
              {dashboard.summary}
            </p>
          )}

          {/* Confidence */}
          <div className="mt-2 flex items-center justify-between text-xs text-slate-500">
            <span>Confiança da análise</span>
            <span>{(dashboard.confidence * 100).toFixed(0)}%</span>
          </div>
        </div>
      )}

      {/* Weekly Comparison */}
      {comparison && (
        <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4">
          <div className="text-sm font-medium text-slate-200 mb-3">Comparativo Semanal</div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs text-slate-400">Eventos esta semana</span>
              <span className="text-sm font-medium text-white">{comparison.currentWeekEvents}</span>
            </div>

            <div className="flex items-center justify-between">
              <span className="text-xs text-slate-400">Semana anterior</span>
              <span className="text-sm text-slate-300">{comparison.previousWeekEvents}</span>
            </div>

            <div className="flex items-center justify-between">
              <span className="text-xs text-slate-400">Variação</span>
              <RiskTrendBadge
                trend={comparison.weekOverWeekChange > 0 ? "increasing" : comparison.weekOverWeekChange < 0 ? "decreasing" : "stable"}
                percentChange={comparison.weekOverWeekChange}
                size="sm"
              />
            </div>

            <div className="border-t border-slate-700/50 pt-2 mt-2">
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-400">FWI atual vs histórico</span>
                <span className="text-sm text-white">
                  {comparison.currentFwiAverage} / {comparison.historicalFwiAverage}
                </span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Weekly Report Recommendations */}
      {weekly && weekly.strategicRecommendations && weekly.strategicRecommendations.length > 0 && (
        <AigRecommendationAlert
          recommendations={weekly.strategicRecommendations.slice(0, 3)}
          riskLevel={dashboard?.riskLevel ?? "Medium"}
          summary={`Semana ${weekly.weekNumber}/${weekly.year}`}
          collapsible
        />
      )}

      {/* Dashboard Recommendations */}
      {dashboard && dashboard.recommendations && dashboard.recommendations.length > 0 && (
        <AigRecommendationAlert
          recommendations={dashboard.recommendations.slice(0, 3)}
          riskLevel={dashboard.riskLevel}
          collapsible
        />
      )}
    </div>
  );
}
