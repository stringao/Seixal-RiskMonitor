"use client";

interface ClassificationBadgeProps {
  classification: string;
  confidence: number;
  reasoning?: string;
}

export function ClassificationBadge({ classification, confidence, reasoning }: ClassificationBadgeProps) {
  const confidencePercent = Math.round(confidence * 100);
  const confidenceColor = confidence >= 0.8 ? "text-emerald-400" : confidence >= 0.5 ? "text-amber-400" : "text-slate-400";

  return (
    <div className="bg-slate-800/60 rounded-lg p-4 space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-xs font-medium text-slate-400 uppercase tracking-wider">AI Classification</span>
        <span className={`text-xs font-semibold ${confidenceColor}`}>{confidencePercent}% confidence</span>
      </div>
      <div className="flex items-center gap-2">
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
          {classification}
        </span>
      </div>
      {reasoning && (
        <p className="text-sm text-slate-400 leading-relaxed">{reasoning}</p>
      )}
    </div>
  );
}