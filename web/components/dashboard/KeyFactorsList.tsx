"use client";

import { AlertCircle, Thermometer, Wind, Droplets, Flame } from "lucide-react";
import type { KeyFactorDto } from "@/lib/types/dashboard";

const FACTOR_ICONS: Record<string, React.ReactNode> = {
  FWI: <Flame className="w-4 h-4 text-orange-400" />,
  Temperature: <Thermometer className="w-4 h-4 text-red-400" />,
  Humidity: <Droplets className="w-4 h-4 text-blue-400" />,
  Wind: <Wind className="w-4 h-4 text-slate-400" />,
};

interface KeyFactorsListProps {
  factors: KeyFactorDto[];
  maxItems?: number;
}

export function KeyFactorsList({ factors, maxItems = 5 }: KeyFactorsListProps) {
  if (!factors || factors.length === 0) {
    return (
      <div className="text-sm text-slate-500 italic">Sem fatores de risco identificados</div>
    );
  }

  const displayFactors = factors.slice(0, maxItems);

  return (
    <ul className="space-y-2">
      {displayFactors.map((factor, index) => (
        <li key={index} className="flex items-start gap-2">
          <div className="mt-0.5">
            {FACTOR_ICONS[factor.factor] ?? <AlertCircle className="w-4 h-4 text-yellow-400" />}
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center justify-between gap-2">
              <span className="text-sm font-medium text-slate-200">{factor.factor}</span>
              <span className="text-sm font-semibold text-white tabular-nums">
                {factor.value}
              </span>
            </div>
            <p className="text-xs text-slate-500 mt-0.5 line-clamp-1">
              {factor.description}
            </p>
          </div>
        </li>
      ))}
      {factors.length > maxItems && (
        <li className="text-xs text-slate-500 text-center">
          +{factors.length - maxItems} mais fatores
        </li>
      )}
    </ul>
  );
}

interface KeyFactorsSummaryProps {
  factors: KeyFactorDto[];
}

export function KeyFactorsSummary({ factors }: KeyFactorsSummaryProps) {
  if (!factors || factors.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {factors.slice(0, 4).map((factor, index) => (
        <div
          key={index}
          className="inline-flex items-center gap-1.5 px-2 py-1 bg-slate-800/80 border border-slate-700/50 rounded-lg"
        >
          {FACTOR_ICONS[factor.factor] ?? <AlertCircle className="w-3 h-3 text-yellow-400" />}
          <span className="text-xs text-slate-300">{factor.factor}:</span>
          <span className="text-xs font-medium text-white">{factor.value}</span>
        </div>
      ))}
    </div>
  );
}
