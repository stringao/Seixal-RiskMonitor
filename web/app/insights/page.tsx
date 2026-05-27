"use client";

import { useState } from "react";
import { useDetectPatterns } from "@/lib/hooks/useInsights";
import { PatternCard } from "@/components/insights/PatternCard";
import { AiQueryBox } from "@/components/insights/AiQueryBox";
import { AiQueryResult } from "@/components/insights/AiQueryResult";
import { useAiQuery } from "@/lib/hooks/useAiQuery";
import { AlertTriangle, BarChart3, TrendingUp, MessageSquare, Zap } from "lucide-react";

type TabType = "patterns" | "assistant";

export default function InsightsPage() {
  const [activeTab, setActiveTab] = useState<TabType>("patterns");
  const { data, loading, error } = useDetectPatterns();
  const { data: queryData, loading: queryLoading, error: queryError, query } = useAiQuery();

  const handleQuery = async (question: string, contextType: string) => {
    await query(question, contextType);
  };

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

      {/* Tab Navigation */}
      <div className="flex gap-1 p-1 bg-slate-800/40 rounded-lg w-fit">
        <button
          onClick={() => setActiveTab("patterns")}
          className={`flex items-center gap-2 px-4 py-2 rounded-md text-sm font-medium transition-colors ${
            activeTab === "patterns"
              ? "bg-emerald-500/20 text-emerald-400"
              : "text-slate-400 hover:text-white"
          }`}
        >
          <BarChart3 className="w-4 h-4" />
          Padrões
        </button>
        <button
          onClick={() => setActiveTab("assistant")}
          className={`flex items-center gap-2 px-4 py-2 rounded-md text-sm font-medium transition-colors ${
            activeTab === "assistant"
              ? "bg-emerald-500/20 text-emerald-400"
              : "text-slate-400 hover:text-white"
          }`}
        >
          <MessageSquare className="w-4 h-4" />
          Assistente
        </button>
      </div>

      {/* Tab Content */}
      {activeTab === "patterns" && (
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
                      Para ativar a deteção de padrões, configure uma API key nas{" "}
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
              <Zap className="w-5 h-5 text-emerald-400" />
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
      )}

      {activeTab === "assistant" && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-4">
            <div className="flex items-center gap-2">
              <MessageSquare className="w-5 h-5 text-emerald-400" />
              <h2 className="text-lg font-semibold text-white">Assistente de Análise</h2>
            </div>

            {/* Query Input */}
            <div className="bg-slate-800/60 rounded-xl p-6">
              <AiQueryBox onSubmit={handleQuery} isLoading={queryLoading} />
            </div>

            {/* Query Result */}
            <AiQueryResult
              answer={queryData?.answer ?? null}
              sources={queryData?.sources ?? []}
              confidence={queryData?.confidence}
              language={queryData?.language}
              isLoading={queryLoading}
              error={queryError}
            />

            {/* Suggested Questions */}
            <div className="bg-slate-800/40 rounded-lg p-4">
              <h3 className="text-sm font-medium text-slate-400 mb-3">Perguntas sugeridas:</h3>
              <div className="flex flex-wrap gap-2">
                {[
                  "Qual foi o maior incêndio na região de Setúbal?",
                  "Quais são os hotspots mais ativos este verão?",
                  "Como está o risco meteorológico atual?",
                  "Quantos incêndios ocorreram este mês?",
                ].map((q, i) => (
                  <button
                    key={i}
                    onClick={() => handleQuery(q, "full")}
                    disabled={queryLoading}
                    className="text-xs px-3 py-1.5 rounded-full bg-slate-700/50 text-slate-300 hover:bg-slate-700 hover:text-white transition-colors disabled:opacity-50"
                  >
                    {q}
                  </button>
                ))}
              </div>
            </div>
          </div>

          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <Zap className="w-5 h-5 text-emerald-400" />
              <h2 className="text-lg font-semibold text-white">Tipos de Análise</h2>
            </div>
            <div className="bg-slate-800/60 rounded-lg p-4 space-y-3">
              <div className="p-3 rounded-lg bg-slate-700/30">
                <h4 className="text-sm font-medium text-white mb-1">Análise Regional</h4>
                <p className="text-xs text-slate-400">Dados sobre uma região específica de Portugal</p>
              </div>
              <div className="p-3 rounded-lg bg-slate-700/30">
                <h4 className="text-sm font-medium text-white mb-1">Análise Sazonal</h4>
                <p className="text-xs text-slate-400">Estatísticas históricas por estação/ano</p>
              </div>
              <div className="p-3 rounded-lg bg-slate-700/30">
                <h4 className="text-sm font-medium text-white mb-1">Comparação de Eventos</h4>
                <p className="text-xs text-slate-400">Comparar com eventos históricos similares</p>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}