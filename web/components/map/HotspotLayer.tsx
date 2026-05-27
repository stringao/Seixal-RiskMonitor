"use client";

import { useEffect, useRef } from "react";
import { useMap } from "react-leaflet";
import L from "leaflet";
import type { FireHotspot } from "@/lib/types/hotspot";

interface HotspotLayerProps {
  hotspots: FireHotspot[];
  onHotspotClick?: (hotspot: FireHotspot) => void;
}

const RISK_COLORS: Record<string, string> = {
  Critical: "#ef4444",
  High: "#f97316",
  Medium: "#eab308",
  Low: "#22c55e",
};

const MONTH_NAMES = [
  "Jan", "Fev", "Mar", "Abr", "Mai", "Jun",
  "Jul", "Ago", "Set", "Out", "Nov", "Dez"
];

function HotspotLayerInner({ hotspots, onHotspotClick }: HotspotLayerProps) {
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

    hotspots.forEach((hotspot) => {
      const color = RISK_COLORS[hotspot.riskLevel] ?? "#94a3b8";
      const radius = Math.min(20, Math.max(8, hotspot.fireCount * 3));

      const circle = L.circleMarker([hotspot.latitude, hotspot.longitude], {
        radius,
        color,
        fillColor: color,
        fillOpacity: 0.4,
        weight: 2,
      });

      const peakMonthLabel = MONTH_NAMES[hotspot.peakMonth - 1] ?? "?";
      const severityLabel = hotspot.averageSeverity === "Critical" ? "Crítico" :
        hotspot.averageSeverity === "High" ? "Alto" :
        hotspot.averageSeverity === "Medium" ? "Médio" : "Baixo";

      const popupHtml = `
        <div class="min-w-[200px] p-3" style="font-family: system-ui, sans-serif;">
          <div class="flex items-center gap-2 mb-2">
            <span class="w-3 h-3 rounded-full" style="background-color: ${color};"></span>
            <span class="font-semibold text-sm text-slate-800">${hotspot.name}</span>
          </div>
          <div class="grid grid-cols-2 gap-1 text-xs text-slate-600">
            <span>Incêndios:</span>
            <span class="font-medium">${hotspot.fireCount}</span>
            <span>Área:</span>
            <span class="font-medium">${hotspot.totalAreaBurned.toFixed(1)} ha</span>
            <span>Severidade:</span>
            <span class="font-medium">${severityLabel}</span>
            <span>Pico:</span>
            <span class="font-medium">${peakMonthLabel} ${hotspot.peakHour}h</span>
          </div>
          ${hotspot.commonWindDirection ? `
          <div class="mt-2 pt-2 border-t border-slate-200">
            <span class="text-xs text-slate-500">Vento predominante: ${hotspot.commonWindDirection}</span>
          </div>
          ` : ''}
        </div>
      `;

      circle.bindPopup(popupHtml);
      circle.on("click", () => onHotspotClick?.(hotspot));

      group.addLayer(circle);
    });

    return () => {
      if (layerGroupRef.current) {
        map.removeLayer(layerGroupRef.current);
        layerGroupRef.current = null;
      }
    };
  }, [map, hotspots, onHotspotClick]);

  return null;
}

export function HotspotLayer(props: HotspotLayerProps) {
  return <HotspotLayerInner {...props} />;
}