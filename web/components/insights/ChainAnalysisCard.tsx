"use client";

import type { EventChain } from "@/lib/api/insights";

interface ChainAnalysisCardProps {
  chain: EventChain;
  onClick?: (chain: EventChain) => void;
}

const TYPE_LABELS: Record<string, string> = {
  Simultaneous: "Fogos Simultâneos",
  Sequential: "Correlação Sequencial",
  ResourceContention: "Competição de Recursos",
  EmberCast: "Ignição por Brasas",
};

const TYPE_COLORS: Record<string, string> = {
  Simultaneous: "bg-blue-500/20 text-blue-400 border-blue-500/30",
  Sequential: "bg-amber-500/20 text-amber-400 border-amber-500/30",
  ResourceContention: "bg-red-500/20 text-red-400 border-red-500/30",
  EmberCast: "bg-emerald-500/20 text-emerald-400 border-emerald-500/30",
};

function ConfidenceBadge({ confidence }: { confidence: number }) {
  const pct = Math.round(confidence * 100);
  const colorClass =
    pct >= 80
      ? "bg-emerald-500/20 text-emerald-400 border-emerald-500/30"
      : pct >= 50
      ? "bg-amber-500/20 text-amber-400 border-amber-500/30"
      : "bg-red-500/20 text-red-400 border-red-500/30";

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium border ${colorClass}`}>
      {pct}% confiança
    </span>
  );
}

export function ChainAnalysisCard({ chain, onClick }: Readonly<ChainAnalysisCardProps>) {
  const colorClass = TYPE_COLORS[chain.analysisType] ?? "bg-slate-500/20 text-slate-400 border-slate-500/30";
  const typeLabel = TYPE_LABELS[chain.analysisType] ?? chain.analysisType;

  return (
    <div
      className="bg-slate-800/60 rounded-lg p-5 space-y-3 cursor-pointer hover:bg-slate-800/80 transition-colors"
      onClick={() => onClick?.(chain)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => e.key === "Enter" && onClick?.(chain)}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-2">
          <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium border ${colorClass}`}>
            {typeLabel}
          </span>
        </div>
        <ConfidenceBadge confidence={chain.confidenceScore} />
      </div>

      <p className="text-sm text-slate-300 leading-relaxed">{chain.description}</p>

      {chain.contributingFactors && chain.contributingFactors.length > 0 && (
        <div className="flex flex-wrap gap-1">
          {chain.contributingFactors.slice(0, 4).map((factor, index) => (
            <span
              key={index}
              className="inline-flex items-center px-2 py-0.5 rounded text-xs bg-slate-700/50 text-slate-400"
            >
              {factor}
            </span>
          ))}
          {chain.contributingFactors.length > 4 && (
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs bg-slate-700/50 text-slate-500">
              +{chain.contributingFactors.length - 4}
            </span>
          )}
        </div>
      )}

      {chain.recommendedAction && (
        <div className="pt-2 border-t border-slate-700">
          <span className="text-xs font-medium text-slate-500 uppercase tracking-wider">Recomendação</span>
          <p className="text-sm text-emerald-400 mt-1">{chain.recommendedAction}</p>
        </div>
      )}

      <div className="pt-2 flex items-center justify-between text-xs text-slate-500">
        <span>ID: {chain.id.slice(0, 8)}...</span>
        <span>
          {new Date(chain.analyzedAt).toLocaleString("pt-PT", {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
          })}
        </span>
      </div>
    </div>
  );
}
