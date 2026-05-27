"use client";

import { useCallback } from "react";
import { format, subDays } from "date-fns";
import type { EventFilters, EventType, RiskLevel } from "@/lib/types/event";
import { EVENT_TYPE_LABELS } from "@/lib/map/config";

interface MapFiltersProps {
  filters: EventFilters;
  onChange: (filters: EventFilters) => void;
  showBoundaries?: boolean;
  onBoundaryToggle?: (show: boolean) => void;
  boundaryLevel?: "municipios" | "distritos" | "both";
  onBoundaryLevelChange?: (level: "municipios" | "distritos" | "both") => void;
}

const EVENT_TYPES: EventType[] = ["Fire", "Flood", "Storm", "Landslide", "Industrial", "Heatwave", "Other"];
const SEVERITY_LEVELS: RiskLevel[] = ["Low", "Medium", "High", "Critical"];

const SEVERITY_LABELS: Record<RiskLevel, string> = {
  Low: "Baixo",
  Medium: "Médio",
  High: "Alto",
  Critical: "Crítico",
};

const QUICK_RANGES: { label: string; days: number | null }[] = [
  { label: "Hoje", days: 0 },
  { label: "7 dias", days: 7 },
  { label: "30 dias", days: 30 },
  { label: "90 dias", days: 90 },
  { label: "Tudo", days: null },
];

function toDateInputValue(date: Date): string {
  return format(date, "yyyy-MM-dd");
}

const BOUNDARY_LEVEL_OPTIONS: { value: "municipios" | "distritos" | "both"; label: string }[] = [
  { value: "municipios", label: "Concelhos" },
  { value: "distritos", label: "Distritos" },
  { value: "both", label: "Ambos" },
];

export function MapFilters({
  filters,
  onChange,
  showBoundaries,
  onBoundaryToggle,
  boundaryLevel,
  onBoundaryLevelChange,
}: MapFiltersProps) {
  const update = useCallback(
    (patch: Partial<EventFilters>) => onChange({ ...filters, ...patch }),
    [filters, onChange],
  );

  const handleQuickRange = useCallback(
    (days: number | null) => {
      if (days === null) {
        update({ from: undefined, to: undefined });
        return;
      }
      const today = new Date();
      today.setHours(23, 59, 59, 999);
      const from = days === 0 ? today : subDays(today, days);
      from.setHours(0, 0, 0, 0);
      update({ from: toDateInputValue(from), to: toDateInputValue(today) });
    },
    [update],
  );

  const activeQuickRange = (() => {
    if (!filters.from || !filters.to) return null;
    const today = new Date();
    today.setHours(23, 59, 59, 999);
    const todayStr = toDateInputValue(today);
    if (filters.from === todayStr) return 0;
    if (filters.from === toDateInputValue(subDays(today, 7))) return 7;
    if (filters.from === toDateInputValue(subDays(today, 30))) return 30;
    if (filters.from === toDateInputValue(subDays(today, 90))) return 90;
    return null;
  })();

  return (
    <div className="p-4 space-y-5">
      <h3 className="text-sm font-semibold text-slate-200 uppercase tracking-wider">
        Filtros
      </h3>

      {/* Quick date range */}
      <div>
        <label className="block text-xs font-medium text-slate-400 mb-1.5">
          Período
        </label>
        <div className="grid grid-cols-3 gap-1.5 mb-2">
          {QUICK_RANGES.map((r) => (
            <button
              key={r.label}
              onClick={() => handleQuickRange(r.days)}
              className={`py-1.5 px-2 rounded-lg text-xs font-medium transition-colors ${
                activeQuickRange === r.days
                  ? "bg-emerald-500 text-white"
                  : "bg-slate-700 text-slate-300 hover:bg-slate-600"
              }`}
            >
              {r.label}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="block text-xs font-medium text-slate-400 mb-1.5">
          Tipo de Evento
        </label>
        <select
          value={filters.type ?? ""}
          onChange={(e) =>
            update({ type: (e.target.value || undefined) as EventType | undefined })
          }
          className="w-full bg-slate-700 text-slate-200 rounded-lg px-3 py-2 text-sm border border-slate-600 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
        >
          <option value="">Todos</option>
          {EVENT_TYPES.map((t) => (
            <option key={t} value={t}>
              {EVENT_TYPE_LABELS[t]}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label className="block text-xs font-medium text-slate-400 mb-1.5">
          Severidade
        </label>
        <select
          value={filters.severity ?? ""}
          onChange={(e) =>
            update({ severity: (e.target.value || undefined) as RiskLevel | undefined })
          }
          className="w-full bg-slate-700 text-slate-200 rounded-lg px-3 py-2 text-sm border border-slate-600 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
        >
          <option value="">Todas</option>
          {SEVERITY_LEVELS.map((s) => (
            <option key={s} value={s}>
              {SEVERITY_LABELS[s]}
            </option>
          ))}
        </select>
      </div>

      <div className="border-t border-slate-700 pt-4">
        <label className="block text-xs font-medium text-slate-400 mb-2">
          Camadas do Mapa
        </label>
        <div className="space-y-2">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={showBoundaries ?? false}
              onChange={(e) => onBoundaryToggle?.(e.target.checked)}
              className="w-4 h-4 rounded border-slate-600 bg-slate-700 text-blue-500 focus:ring-blue-500 focus:ring-offset-0"
            />
            <span className="text-sm text-slate-300">Limites Administrativos</span>
          </label>
        </div>

        {/* Boundary level selector - only visible when boundaries are enabled */}
        <div
          className={`mt-3 ml-6 space-y-1.5 transition-all duration-200 ${
            showBoundaries ? "opacity-100" : "opacity-0 pointer-events-none h-0 overflow-hidden"
          }`}
        >
          <div className="text-[10px] uppercase tracking-wider text-slate-500 font-semibold mb-1.5">
            Nível de Limite
          </div>
          {BOUNDARY_LEVEL_OPTIONS.map((opt) => (
            <label key={opt.value} className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="boundaryLevel"
                value={opt.value}
                checked={(boundaryLevel ?? "municipios") === opt.value}
                onChange={() => onBoundaryLevelChange?.(opt.value)}
                className="w-3.5 h-3.5 border-slate-600 bg-slate-700 text-blue-500 focus:ring-blue-500 focus:ring-offset-0"
              />
              <span className="text-xs text-slate-300">{opt.label}</span>
            </label>
          ))}
        </div>
      </div>

      <button
        onClick={() => onChange({})}
        className="w-full py-2 text-sm text-slate-400 hover:text-white transition-colors border border-slate-700 rounded-lg hover:border-slate-500"
      >
        Limpar Filtros
      </button>
    </div>
  );
}
