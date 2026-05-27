"use client";

import type { RiskZone } from "@/lib/types/riskZone";

interface RiskPopupProps {
  zone: RiskZone;
}

const LEVEL_COLORS: Record<string, string> = {
  Critical: "#ef4444",
  High: "#f97316",
  Medium: "#eab308",
  Low: "#22c55e",
};

const LEVEL_LABELS: Record<string, string> = {
  Critical: "Crítico",
  High: "Alto",
  Medium: "Médio",
  Low: "Baixo",
};

export function RiskPopup({ zone }: RiskPopupProps) {
  const levelColor = LEVEL_COLORS[zone.riskLevelName] ?? "#94a3b8";
  const levelLabel = LEVEL_LABELS[zone.riskLevelName] ?? zone.riskLevelName;

  return (
    <div className="min-w-[240px] p-3" style={{ fontFamily: "system-ui, sans-serif" }}>
      <div className="flex items-center gap-2 mb-3">
        <span className="w-3 h-3 rounded-full shrink-0" style={{ backgroundColor: levelColor }} />
        <span className="font-semibold text-sm text-slate-800">{levelLabel}</span>
        <span className="text-xs text-slate-500">FWI {zone.riskIndex}</span>
      </div>
      <div className="grid grid-cols-2 gap-1 text-sm text-slate-700">
        <span>🌡️ Temp:</span>
        <span>{zone.temperature}°C</span>
        <span>💧 Hum:</span>
        <span>{zone.humidity}%</span>
        <span>🌬️ Vento:</span>
        <span>{zone.windSpeed} km/h {zone.windDirection}</span>
      </div>
      <div className="mt-3 pt-3 border-t border-slate-700">
        <p className="text-xs text-slate-300">{zone.conclusion}</p>
      </div>
    </div>
  );
}
