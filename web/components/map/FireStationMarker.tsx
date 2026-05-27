"use client";

import { Marker, Popup } from "react-leaflet";
import L from "leaflet";
import type { FireStation } from "@/lib/types/fireStation";

// Compact recognizable vectors for fire stations
const TYPE_PATHS: Record<string, string> = {
  // Voluntarios - flame with house
  Voluntarios: "M3 13l1-7h2l1 4h2l1-4h2l1 7h2v8h-2v-5h-2v-2h-2v2h-2v5H3v-8z M9 2c1.5 0 2.7.8 3.3 2M9 6c1 0 1.8.5 2.2 1.3",
  // Sapadores - station building
  Sapadores: "M2 22h20V12l-6-4V4H8v4L2 12v10z M8 4v4h2V4 M14 4v4h2V4",
  // Mistos - mixed/association (house + flame combined)
  Mistos: "M12 3L2 12h3v8h14v-8h3L12 3z M12 7c1.5 0 2.7.8 3.3 2",
};

const TYPE_LABELS_PT: Record<string, string> = {
  Voluntarios: "Voluntários",
  Sapadores: "Sapadores",
  Mistos: "Mistos",
};

const STATION_COLORS = {
  active: "#1e40af",
  activeLight: "#3b82f6",
  inactive: "#94a3b8",
};

const ICON_BASE_SIZE = 32;

function createStationIcon(type: string, isActive: boolean, dimmed: boolean = false): L.DivIcon {
  const color = isActive ? STATION_COLORS.active : STATION_COLORS.inactive;
  const lightColor = isActive ? STATION_COLORS.activeLight : STATION_COLORS.inactive;
  const path = TYPE_PATHS[type];
  const size = ICON_BASE_SIZE;
  const opacity = dimmed ? 0.4 : 1;

  return L.divIcon({
    className: "fire-station-marker",
    html: `
      <svg width="${size}" height="${size}" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" style="opacity: ${opacity};">
        <!-- Background circle with glow -->
        <circle cx="12" cy="12" r="11" fill="${color}" opacity="0.15"/>
        <circle cx="12" cy="12" r="8" fill="${lightColor}" opacity="0.2"/>
        <!-- Outer ring -->
        <circle cx="12" cy="12" r="11" fill="none" stroke="${color}" stroke-width="1.5"/>
        <!-- Icon path - white, centered -->
        <path d="${path}" fill="white" stroke="none" transform="translate(0,1) scale(0.9)"/>
      </svg>
    `,
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2],
    popupAnchor: [0, -size / 2],
  });
}

interface FireCompanyPopupProps {
  station: FireStation;
  label: string;
  markerColor: string;
  badgeColor: string;
}

function FireCompanyPopup({ station, label, markerColor, badgeColor }: FireCompanyPopupProps) {
  return (
    <div className="min-w-56 text-sm" style={{ fontFamily: "system-ui, sans-serif" }}>
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
        {station.phone && (
          <div className="flex gap-2">
            <span className="font-medium text-slate-500 min-w-16">Telefone:</span>
            <span className="text-slate-700">{station.phone}</span>
          </div>
        )}
        {station.email && (
          <div className="flex gap-2">
            <span className="font-medium text-slate-500 min-w-16">Email:</span>
            <span className="text-slate-700">{station.email}</span>
          </div>
        )}
        <div className="flex gap-2">
          <span className="font-medium text-slate-500 min-w-16">Pessoal:</span>
          <span className="text-slate-700">{station.personnelCount}</span>
        </div>
        <div className="flex gap-2">
          <span className="font-medium text-slate-500 min-w-16">Veículos:</span>
          <span className="text-slate-700">{station.vehicleCount}</span>
        </div>
        {station.operationalZone && (
          <div className="flex gap-2">
            <span className="font-medium text-slate-500 min-w-16">Zona:</span>
            <span className="text-slate-700">{station.operationalZone}</span>
          </div>
        )}
        {station.cim && (
          <div className="flex gap-2">
            <span className="font-medium text-slate-500 min-w-16">CIM:</span>
            <span className="text-slate-700">{station.cim}</span>
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

interface FireStationMarkerProps {
  station: FireStation;
  onClick?: (station: FireStation) => void;
  dimmed?: boolean;
}

export function FireStationMarker({ station, onClick, dimmed = false }: FireStationMarkerProps) {
  const markerColor = station.isActive ? STATION_COLORS.active : STATION_COLORS.inactive;
  const badgeColor = station.isActive ? STATION_COLORS.activeLight : STATION_COLORS.inactive;
  const label = TYPE_LABELS_PT[station.type] ?? station.type;

  return (
    <Marker
      position={[station.latitude, station.longitude]}
      icon={createStationIcon(station.type, station.isActive, dimmed)}
      eventHandlers={{ click: () => onClick?.(station) }}
      opacity={dimmed ? 0.4 : 1}
    >
      <Popup>
        <FireCompanyPopup
          station={station}
          label={label}
          markerColor={markerColor}
          badgeColor={badgeColor}
        />
      </Popup>
    </Marker>
  );
}
