"use client";

import { Marker, Popup } from "react-leaflet";
import L from "leaflet";
import { SEVERITY_COLORS, EVENT_TYPE_LABELS } from "@/lib/map/config";
import { format } from "date-fns";
import { pt } from "date-fns/locale";
import type { GeoEvent } from "@/lib/types/event";

function createSeverityIcon(severity: string): L.DivIcon {
  const color = SEVERITY_COLORS[severity] ?? "#94a3b8";
  return L.divIcon({
    className: "custom-marker",
    html: `<div style="
      background-color: ${color};
      width: 14px; height: 14px;
      border-radius: 50%;
      border: 2px solid white;
      box-shadow: 0 0 6px ${color}80;
    "></div>`,
    iconSize: [14, 14],
    iconAnchor: [7, 7],
  });
}

interface EventMarkerProps {
  event: GeoEvent;
}

export function EventMarker({ event }: EventMarkerProps) {
  return (
    <Marker position={[event.latitude, event.longitude]} icon={createSeverityIcon(event.severity)}>
      <Popup>
        <div className="min-w-48 text-sm">
          <div className="flex items-center gap-2 mb-1">
            <span
              className="inline-block w-2.5 h-2.5 rounded-full"
              style={{ backgroundColor: SEVERITY_COLORS[event.severity] }}
            />
            <span className="font-semibold text-slate-900">{event.title}</span>
          </div>
          <div className="text-slate-600 space-y-0.5">
            <div>Tipo: {EVENT_TYPE_LABELS[event.eventType] ?? event.eventType}</div>
            <div>Severidade: {event.severity}</div>
            <div>
              Data: {format(new Date(event.occurredAt), "d MMM yyyy HH:mm", { locale: pt })}
            </div>
            {event.description && <div className="mt-1 text-slate-500">{event.description}</div>}
            {event.aiClassification && (
              <div className="mt-1 text-xs text-indigo-600">
                IA: {event.aiClassification}
              </div>
            )}
          </div>
        </div>
      </Popup>
    </Marker>
  );
}
