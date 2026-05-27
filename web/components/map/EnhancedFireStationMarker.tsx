"use client";

import { Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import { useEffect } from "react";
import type { FireStation } from "@/lib/types/fireStation";
import type { StationWithResources } from "@/lib/types/resource";

interface EnhancedFireStationMarkerProps {
  station: FireStation | StationWithResources;
  availableCount?: number;
  totalCount?: number;
  onClick?: (station: FireStation | StationWithResources) => void;
  showRoutes?: boolean;
  dimmed?: boolean;
}

// Compact recognizable vectors for fire stations (same as original)
const TYPE_PATHS: Record<string, string> = {
  Voluntarios: "M3 13l1-7h2l1 4h2l1-4h2l1 7h2v8h-2v-5h-2v-2h-2v2h-2v5H3v-8z M9 2c1.5 0 2.7.8 3.3 2M9 6c1 0 1.8.5 2.2 1.3",
  Sapadores: "M2 22h20V12l-6-4V4H8v4L2 12v10z M8 4v4h2V4 M14 4v4h2V4",
  Mistos: "M12 3L2 12h3v8h14v-8h3L12 3z M12 7c1.5 0 2.7.8 3.3 2",
};

const STATION_COLORS = {
  active: "#1e40af",
  activeLight: "#3b82f6",
  inactive: "#94a3b8",
  available: "#22c55e",
  limited: "#eab308",
  busy: "#ef4444",
};

const ICON_BASE_SIZE = 36;

function createEnhancedStationIcon(
  type: string,
  isActive: boolean,
  availableCount: number,
  totalCount: number,
  dimmed: boolean = false
): L.DivIcon {
  // Determine availability status
  const availabilityRatio = totalCount > 0 ? availableCount / totalCount : 0;
  let statusColor: string;
  if (availableCount === 0) {
    statusColor = STATION_COLORS.busy;
  } else if (availabilityRatio < 0.3) {
    statusColor = STATION_COLORS.limited;
  } else {
    statusColor = STATION_COLORS.available;
  }

  const baseColor = isActive ? STATION_COLORS.active : STATION_COLORS.inactive;
  const path = TYPE_PATHS[type] || TYPE_PATHS.Mistos;
  const size = ICON_BASE_SIZE;
  const opacity = dimmed ? 0.4 : 1;

  return L.divIcon({
    className: "fire-station-marker-enhanced",
    html: `
      <svg width="${size}" height="${size}" viewBox="0 0 36 36" xmlns="http://www.w3.org/2000/svg" style="opacity: ${opacity};">
        <!-- Background glow -->
        <circle cx="18" cy="18" r="16" fill="${statusColor}" opacity="0.15"/>
        <!-- Status ring -->
        <circle cx="18" cy="18" r="16" fill="none" stroke="${statusColor}" stroke-width="2"/>
        <!-- White circle -->
        <circle cx="18" cy="18" r="11" fill="${baseColor}"/>
        <!-- Icon path - white -->
        <path d="${path}" fill="white" stroke="none" transform="translate(6,7) scale(0.85)"/>
        <!-- Availability badge -->
        <circle cx="28" cy="8" r="7" fill="${statusColor}" stroke="white" stroke-width="2"/>
        <text x="28" y="11" text-anchor="middle" fill="white" font-size="8" font-weight="bold" font-family="system-ui">${availableCount}</text>
      </svg>
    `,
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2],
    popupAnchor: [0, -size / 2],
  });
}

interface FireStationPopupProps {
  station: FireStation | StationWithResources;
  availableCount: number;
  totalCount: number;
  label: string;
  markerColor: string;
  badgeColor: string;
}

function EnhancedFireStationPopup({ station, availableCount, totalCount, label, markerColor, badgeColor }: FireStationPopupProps) {
  const isStationWithResources = 'availableCount' in station;

  return (
    <div className="min-w-64 text-sm" style={{ fontFamily: "system-ui, sans-serif" }}>
      <div className="flex items-center gap-2 mb-2 pb-2 border-b border-slate-200">
        <span className="w-3 h-3 rounded-full shrink-0" style={{ backgroundColor: markerColor }} />
        <span className="font-bold text-slate-800 text-base">{station.name}</span>
      </div>
      <div className="text-slate-600 space-y-1.5 text-xs">
        <div className="flex gap-2">
          <span className="font-medium text-slate-500 min-w-16">Tipo:</span>
          <span
            className="px-1.5 py-0.5 rounded text-white text-[10px] font-semibold"
            style={{ backgroundColor: badgeColor }}
          >
            {label}
          </span>
        </div>
        <div className="flex gap-2">
          <span className="font-medium text-slate-500 min-w-16">Morada:</span>
          <span className="text-slate-700">{station.address}</span>
        </div>
        <div className="flex gap-2">
          <span className="font-medium text-slate-500 min-w-16">Cidade:</span>
          <span className="text-slate-700">{station.postalCode} {station.city}</span>
        </div>

        {/* Resource availability section */}
        <div className="mt-3 pt-2 border-t border-slate-200">
          <div className="flex items-center justify-between mb-2">
            <span className="font-semibold text-slate-700">Recursos</span>
            <span className="text-xs text-slate-500">
              {availableCount} disponíveis / {totalCount} total
            </span>
          </div>

          {/* Availability bar */}
          <div className="w-full h-2 bg-slate-200 rounded-full overflow-hidden">
            <div
              className="h-full transition-all"
              style={{
                width: `${totalCount > 0 ? (availableCount / totalCount) * 100 : 0}%`,
                backgroundColor: availableCount === 0 ? STATION_COLORS.busy :
                               availableCount / totalCount < 0.3 ? STATION_COLORS.limited : STATION_COLORS.available,
              }}
            />
          </div>
        </div>

        {isStationWithResources && (station as StationWithResources).resources && (
          <div className="mt-2 space-y-1">
            <span className="font-medium text-slate-500">Recursos:</span>
            {(station as StationWithResources).resources.map((resource) => (
              <div key={resource.id} className="flex items-center gap-2 ml-2">
                <span
                  className={`w-2 h-2 rounded-full ${
                    resource.status === "Available" ? "bg-green-500" :
                    resource.status === "EnRoute" ? "bg-blue-500" :
                    resource.status === "OnScene" ? "bg-green-600" :
                    "bg-gray-400"
                  }`}
                />
                <span className="text-slate-700">{resource.name}</span>
                <span className="text-slate-400">({resource.resourceType})</span>
              </div>
            ))}
          </div>
        )}

        {!station.isActive && (
          <div className="mt-2 p-2 bg-slate-100 border border-slate-200 rounded text-slate-500">
            <span className="font-semibold text-[10px] uppercase tracking-wide">Estação Inativa</span>
          </div>
        )}
      </div>
    </div>
  );
}

export function EnhancedFireStationMarker({
  station,
  availableCount = 0,
  totalCount = 0,
  onClick,
  dimmed = false,
}: EnhancedFireStationMarkerProps) {
  const markerColor = station.isActive ? STATION_COLORS.active : STATION_COLORS.inactive;
  const badgeColor = station.isActive ? STATION_COLORS.activeLight : STATION_COLORS.inactive;

  const TYPE_LABELS_PT: Record<string, string> = {
    Voluntarios: "Voluntários",
    Sapadores: "Sapadores",
    Mistos: "Mistos",
  };
  const label = TYPE_LABELS_PT[station.type] ?? station.type;

  return (
    <Marker
      position={[station.latitude, station.longitude]}
      icon={createEnhancedStationIcon(station.type, station.isActive ?? true, availableCount, totalCount, dimmed)}
      eventHandlers={{ click: () => onClick?.(station) }}
      opacity={dimmed ? 0.4 : 1}
    >
      <Popup>
        <EnhancedFireStationPopup
          station={station}
          availableCount={availableCount}
          totalCount={totalCount}
          label={label}
          markerColor={markerColor}
          badgeColor={badgeColor}
        />
      </Popup>
    </Marker>
  );
}

// Component to draw route lines from stations to events
interface RouteLinesProps {
  routes: Array<{
    fromLat: number;
    fromLng: number;
    toLat: number;
    toLng: number;
    color?: string;
  }>;
}

export function RouteLines({ routes }: RouteLinesProps) {
  const map = useMap();

  useEffect(() => {
    const lines: L.Polyline[] = [];

    routes.forEach((route) => {
      const polyline = L.polyline(
        [[route.fromLat, route.fromLng], [route.toLat, route.toLng]],
        {
          color: route.color || "#3b82f6",
          weight: 3,
          opacity: 0.7,
          dashArray: "10, 10",
        }
      );
      polyline.addTo(map);
      lines.push(polyline);
    });

    return () => {
      lines.forEach((line) => line.remove());
    };
  }, [map, routes]);

  return null;
}