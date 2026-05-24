"use client";

import type { DetectedPattern } from "@/lib/api/insights";

interface PatternCardProps {
  pattern: DetectedPattern;
}

const typeColors: Record<string, string> = {
  temporal: "bg-blue-500/20 text-blue-400 border-blue-500/30",
  spatial: "bg-purple-500/20 text-purple-400 border-purple-500/30",
  sequential: "bg-amber-500/20 text-amber-400 border-amber-500/30",
  severity: "bg-red-500/20 text-red-400 border-red-500/30",
};

export function PatternCard({ pattern }: Readonly<PatternCardProps>) {
  const colorClass = typeColors[pattern.patternType] ?? "bg-slate-500/20 text-slate-400 border-slate-500/30";

  return (
    <div className="bg-slate-800/60 rounded-lg p-5 space-y-3">
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-2">
          <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium border ${colorClass}`}>
            {pattern.patternType}
          </span>
          <span className="text-sm font-semibold text-white">{pattern.title}</span>
        </div>
        <span className="text-xs text-slate-500">
          {Math.round(pattern.confidence * 100)}% confidence
        </span>
      </div>
      <p className="text-sm text-slate-400 leading-relaxed">{pattern.description}</p>
      {pattern.recommendation && (
        <div className="pt-2 border-t border-slate-700">
          <span className="text-xs font-medium text-slate-500 uppercase tracking-wider">Recommendation</span>
          <p className="text-sm text-emerald-400 mt-1">{pattern.recommendation}</p>
        </div>
      )}
      <div className="flex flex-wrap gap-1 pt-1">
        {pattern.affectedEventIds.map((id) => (
          <span key={id} className="inline-flex items-center px-2 py-0.5 rounded text-xs bg-slate-700 text-slate-400">
            {id.slice(0, 8)}...
          </span>
        ))}
      </div>
    </div>
  );
}