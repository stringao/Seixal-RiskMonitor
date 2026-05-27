"use client";

import { useDetectPatterns } from "@/lib/hooks/useInsights";
import { PatternCard } from "@/components/insights/PatternCard";
import { AlertTriangle, BarChart3, TrendingUp } from "lucide-react";

export default function InsightsPage() {
  const { data, loading, error } = useDetectPatterns();

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Insights AI</h1>
          <p className="text-sm text-slate-400 mt-1">Deteção de padrões e análise de risco alimentadas por IA</p>
        </div>
        <div className="flex items-center gap-2 text-xs text-slate-500">
          <TrendingUp className="w-4 h-4" />
          <span>Últimos 7 dias</span>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-4">
          <div className="flex items-center gap-2">
            <BarChart3 className="w-5 h-5 text-emerald-400" />
            <h2 className="text-lg font-semibold text-white">Padrões Detetados</h2>
          </div>

          {loading && (
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="bg-slate-800/60 rounded-lg p-5 animate-pulse">
                  <div className="h-4 w-32 bg-slate-700/60 rounded mb-3" />
                  <div className="h-3 w-full bg-slate-700/60 rounded mb-2" />
                  <div className="h-3 w-5/6 bg-slate-700/60 rounded" />
                </div>
              ))}
            </div>
          )}

          {error && (
            <div className="bg-slate-800/60 border border-slate-700 rounded-lg p-6 text-center">
              {error.includes("not configured") || error.includes("API key") || error.includes("401") || error.includes("403") ? (
                <>
                  <AlertTriangle className="w-8 h-8 text-yellow-400 mx-auto mb-3" />
                  <p className="text-slate-300 font-medium mb-2">Funcionalidades de IA não configuradas</p>
                  <p className="text-sm text-slate-400 mb-4">
                    Para ativar a deteção de padrões, configure uma API key nas{' '}
                    <a href="/settings" className="text-emerald-400 hover:underline">
                      Definições
                    </a>
                    .
                  </p>
                </>
              ) : (
                <>
                  <p className="text-red-400">Falha ao carregar padrões: {error}</p>
                </>
              )}
            </div>
          )}

          {!loading && !error && data && data.patterns.length === 0 && (
            <div className="bg-slate-800/60 rounded-xl p-8 text-center">
              <AlertTriangle className="w-8 h-8 text-slate-600 mx-auto mb-3" />
              <p className="text-slate-500">Nenhum padrão detetado nos últimos 7 dias.</p>
            </div>
          )}

          {!loading && !error && data && data.patterns.length > 0 && (
            <div className="space-y-4">
              {data.patterns.map((pattern, idx) => (
                <PatternCard key={idx} pattern={pattern} />
              ))}
            </div>
          )}
        </div>

        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <TrendingUp className="w-5 h-5 text-emerald-400" />
            <h2 className="text-lg font-semibold text-white">Ações Rápidas</h2>
          </div>
          <div className="bg-slate-800/60 rounded-lg p-4 space-y-3">
            <a
              href="/insights/report"
              className="flex items-center gap-3 p-3 rounded-lg bg-slate-700/50 hover:bg-slate-700 transition-colors"
            >
              <BarChart3 className="w-4 h-4 text-emerald-400" />
              <span className="text-sm text-slate-200">Gerar Relatório</span>
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}
