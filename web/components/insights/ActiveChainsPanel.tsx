"use client";

import { useState } from "react";
import { ChainAnalysisCard } from "./ChainAnalysisCard";
import type { EventChain } from "@/lib/api/insights";

interface ActiveChainsPanelProps {
  chains: EventChain[];
  onChainClick?: (chain: EventChain) => void;
  onRefresh?: () => void;
}

const TYPE_COUNTS_INITIAL = {
  Simultaneous: 0,
  Sequential: 0,
  ResourceContention: 0,
  EmberCast: 0,
};

export function ActiveChainsPanel({ chains, onChainClick, onRefresh }: Readonly<ActiveChainsPanelProps>) {
  const [filter, setFilter] = useState<string | null>(null);
  const [showOnlyHighConfidence, setShowOnlyHighConfidence] = useState(false);

  const typeCounts = chains.reduce(
    (acc, chain) => {
      if (chain.analysisType in acc) {
        acc[chain.analysisType as keyof typeof acc]++;
      }
      return acc;
    },
    { ...TYPE_COUNTS_INITIAL }
  );

  const filteredChains = chains.filter((chain) => {
    if (filter && chain.analysisType !== filter) return false;
    if (showOnlyHighConfidence && chain.confidenceScore < 0.7) return false;
    return true;
  });

  const highConfidenceCount = chains.filter((c) => c.confidenceScore >= 0.7).length;

  return (
    <div className="bg-slate-900/80 backdrop-blur rounded-lg border border-slate-700/50 overflow-hidden">
      {/* Header */}
      <div className="p-4 border-b border-slate-700/50">
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-lg font-semibold text-white">Cadeias de Eventos Ativas</h3>
          <div className="flex items-center gap-2">
            {highConfidenceCount > 0 && (
              <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-500/20 text-red-400 border border-red-500/30">
                {highConfidenceCount} alta confiança
              </span>
            )}
            {onRefresh && (
              <button
                onClick={onRefresh}
                className="p-1.5 rounded hover:bg-slate-700/50 text-slate-400 transition-colors"
                title="Atualizar"
              >
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
              </button>
            )}
          </div>
        </div>

        {/* Type filter pills */}
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setFilter(null)}
            className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
              filter === null
                ? "bg-slate-600 text-white"
                : "bg-slate-800 text-slate-400 hover:bg-slate-700"
            }`}
          >
            Todos ({chains.length})
          </button>
          {Object.entries(typeCounts).map(([type, count]) =>
            count > 0 ? (
              <button
                key={type}
                onClick={() => setFilter(type)}
                className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
                  filter === type
                    ? "bg-blue-600 text-white"
                    : "bg-slate-800 text-slate-400 hover:bg-slate-700"
                }`}
              >
                {type} ({count})
              </button>
            ) : null
          )}
        </div>

        {/* High confidence toggle */}
        <div className="mt-3 flex items-center gap-2">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={showOnlyHighConfidence}
              onChange={(e) => setShowOnlyHighConfidence(e.target.checked)}
              className="w-4 h-4 rounded border-slate-600 bg-slate-800 text-blue-600 focus:ring-blue-500 focus:ring-offset-slate-900"
            />
            <span className="text-xs text-slate-400">Apenas alta confiança (≥70%)</span>
          </label>
        </div>
      </div>

      {/* Chain list */}
      <div className="max-h-[500px] overflow-y-auto p-4 space-y-3">
        {filteredChains.length === 0 ? (
          <div className="text-center py-8">
            <div className="inline-flex items-center justify-center w-12 h-12 rounded-full bg-slate-800/50 mb-3">
              <svg className="w-6 h-6 text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
              </svg>
            </div>
            <p className="text-slate-400 text-sm">
              {chains.length === 0
                ? "Nenhuma cadeia de eventos detetada"
                : "Nenhuma cadeia corresponde aos filtros"}
            </p>
          </div>
        ) : (
          filteredChains.map((chain) => (
            <ChainAnalysisCard key={chain.id} chain={chain} onClick={onChainClick} />
          ))
        )}
      </div>

      {/* Footer */}
      {chains.length > 0 && (
        <div className="px-4 py-3 border-t border-slate-700/50 bg-slate-800/30">
          <p className="text-xs text-slate-500 text-center">
            {filteredChains.length} de {chains.length} cadeias mostradas
          </p>
        </div>
      )}
    </div>
  );
}
