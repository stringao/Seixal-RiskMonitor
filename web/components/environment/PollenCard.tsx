"use client";

import { Flower2, TrendingUp, TrendingDown, Minus } from "lucide-react";
import type { PollenResponse } from "@/lib/types/environment";

interface PollenCardProps {
  data: PollenResponse | null;
  loading?: boolean;
}

const TREND_ICONS = {
  rising: <TrendingUp className="w-3 h-3 text-red-400" />,
  falling: <TrendingDown className="w-3 h-3 text-green-400" />,
  stable: <Minus className="w-3 h-3 text-slate-400" />,
};

export function PollenCard({ data, loading }: PollenCardProps) {
  if (loading) {
    return (
      <div className="bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-5">
        <div className="flex items-center gap-2 mb-4">
          <div className="w-5 h-5 bg-slate-700 rounded animate-pulse" />
          <div className="h-5 w-24 bg-slate-700 rounded animate-pulse" />
        </div>
        <div className="h-20 bg-slate-700/60 rounded-xl animate-pulse mb-4" />
        <div className="space-y-3">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="h-8 bg-slate-700/60 rounded-lg animate-pulse" />
          ))}
        </div>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-5">
        <div className="flex items-center gap-2 mb-4">
          <Flower2 className="w-5 h-5 text-emerald-400" />
          <span className="text-sm font-medium text-slate-300">Polén</span>
        </div>
        <p className="text-slate-400 text-sm">Dados não disponíveis</p>
      </div>
    );
  }

  const levelColors: Record<string, string> = {
    Low: "#00E400",
    Moderate: "#FFFF00",
    High: "#FF7E00",
    VeryHigh: "#FF0000",
  };
  const levelColor = levelColors[data.level] ?? "#FFFF00";

  return (
    <div className="bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-5">
      {/* Header */}
      <div className="flex items-center gap-2 mb-4">
        <Flower2 className="w-5 h-5 text-emerald-400" />
        <span className="text-sm font-medium text-slate-300">Polén</span>
      </div>

      {/* Pollen Index Display */}
      <div
        className="bg-slate-900/60 rounded-xl p-4 mb-4 border border-slate-700/50"
        style={{ borderLeftColor: levelColor, borderLeftWidth: "3px" }}
      >
        <div className="flex items-center justify-between mb-2">
          <span className="text-2xl font-bold text-white">{data.label}</span>
          <span className="text-sm text-slate-400">
            ({data.pollenIndex}/5)
          </span>
        </div>
        <div className="flex gap-2 flex-wrap">
          {data.breakdown.slice(0, 3).map((species) => (
            <div key={species.name} className="flex items-center gap-1 text-xs">
              <span className="text-slate-300">{species.name}</span>
              {species.trend && TREND_ICONS[species.trend]}
            </div>
          ))}
        </div>
      </div>

      {/* Species Breakdown */}
      <div className="space-y-2">
        {data.breakdown.map((species) => {
          const color = levelColors[species.level] ?? "#FFFF00";
          return (
            <div key={species.name} className="flex items-center gap-3 text-sm">
              <span className="w-20 text-slate-400 font-medium">{species.name}</span>
              <span className="w-24 text-slate-300">
                {species.value} grains/m³
              </span>
              <div className="flex-1 h-1.5 bg-slate-700 rounded-full overflow-hidden">
                <div
                  className="h-full rounded-full"
                  style={{
                    width: `${Math.min((species.value / 100) * 100, 100)}%`,
                    backgroundColor: color,
                  }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}