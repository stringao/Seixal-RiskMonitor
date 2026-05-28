"use client";

import { Wind, TrendingUp } from "lucide-react";
import type { AirQualityResponse } from "@/lib/types/environment";

interface AirQualityCardProps {
  data: AirQualityResponse | null;
  loading?: boolean;
}

export function AirQualityCard({ data, loading }: AirQualityCardProps) {
  if (loading) {
    return (
      <div className="bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-5">
        <div className="flex items-center gap-2 mb-4">
          <div className="w-5 h-5 bg-slate-700 rounded animate-pulse" />
          <div className="h-5 w-32 bg-slate-700 rounded animate-pulse" />
        </div>
        <div className="h-24 bg-slate-700/60 rounded-xl animate-pulse mb-4" />
        <div className="space-y-3">
          {[...Array(4)].map((_, i) => (
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
          <Wind className="w-5 h-5 text-emerald-400" />
          <span className="text-sm font-medium text-slate-300">Qualidade do Ar</span>
        </div>
        <p className="text-slate-400 text-sm">Dados não disponíveis</p>
      </div>
    );
  }

  const aqiLevel = data.aqiLevel;
  if (!aqiLevel) {
    return (
      <div className="bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-5">
        <div className="flex items-center gap-2 mb-4">
          <Wind className="w-5 h-5 text-emerald-400" />
          <span className="text-sm font-medium text-slate-300">Qualidade do Ar</span>
        </div>
        <p className="text-slate-400 text-sm">Dados não disponíveis</p>
      </div>
    );
  }
  const aqiDots = Array.from({ length: 5 }, (_, i) => i < data.aqi);

  return (
    <div className="bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-5">
      {/* Header */}
      <div className="flex items-center gap-2 mb-4">
        <Wind className="w-5 h-5 text-emerald-400" />
        <span className="text-sm font-medium text-slate-300">Qualidade do Ar</span>
      </div>

      {/* AQI Display */}
      <div
        className="bg-slate-900/60 rounded-xl p-4 mb-4 border border-slate-700/50"
        style={{ borderLeftColor: aqiLevel.color, borderLeftWidth: "3px" }}
      >
        <div className="flex items-center justify-between mb-2">
          <span className="text-2xl font-bold text-white">{aqiLevel.label}</span>
          <span className="text-sm text-slate-400">
            ({data.pollutants.find(p => p.symbol === "O3")?.symbol ?? "O3"})
          </span>
        </div>
        <div className="flex items-center gap-2">
          <div className="flex gap-1">
            {aqiDots.map((filled, i) => (
              <div
                key={i}
                className={`w-3 h-3 rounded-full ${
                  filled ? "" : "bg-slate-700"
                }`}
                style={{ backgroundColor: filled ? aqiLevel.color : undefined }}
              />
            ))}
          </div>
          <span className="text-xs text-slate-400">{data.aqi}/5</span>
        </div>
      </div>

      {/* Pollutants List */}
      <div className="space-y-2">
        {data.pollutants.slice(0, 4).map((pollutant) => (
          <div
            key={pollutant.symbol}
            className="flex items-center gap-3 text-sm"
          >
            <span className="w-14 text-slate-400 font-medium">
              {pollutant.symbol}
            </span>
            <span className="w-20 text-slate-300">
              {pollutant.value} {pollutant.unit}
            </span>
            <div className="flex-1 h-1.5 bg-slate-700 rounded-full overflow-hidden">
              <div
                className="h-full rounded-full"
                style={{
                  width: `${Math.min((pollutant.value / 200) * 100, 100)}%`,
                  backgroundColor: pollutant.color,
                }}
              />
            </div>
            <span
              className="text-xs w-12 text-right"
              style={{ color: pollutant.color }}
            >
              {pollutant.level}
            </span>
          </div>
        ))}
      </div>

      {/* Health Risk */}
      {data.healthRisk && (
        <div className="mt-4 pt-3 border-t border-slate-700/50 flex items-center gap-2">
          <TrendingUp className="w-4 h-4 text-amber-400" />
          <span className="text-xs text-slate-400">Risco saúde:</span>
          <span className="text-xs font-medium text-amber-400">{data.healthRisk}</span>
        </div>
      )}
    </div>
  );
}