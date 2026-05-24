"use client";

import Link from "next/link";
import { format } from "date-fns";
import { pt } from "date-fns/locale";
import type { GeoEvent } from "@/lib/types/event";
import { SEVERITY_COLORS, EVENT_TYPE_LABELS } from "@/lib/map/config";

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

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl divide-y divide-slate-700/50">
      <div className="px-4 py-3 border-b border-slate-700">
        <h3 className="text-sm font-semibold text-slate-200">Eventos Recentes</h3>
      </div>
      <ul>
        {events.slice(0, 10).map((event) => (
          <li key={event.id}>
            <Link
              href={`/dashboard/events/${event.id}`}
              className="flex items-center gap-3 px-4 py-3 hover:bg-slate-700/30 transition-colors"
            >
              <span
                className="w-2.5 h-2.5 rounded-full shrink-0"
                style={{ backgroundColor: SEVERITY_COLORS[event.severity] }}
              />
              <div className="flex-1 min-w-0">
                <div className="text-sm text-white truncate">{event.title}</div>
                <div className="text-xs text-slate-500">
                  {EVENT_TYPE_LABELS[event.eventType]} &middot;{" "}
                  {format(new Date(event.occurredAt), "d MMM HH:mm", { locale: pt })}
                </div>
              </div>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}