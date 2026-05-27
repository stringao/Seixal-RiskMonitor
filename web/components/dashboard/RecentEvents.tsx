"use client";

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
import type { GeoEvent } from "@/lib/types/event";
import { SEVERITY_COLORS, EVENT_TYPE_LABELS } from "@/lib/map/config";

const SEVERITY_LABELS: Record<string, string> = {
  Low: "Baixo",
  Medium: "Médio",
  High: "Alto",
  Critical: "Crítico",
};

const TYPE_ICONS: Record<string, React.ReactNode> = {
  Fire: <Flame className="w-4 h-4 text-orange-400" />,
  Flood: <Droplets className="w-4 h-4 text-blue-400" />,
  Storm: <CloudLightning className="w-4 h-4 text-yellow-400" />,
  Landslide: <Mountain className="w-4 h-4 text-purple-400" />,
  Industrial: <Factory className="w-4 h-4 text-slate-400" />,
  Heatwave: <Thermometer className="w-4 h-4 text-red-400" />,
  Other: <AlertTriangle className="w-4 h-4 text-slate-400" />,
};

const DISPLAY_LIMIT = 8;

function formatRelativeTime(dateString: string): string {
  return formatDistanceToNow(new Date(dateString), {
    locale: pt,
    addSuffix: true,
  });
}

interface RecentEventsProps {
  events: GeoEvent[];
}

export function RecentEvents({ events }: RecentEventsProps) {
  if (events.length === 0) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 text-center text-slate-500">
        Nenhum evento encontrado
      </div>
    );
  }

  const visibleEvents = events.slice(0, DISPLAY_LIMIT);

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl">
      <div className="px-4 py-3 border-b border-slate-700 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-200">
          Eventos Recentes
        </h3>
        <span className="text-xs bg-slate-700 text-slate-300 rounded-full px-2">
          {events.length}
        </span>
      </div>
      <ul className="divide-y divide-slate-700/50">
        {visibleEvents.map((event) => (
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
                  <span className="text-sm text-white truncate">
                    {event.title}
                  </span>
                  <span
                    className="text-[10px] px-1.5 py-0.5 rounded-full shrink-0 font-medium"
                    style={{
                      backgroundColor:
                        SEVERITY_COLORS[event.severity] + "33",
                      color: SEVERITY_COLORS[event.severity],
                    }}
                  >
                    {SEVERITY_LABELS[event.severity]}
                  </span>
                </div>
                <div className="text-xs text-slate-500 mt-0.5">
                  {EVENT_TYPE_LABELS[event.eventType]} &middot;{" "}
                  {formatRelativeTime(event.occurredAt)}
                </div>
              </div>
            </Link>
          </li>
        ))}
      </ul>
      <Link
        href="/dashboard/events"
        className="block text-center text-sm text-emerald-400 py-2 hover:bg-slate-700/30 transition-colors border-t border-slate-700/50"
      >
        Ver todos
      </Link>
    </div>
  );
}
