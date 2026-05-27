"use client";

import type { FireHotspot } from "@/lib/types/hotspot";

interface HotspotCardProps {
  hotspot: FireHotspot;
  compact?: boolean;
}

const RISK_COLORS: Record<string, string> = {
  Critical: "bg-red-500/20 text-red-400 border-red-500/30",
  High: "bg-orange-500/20 text-orange-400 border-orange-500/30",
  Medium: "bg-yellow-500/20 text-yellow-400 border-yellow-500/30",
  Low: "bg-green-500/20 text-green-400 border-green-500/30",
};

const RISK_BG_COLORS: Record<string, string> = {
  Critical: "#ef4444",
  High: "#f97316",
  Medium: "#eab308",
  Low: "#22c55e",
};

const MONTH_NAMES = [
  "Jan", "Fev", "Mar", "Abr", "Mai", "Jun",
  "Jul", "Ago", "Set", "Out", "Nov", "Dez"
];

export function HotspotCard({ hotspot, compact = false }: HotspotCardProps) {
  const riskColor = RISK_COLORS[hotspot.riskLevel] ?? "bg-slate-500/20 text-slate-400";
  const bgColor = RISK_BG_COLORS[hotspot.riskLevel] ?? "#94a3b8";
  const peakMonthLabel = MONTH_NAMES[hotspot.peakMonth - 1] ?? "?";

  if (compact) {
    return (
      <div className="bg-slate-800/60 rounded-lg p-3 border border-slate-700 hover:border-slate-600 transition-colors">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <span
              className="w-2 h-2 rounded-full"
              style={{ backgroundColor: bgColor }}
            />
            <span className="text-sm font-medium text-white">{hotspot.name}</span>
          </div>
          <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium border ${riskColor}`}>
            {hotspot.fireCount} fogos
          </span>
        </div>
        <div className="mt-2 flex items-center gap-4 text-xs text-slate-400">
          <span>Pico: {peakMonthLabel} {hotspot.peakHour}h</span>
          {hotspot.commonWindDirection && <span>Vento: {hotspot.commonWindDirection}</span>}
        </div>
      </div>
    );
  }

  return (
    <div className="bg-slate-800/60 rounded-lg p-5 border border-slate-700">
      <div className="flex items-start justify-between gap-3 mb-4">
        <div className="flex items-center gap-3">
          <span
            className="w-4 h-4 rounded-full"
            style={{ backgroundColor: bgColor }}
          />
          <div>
            <h3 className="text-base font-semibold text-white">{hotspot.name}</h3>
            <p className="text-xs text-slate-500 mt-0.5">
              Cell: {hotspot.gridCellId}
            </p>
          </div>
        </div>
        <span className={`inline-flex items-center px-2.5 py-1 rounded text-xs font-medium border ${riskColor}`}>
          {hotspot.riskLevel === "Critical" ? "Crítico" :
           hotspot.riskLevel === "High" ? "Alto" :
           hotspot.riskLevel === "Medium" ? "Médio" : "Baixo"}
        </span>
      </div>

      <div className="grid grid-cols-3 gap-4 mb-4">
        <div className="text-center">
          <div className="text-xl font-bold text-white">{hotspot.fireCount}</div>
          <div className="text-xs text-slate-400">Incêndios totais</div>
        </div>
        <div className="text-center">
          <div className="text-xl font-bold text-white">{hotspot.totalAreaBurned.toFixed(1)}</div>
          <div className="text-xs text-slate-400">Hectares</div>
        </div>
        <div className="text-center">
          <div className="text-xl font-bold text-white">{hotspot.averageFwi.toFixed(0)}</div>
          <div className="text-xs text-slate-400">FWI médio</div>
        </div>
      </div>

      <div className="border-t border-slate-700 pt-4">
        <h4 className="text-xs font-medium text-slate-500 uppercase tracking-wider mb-3">Padrão Temporal</h4>
        <div className="grid grid-cols-2 gap-4">
          <div className="flex items-center gap-2">
            <span className="text-lg">📅</span>
            <div>
              <div className="text-sm font-medium text-white">
                {peakMonthLabel}
              </div>
              <div className="text-xs text-slate-400">Mês de pico</div>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-lg">🕐</span>
            <div>
              <div className="text-sm font-medium text-white">
                {hotspot.peakHour}:00
              </div>
              <div className="text-xs text-slate-400">Hora de pico</div>
            </div>
          </div>
        </div>
      </div>

      {hotspot.commonWindDirection && (
        <div className="mt-3 pt-3 border-t border-slate-700">
          <div className="flex items-center gap-2">
            <span className="text-lg">🌬️</span>
            <div>
              <div className="text-sm font-medium text-white">Vento {hotspot.commonWindDirection}</div>
              <div className="text-xs text-slate-400">Direção predominante</div>
            </div>
          </div>
        </div>
      )}

      <div className="mt-4 pt-4 border-t border-slate-700 text-xs text-slate-500">
        Atualizado: {new Date(hotspot.lastUpdated).toLocaleDateString("pt-PT")}
      </div>
    </div>
  );
}