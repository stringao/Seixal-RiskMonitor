"use client";

import { useEffect, useRef } from "react";
import { useMap } from "react-leaflet";
import L from "leaflet";
import type { RiskZone } from "@/lib/types/riskZone";

interface RiskZoneLayerProps {
  riskZones: RiskZone[];
  onZoneClick?: (zone: RiskZone) => void;
}

const RISK_COLORS: Record<string, string> = {
  Critical: "#ef4444",
  High: "#f97316",
  Medium: "#eab308",
  Low: "#22c55e",
};

/**
 * Parse WKT POLYGON string to Leaflet LatLng array
 * WKT format: POLYGON((lon lat, lon lat, ...))
 * Returns: [[lat, lng], [lat, lng], ...]
 */
export function parseWktToLatLngs(wkt: string): [number, number][] {
  // Extract coordinates between POLYGON( and )
  const match = wkt.match(/POLYGON\s*\(\s*\((.+)\)\s*\)/i);
  if (!match || !match[1]) {
    return [];
  }

  const coordsStr = match[1];
  const coords = coordsStr.split(",").map((pair) => {
    const [lng, lat] = pair.trim().split(/\s+/).map(Number);
    return [lat, lng] as [number, number];
  });

  return coords;
}

function RiskZoneLayerInner({ riskZones, onZoneClick }: RiskZoneLayerProps) {
  const map = useMap();
  const layerGroupRef = useRef<L.LayerGroup | null>(null);

  useEffect(() => {
    if (!map) return;

    // Clean up previous layer
    if (layerGroupRef.current) {
      map.removeLayer(layerGroupRef.current);
      layerGroupRef.current = null;
    }

    const group = L.layerGroup();
    layerGroupRef.current = group;
    map.addLayer(group);

    riskZones.forEach((zone) => {
      const latLngs = parseWktToLatLngs(zone.wkt);
      if (latLngs.length === 0) return;

      const color = RISK_COLORS[zone.riskLevelName] ?? "#94a3b8";

      const polygon = L.polygon(latLngs, {
        color,
        fillColor: color,
        fillOpacity: 0.25,
        weight: 1.5,
      });

      polygon.bindPopup(() => {
        const container = document.createElement("div");
        container.innerHTML = createPopupHtml(zone, color);
        return container;
      });

      polygon.on("click", () => {
        onZoneClick?.(zone);
      });

      group.addLayer(polygon);
    });

    return () => {
      if (layerGroupRef.current) {
        map.removeLayer(layerGroupRef.current);
        layerGroupRef.current = null;
      }
    };
  }, [map, riskZones, onZoneClick]);

  return null;
}

/**
 * Create HTML string for popup content (matching RiskPopup styling)
 */
function createPopupHtml(zone: RiskZone, color: string): string {
  const levelLabels: Record<string, string> = {
    Critical: "Crítico",
    High: "Alto",
    Medium: "Médio",
    Low: "Baixo",
  };
  const levelLabel = levelLabels[zone.riskLevelName] ?? zone.riskLevelName;

  return `
    <div class="min-w-[240px] p-3" style="font-family: system-ui, sans-serif;">
      <div class="flex items-center gap-2 mb-3">
        <span class="w-3 h-3 rounded-full shrink-0" style="background-color: ${color};"></span>
        <span class="font-semibold text-sm text-slate-800">${levelLabel}</span>
        <span class="text-xs text-slate-500">FWI ${zone.riskIndex}</span>
      </div>
      <div class="grid grid-cols-2 gap-1 text-sm text-slate-700">
        <span>🌡️ Temp:</span>
        <span>${zone.temperature}°C</span>
        <span>💧 Hum:</span>
        <span>${zone.humidity}%</span>
        <span>🌬️ Vento:</span>
        <span>${zone.windSpeed} km/h ${zone.windDirection}</span>
      </div>
      <div class="mt-3 pt-3 border-t border-slate-700">
        <p class="text-xs text-slate-300">${zone.conclusion}</p>
      </div>
    </div>
  `;
}

export function RiskZoneLayer(props: RiskZoneLayerProps) {
  return <RiskZoneLayerInner {...props} />;
}
