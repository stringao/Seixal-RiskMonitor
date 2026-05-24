"use client";

import { useCallback } from "react";
import type { EventFilters, EventType, RiskLevel } from "@/lib/types/event";
import { EVENT_TYPE_LABELS } from "@/lib/map/config";

interface MapFiltersProps {
  filters: EventFilters;
  onChange: (filters: EventFilters) => void;
}

const EVENT_TYPES: EventType[] = ["Fire", "Flood", "Storm", "Landslide", "Industrial", "Heatwave", "Other"];
const SEVERITY_LEVELS: RiskLevel[] = ["Low", "Medium", "High", "Critical"];

export function MapFilters({ filters, onChange }: MapFiltersProps) {
  const update = useCallback(
    (patch: Partial<EventFilters>) => onChange({ ...filters, ...patch }),
    [filters, onChange],
  );

  return (
    <div className="p-4 space-y-5">
      <h3 className="text-sm font-semibold text-slate-200 uppercase tracking-wider">Filtros</h3>

      <div>
        <label className="block text-xs font-medium text-slate-400 mb-1.5">Tipo de Evento</label>
        <select
          value={filters.type ?? ""}
          onChange={(e) => update({ type: (e.target.value || undefined) as EventType | undefined })}
          className="w-full bg-slate-700 text-slate-200 rounded-lg px-3 py-2 text-sm border border-slate-600 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
        >
          <option value="">Todos</option>
          {EVENT_TYPES.map((t) => (
            <option key={t} value={t}>{EVENT_TYPE_LABELS[t]}</option>
          ))}
        </select>
      </div>

      <div>
        <label className="block text-xs font-medium text-slate-400 mb-1.5">Severidade</label>
        <select
          value={filters.severity ?? ""}
          onChange={(e) => update({ severity: (e.target.value || undefined) as RiskLevel | undefined })}
          className="w-full bg-slate-700 text-slate-200 rounded-lg px-3 py-2 text-sm border border-slate-600 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
        >
          <option value="">Todas</option>
          {SEVERITY_LEVELS.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </div>

      <div>
        <label className="block text-xs font-medium text-slate-400 mb-1.5">Data Início</label>
        <input
          type="date"
          value={filters.from ?? ""}
          onChange={(e) => update({ from: e.target.value || undefined })}
          className="w-full bg-slate-700 text-slate-200 rounded-lg px-3 py-2 text-sm border border-slate-600 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
        />
      </div>

      <div>
        <label className="block text-xs font-medium text-slate-400 mb-1.5">Data Fim</label>
        <input
          type="date"
          value={filters.to ?? ""}
          onChange={(e) => update({ to: e.target.value || undefined })}
          className="w-full bg-slate-700 text-slate-200 rounded-lg px-3 py-2 text-sm border border-slate-600 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
        />
      </div>

      <button
        onClick={() => onChange({})}
        className="w-full py-2 text-sm text-slate-400 hover:text-white transition-colors"
      >
        Limpar Filtros
      </button>
    </div>
  );
}