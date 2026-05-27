"use client";

import { Marker, Popup } from "react-leaflet";
import L from "leaflet";
import { SEVERITY_COLORS, EVENT_TYPE_LABELS } from "@/lib/map/config";
import { format } from "date-fns";
import type { GeoEvent } from "@/lib/types/event";

const TYPE_PATHS: Record<string, string> = {
  Fire: "M17.66 11.2C17.43 10.9 17.15 10.64 16.89 10.38C16.22 9.78 15.46 9.35 14.82 8.72C13.33 7.26 13 4.85 13.95 3C13 3.23 12.17 3.75 11.46 4.32C8.87 6.4 7.85 10.07 9.07 13.22C9.11 13.32 9.15 13.42 9.15 13.55C9.15 13.77 9 13.97 8.8 14.05C8.57 14.15 8.33 14.09 8.14 13.93C8.08 13.88 8.04 13.83 8 13.76C6.87 12.33 6.69 10.28 7.45 8.64C5.78 10 4.87 12.3 5 14.47C5.06 15.58 5.5 16.68 6 17.69C7.08 19.44 8.95 20.89 10.96 21.68C11.36 21.78 11.78 21.61 12 21.35C12.22 21.09 12.35 20.7 12.22 20.36C12.1 20.05 11.94 19.76 11.8 19.47C11.14 18.19 11.11 16.71 11.6 15.39C12.24 13.57 13.81 12.23 15.68 12.23C16.43 12.23 17.13 12.38 17.77 12.67C17.94 12.74 18.12 12.75 18.28 12.71C18.45 12.67 18.6 12.58 18.72 12.44C18.84 12.31 18.91 12.15 18.91 11.97C18.87 11.72 18.78 11.5 18.66 11.31L17.66 11.2ZM16.5 14C15.28 14 14.18 13.36 13.55 12.35C13.8 12.05 14.17 11.88 14.57 11.88C15.47 11.88 16.2 12.6 16.2 13.5C16.2 13.9 16.03 14.27 15.72 14.5C15.97 14.21 16.28 14.09 16.5 14Z",
  Flood: "M12 2C6.48 2 2 6.48 2 12C2 17.52 6.48 22 12 22C17.52 22 22 17.52 22 12C22 6.48 17.52 2 12 2ZM12 20C7.58 20 4 16.42 4 12C4 7.58 7.58 4 12 4C16.42 4 20 7.58 20 12C20 16.42 16.42 20 12 20ZM12 6C9.24 6 7 8.24 7 11H9C9 9.34 10.34 8 12 8C13.66 8 15 9.34 15 11H17C17 8.24 14.76 6 12 6ZM7 13V15H9V13H7ZM11 13V17H13V13H11Z",
  Storm: "M19 8C19.55 8 20 7.55 20 7C20 6.45 19.55 6 19 6C18.45 6 18 6.45 18 7C18 7.55 18.45 8 19 8ZM19 10C18.04 10 17.36 10.68 17.36 11.64C17.36 12.6 18.04 13.28 19 13.28C19.96 13.28 20.64 12.6 20.64 11.64C20.64 10.68 19.96 10 19 10ZM15.45 8.15C15.15 7.85 14.7 7.85 14.4 8.15L10.4 12.15C10.1 12.45 10.1 12.9 10.4 13.2L11.8 14.6C12.1 14.9 12.55 14.9 12.85 14.6L16.85 10.6C17.15 10.3 17.15 9.85 16.85 9.55L15.45 8.15ZM4 15.5L2 22H22L20 15.5L17 18.5L14 15.5L11 18.5L8 15.5L5 18.5L4 15.5Z",
  Landslide: "M14 2H6C4.9 2 4 2.9 4 4V20C4 21.1 4.9 22 6 22H18C19.1 22 20 21.1 20 20V8L14 2ZM16 16H13V13H11V16H8L12 20L16 16Z",
  Industrial: "M19 3H5C3.9 3 3 3.9 3 5V19C3 20.1 3.9 21 5 21H19C20.1 21 21 20.1 21 19V5C21 3.9 20.1 3 19 3ZM19 19H5V5H19V19ZM7 17H9V10H7ZM11 17H13V7H11ZM15 17H17V13H15Z",
  Heatwave: "M12 7C9.24 7 7 9.24 7 12C7 14.76 9.24 17 12 17C14.76 17 17 14.76 17 12C17 9.24 14.76 7 12 7ZM12 15C10.34 15 9 13.66 9 12C9 10.34 10.34 9 12 9C13.66 9 15 10.34 15 12C15 13.66 13.66 15 12 15ZM11 2V5H13V2ZM13 22V19H11V22ZM5 13V11H2V13ZM22 13H19V11H22ZM12 17C10.34 17 9 15.66 9 14C9 12.34 10.34 11 12 11C13.66 11 15 12.34 15 14C15 15.66 13.66 17 12 17ZM12 13C11.45 13 11 12.55 11 12C11 11.45 11.45 11 12 11C12.55 11 13 11.45 13 12C13 12.55 12.55 13 12 13Z",
  Other: "M12 2C6.48 2 2 6.48 2 12C2 17.52 6.48 22 12 22C17.52 22 22 17.52 22 12C22 6.48 17.52 2 12 2ZM13 17H11V15H13ZM13 13H11V7H13Z",
};

const TYPE_LABELS_PT: Record<string, string> = {
  Fire: "Incêndio",
  Flood: "Inundação",
  Storm: "Tempestade",
  Landslide: "Deslizamento",
  Industrial: "Industrial",
  Heatwave: "Onda de Calor",
  Other: "Outro",
};

interface EventMarkerProps {
  event: GeoEvent;
  isSelected?: boolean;
  dimmed?: boolean;
  onClick?: (eventId: string) => void;
}

function createFallbackIcon(color: string, baseSize: number, isSelected: boolean, opacity: number): L.DivIcon {
  return L.divIcon({
    className: isSelected ? "custom-marker selected-marker" : "custom-marker",
    html: `<div style="
      background-color: ${color};
      width: ${isSelected ? 24 : 18}px; 
      height: ${isSelected ? 24 : 18}px;
      border-radius: 50%;
      border: ${isSelected ? 3 : 2}px solid white;
      box-shadow: ${isSelected 
        ? `0 0 0 4px ${color}40, 0 0 20px ${color}` 
        : `0 0 8px ${color}80`};
      opacity: ${opacity};
      animation: ${isSelected ? 'pulse-ring 1.5s ease-out infinite' : 'none'};
    "></div>`,
    iconSize: [baseSize, baseSize],
    iconAnchor: [baseSize / 2, baseSize / 2],
  });
}

function buildSvgIcon(path: string, color: string, baseSize: number, opacity: number, isSelected: boolean): string {
  const glowStyle = isSelected 
    ? `filter: drop-shadow(0 0 8px ${color}) drop-shadow(0 0 16px ${color}80);` 
    : "";

  const pulseRing = isSelected 
    ? `<circle cx="12" cy="12" r="11" fill="${color}" opacity="0.3" class="pulse-ring"/>` 
    : "";
  const outerRing = `<circle cx="12" cy="12" r="${isSelected ? 10 : 11}" fill="${color}" opacity="${isSelected ? 0.3 : 0.2}"/>`;
  const innerRing = `<circle cx="12" cy="12" r="${isSelected ? 7 : 8.5}" fill="${color}" opacity="${isSelected ? 0.5 : 0.4}"/>`;
  const iconPath = `<path d="${path}" fill="white" transform="translate(2,2) scale(0.83)"/>`;
  const strokeRing = `<circle cx="12" cy="12" r="11" fill="none" stroke="${color}" stroke-width="${isSelected ? 2 : 1.5}"/>`;
  const pulseRingOuter = isSelected 
    ? `<circle cx="12" cy="12" r="12" fill="none" stroke="${color}" stroke-width="2" opacity="0.6" class="pulse-ring"/>` 
    : "";

  return `
    <svg width="${baseSize}" height="${baseSize}" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" style="${glowStyle} opacity: ${opacity};">
      ${pulseRing}
      ${outerRing}
      ${innerRing}
      ${iconPath}
      ${strokeRing}
      ${pulseRingOuter}
    </svg>
  `;
}

function createSvgIcon(eventType: string, severity: string, baseSize: number, opacity: number, isSelected: boolean): L.DivIcon {
  const color = SEVERITY_COLORS[severity] ?? "#94a3b8";
  const path = TYPE_PATHS[eventType] ?? "";

  return L.divIcon({
    className: isSelected ? "custom-marker selected-marker" : "custom-marker",
    html: buildSvgIcon(path, color, baseSize, opacity, isSelected),
    iconSize: [baseSize, baseSize],
    iconAnchor: [baseSize / 2, baseSize / 2],
  });
}

function createEventIcon(eventType: string, severity: string, isSelected: boolean, dimmed: boolean): L.DivIcon {
  const color = SEVERITY_COLORS[severity] ?? "#94a3b8";
  const path = TYPE_PATHS[eventType];
  
  const baseSize = isSelected ? 48 : (dimmed ? 28 : 36);
  const opacity = dimmed ? 0.3 : 1;

  if (!path) {
    return createFallbackIcon(color, baseSize, isSelected, opacity);
  }

  return createSvgIcon(eventType, severity, baseSize, opacity, isSelected);
}

export function EventMarker({ event, isSelected = false, dimmed = false, onClick }: EventMarkerProps) {
  const handleClick = () => {
    onClick?.(event.id);
  };

  return (
    <Marker
      position={[event.latitude, event.longitude]}
      icon={createEventIcon(event.eventType, event.severity, isSelected, dimmed)}
      eventHandlers={{ click: handleClick }}
    >
      <Popup>
        <div className="min-w-52 text-sm" style={{ fontFamily: "system-ui, sans-serif" }}>
          <div className="flex items-center gap-2 mb-2 pb-2 border-b border-slate-200">
            <span
              className="w-3 h-3 rounded-full shrink-0"
              style={{ backgroundColor: SEVERITY_COLORS[event.severity] }}
            />
            <span className="font-bold text-slate-800 text-base">{event.title}</span>
          </div>
          <div className="text-slate-600 space-y-1.5 text-xs">
            <div className="flex gap-2">
              <span className="font-medium text-slate-500 min-w-16">Tipo:</span>
              <span className="text-slate-700">
                {TYPE_LABELS_PT[event.eventType] ?? event.eventType}
              </span>
            </div>
            <div className="flex gap-2">
              <span className="font-medium text-slate-500 min-w-16">Severidade:</span>
              <span
                className="px-1.5 py-0.5 rounded text-white text-[10px] font-semibold"
                style={{ backgroundColor: SEVERITY_COLORS[event.severity] }}
              >
                {event.severity === "Low" ? "Baixo" :
                 event.severity === "Medium" ? "Médio" :
                 event.severity === "High" ? "Alto" :
                 event.severity === "Critical" ? "Crítico" : event.severity}
              </span>
            </div>
            <div className="flex gap-2">
              <span className="font-medium text-slate-500 min-w-16">Data:</span>
              <span className="text-slate-700">
                {format(new Date(event.occurredAt), "dd/MM/yyyy HH:mm")}
              </span>
            </div>
            {event.source && (
              <div className="flex gap-2">
                <span className="font-medium text-slate-500 min-w-16">Fonte:</span>
                <span className="text-slate-700">{event.source}</span>
              </div>
            )}
            {event.description && (
              <div className="pt-1.5 border-t border-slate-100">
                <span className="text-slate-500 leading-relaxed">{event.description}</span>
              </div>
            )}
            {event.aiClassification && (
              <div className="mt-2 p-2 bg-indigo-50 border border-indigo-100 rounded text-indigo-700">
                <span className="font-semibold text-[10px] uppercase tracking-wide">IA:</span>
                <span className="ml-1 text-[11px]">{event.aiClassification}</span>
              </div>
            )}
            {isSelected && (
              <div className="mt-2 p-2 bg-amber-50 border border-amber-200 rounded text-amber-700">
                <span className="font-semibold text-[10px] uppercase tracking-wide">Selecionado</span>
              </div>
            )}
          </div>
        </div>
      </Popup>
    </Marker>
  );
}