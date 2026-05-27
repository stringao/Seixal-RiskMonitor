"use client";

import { useState } from "react";
import Link from "next/link";
import { formatDistanceToNow } from "date-fns";
import { pt } from "date-fns/locale";
import {
  Flame,
  Droplets,
  CloudLightning,
  Mountain,
  Factory,
  Thermometer,
  AlertTriangle,
} from "lucide-react";
import { useEvents } from "@/lib/hooks/useEvents";
import { SEVERITY_COLORS, EVENT_TYPE_LABELS } from "@/lib/map/config";
import type { EventType, RiskLevel } from "@/lib/types/event";

const TYPE_ICONS: Record<string, React.ReactNode> = {
  Fire: <Flame className="w-4 h-4 text-orange-400" />,
  Flood: <Droplets className="w-4 h-4 text-blue-400" />,
  Storm: <CloudLightning className="w-4 h-4 text-yellow-400" />,
  Landslide: <Mountain className="w-4 h-4 text-purple-400" />,
  Industrial: <Factory className="w-4 h-4 text-slate-400" />,
  Heatwave: <Thermometer className="w-4 h-4 text-red-400" />,
  Other: <AlertTriangle className="w-4 h-4 text-slate-400" />,
};

const SEVERITY_LABELS: Record<string, string> = {
  Low: "Baixo",
  Medium: "Médio",
  High: "Alto",
  Critical: "Crítico",
};

export default function EventsPage() {
  const [typeFilter, setTypeFilter] = useState<EventType | "">("");
  const [severityFilter, setSeverityFilter] = useState<RiskLevel | "">("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, loading } = useEvents({
    type: typeFilter || undefined,
    severity: severityFilter || undefined,
    page,
    pageSize,
  });

  const events = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-xl font-bold text-white">Eventos</h2>
        <p className="text-xs text-slate-500 mt-1">{totalCount} eventos registados</p>
      </div>

      <div className="flex items-center gap-4 flex-wrap">
        <select
          value={typeFilter}
          onChange={(e) => {
            setTypeFilter(e.target.value as EventType);
            setPage(1);
          }}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-emerald-500"
        >
          <option value="">Todos os tipos</option>
          <option value="Fire">Fire</option>
          <option value="Flood">Flood</option>
          <option value="Storm">Storm</option>
          <option value="Landslide">Landslide</option>
          <option value="Industrial">Industrial</option>
          <option value="Heatwave">Heatwave</option>
          <option value="Other">Other</option>
        </select>

        <select
          value={severityFilter}
          onChange={(e) => {
            setSeverityFilter(e.target.value as RiskLevel);
            setPage(1);
          }}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-emerald-500"
        >
          <option value="">Todas severidades</option>
          <option value="Low">Low</option>
          <option value="Medium">Medium</option>
          <option value="High">High</option>
          <option value="Critical">Critical</option>
        </select>
      </div>

      {loading ? (
        <div className="space-y-3">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-16 bg-slate-800/60 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : events.length === 0 ? (
        <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-8 text-center text-slate-500">
          Nenhum evento encontrado
        </div>
      ) : (
        <>
          <div className="bg-slate-800/60 border border-slate-700 rounded-xl overflow-hidden">
            <ul className="divide-y divide-slate-700/50">
              {events.map((event) => (
                <li key={event.id}>
                  <Link
                    href={`/dashboard/events/${event.id}`}
                    className="flex items-center gap-3 px-4 py-3 hover:bg-slate-700/30 transition-colors"
                  >
                    <span className="shrink-0">
                      {TYPE_ICONS[event.eventType] ?? TYPE_ICONS.Other}
                    </span>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm text-white truncate">{event.title}</span>
                        <span
                          className="text-[10px] px-1.5 py-0.5 rounded-full shrink-0 font-medium"
                          style={{
                            backgroundColor: SEVERITY_COLORS[event.severity] + "33",
                            color: SEVERITY_COLORS[event.severity],
                          }}
                        >
                          {SEVERITY_LABELS[event.severity]}
                        </span>
                      </div>
                      <div className="text-xs text-slate-500 mt-0.5">
                        {EVENT_TYPE_LABELS[event.eventType]} &middot;{" "}
                        {formatDistanceToNow(new Date(event.occurredAt), {
                          locale: pt,
                          addSuffix: true,
                        })}
                      </div>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1.5 bg-slate-800 border border-slate-700 rounded-lg text-sm text-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-700 transition-colors"
              >
                Anterior
              </button>
              <span className="text-sm text-slate-400">
                Página {page} de {totalPages}
              </span>
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="px-3 py-1.5 bg-slate-800 border border-slate-700 rounded-lg text-sm text-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-700 transition-colors"
              >
                Próxima
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
