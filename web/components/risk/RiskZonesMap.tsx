"use client";

import { useState, useCallback, useEffect } from "react";
import { Filter, X } from "lucide-react";
import { MAP_CONFIG } from "@/lib/map/config";
import { RiskFilters } from "./RiskFilters";
import { useRiskZones } from "@/lib/hooks/useRiskZones";
import type { RiskZone } from "@/lib/types/riskZone";
import { MapContainer as LeafletMap, TileLayer, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

const RISK_COLORS: Record<string, string> = {
  Low: "#22c55e",
  Medium: "#eab308",
  High: "#f97316",
  Critical: "#ef4444",
};

function parseWKT(wkt: string): [number, number][] | null {
  const wktMatch = wkt.match(/POLYGON\s*\(\((.+?)\)\)/i);
  if (!wktMatch) return null;
  const coordsStr = wktMatch[1];
  return coordsStr.split(",").map((pair) => {
    const [lng, lat] = pair.trim().split(/\s+/).map(Number);
    return [lat, lng] as [number, number];
  });
}

interface RiskZonesMapProps {
  riskZones?: RiskZone[];
  showHeatLayer?: boolean;
  showPolygons?: boolean;
  onRiskZoneClick?: (zone: RiskZone) => void;
}

function RiskZoneLayer({
  zones,
  showPolygons,
  onRiskZoneClick,
}: {
  zones: RiskZone[];
  showPolygons: boolean;
  onRiskZoneClick?: (zone: RiskZone) => void;
}) {
  const map = useMap();

  useEffect(() => {
    if (!showPolygons || zones.length === 0) return;

    const layerGroup = L.layerGroup().addTo(map);

    zones.forEach((zone) => {
      const coords = parseWKT(zone.wkt);
      if (!coords) return;

      const color = RISK_COLORS[zone.riskLevelName] ?? "#94a3b8";

      const polygon = L.polygon(coords, {
        color,
        weight: 2,
        opacity: 0.8,
        fillColor: color,
        fillOpacity: 0.3,
      });

      polygon.bindPopup(`
        <div class="min-w-48 text-sm" style="font-family: system-ui, sans-serif;">
          <div class="flex items-center gap-2 mb-2 pb-2 border-b border-slate-200">
            <span class="w-3 h-3 rounded-full" style="background-color: ${color}"></span>
            <span class="font-bold text-slate-800">Zona de Risco</span>
          </div>
          <div class="text-slate-600 space-y-1.5 text-xs">
            <div class="flex gap-2">
              <span class="font-medium text-slate-500 min-w-16">Nível:</span>
              <span class="px-1.5 py-0.5 rounded text-white text-[10px] font-semibold" style="background-color: ${color}">
                ${zone.riskLevelName}
              </span>
            </div>
            <div class="flex gap-2">
              <span class="font-medium text-slate-500 min-w-16">Índice:</span>
              <span class="text-slate-700">${zone.riskIndex.toFixed(2)}</span>
            </div>
            <div class="flex gap-2">
              <span class="font-medium text-slate-500 min-w-16">Temp:</span>
              <span class="text-slate-700">${zone.temperature}°C</span>
            </div>
            <div class="flex gap-2">
              <span class="font-medium text-slate-500 min-w-16">Humidade:</span>
              <span class="text-slate-700">${zone.humidity}%</span>
            </div>
            <div class="flex gap-2">
              <span class="font-medium text-slate-500 min-w-16">Vento:</span>
              <span class="text-slate-700">${zone.windSpeed} km/h</span>
            </div>
            ${zone.conclusion ? `
            <div class="pt-1.5 border-t border-slate-100">
              <span class="text-slate-500 leading-relaxed">${zone.conclusion}</span>
            </div>
            ` : ""}
          </div>
        </div>
      `);

      polygon.on("click", () => onRiskZoneClick?.(zone));
      layerGroup.addLayer(polygon);
    });

    return () => {
      map.removeLayer(layerGroup);
    };
  }, [map, zones, showPolygons, onRiskZoneClick]);

  return null;
}

function HeatLayerComponent({
  zones,
  showHeatLayer,
}: {
  zones: RiskZone[];
  showHeatLayer: boolean;
}) {
  const map = useMap();

  useEffect(() => {
    if (!showHeatLayer || zones.length === 0) return;

    const layerGroup = L.layerGroup().addTo(map);

    zones.forEach((zone) => {
      zone.dataPoints.forEach((point) => {
        const intensity = Math.min(point.fwi / 50, 1);
        const tempColor = intensity > 0.7 ? "#ef4444" : intensity > 0.4 ? "#f97316" : "#eab308";

        const circle = L.circle([point.latitude, point.longitude], {
          radius: 200 + intensity * 300,
          color: tempColor,
          weight: 1,
          opacity: 0.6,
          fillColor: tempColor,
          fillOpacity: 0.2 + intensity * 0.3,
        });

        circle.bindPopup(`
          <div class="text-xs" style="font-family: system-ui, sans-serif;">
            <div class="font-semibold text-slate-800">Ponto de Dados</div>
            <div class="text-slate-600 mt-1">
              <div>Temp: ${point.temperature}°C</div>
              <div>Humidade: ${point.humidity}%</div>
              <div>FWI: ${point.fwi.toFixed(1)}</div>
            </div>
          </div>
        `);

        layerGroup.addLayer(circle);
      });
    });

    return () => {
      map.removeLayer(layerGroup);
    };
  }, [map, zones, showHeatLayer]);

  return null;
}

function RiskLayers({
  zones,
  showPolygons,
  showHeatLayer,
  onRiskZoneClick,
}: {
  zones: RiskZone[];
  showPolygons: boolean;
  showHeatLayer: boolean;
  onRiskZoneClick?: (zone: RiskZone) => void;
}) {
  return (
    <>
      <TileLayer url={MAP_CONFIG.tileUrl} attribution={MAP_CONFIG.tileAttribution} />
      <RiskZoneLayer
        zones={zones}
        showPolygons={showPolygons}
        onRiskZoneClick={onRiskZoneClick}
      />
      <HeatLayerComponent zones={zones} showHeatLayer={showHeatLayer} />
    </>
  );
}

export function RiskZonesMap({
  riskZones = [],
  showHeatLayer: initialShowHeatLayer = false,
  showPolygons: initialShowPolygons = true,
  onRiskZoneClick,
}: RiskZonesMapProps = {}) {
  const [showHeatLayer, setShowHeatLayer] = useState(initialShowHeatLayer);
  const [showPolygons, setShowPolygons] = useState(initialShowPolygons);
  const [minRiskLevel, setMinRiskLevel] = useState<string>("Low");
  const [filtersOpen, setFiltersOpen] = useState(false);

  const { data: riskZonesData } = useRiskZones(minRiskLevel);

  const allZones = riskZones.length > 0 ? riskZones : (riskZonesData?.zones ?? []);

  const handleHeatLayerChange = useCallback((show: boolean) => {
    setShowHeatLayer(show);
  }, []);

  const handlePolygonsChange = useCallback((show: boolean) => {
    setShowPolygons(show);
  }, []);

  const handleMinRiskLevelChange = useCallback((level: string) => {
    setMinRiskLevel(level);
  }, []);

  return (
    <div className="flex w-full h-full relative">
      <LeafletMap
        center={[MAP_CONFIG.center.lat, MAP_CONFIG.center.lng]}
        zoom={MAP_CONFIG.zoom}
        className="h-full w-full"
        zoomControl={true}
        preferCanvas={true}
      >
        <RiskLayers
          zones={allZones}
          showPolygons={showPolygons}
          showHeatLayer={showHeatLayer}
          onRiskZoneClick={onRiskZoneClick}
        />
      </LeafletMap>
      <button
        onClick={() => setFiltersOpen(!filtersOpen)}
        className="absolute top-4 right-4 z-[1000] bg-slate-800/95 backdrop-blur-sm border border-slate-700 rounded-xl p-3 text-white hover:bg-slate-700"
      >
        {filtersOpen ? <X className="w-5 h-5" /> : <Filter className="w-5 h-5" />}
      </button>
      <div
        className={`absolute top-4 right-16 z-[1000] bg-slate-800/95 backdrop-blur-sm border border-slate-700 rounded-xl overflow-hidden transition-all duration-300 ${
          filtersOpen ? "w-72 opacity-100" : "w-0 opacity-0 pointer-events-none"
        }`}
      >
        <div className="w-72">
          <RiskFilters
            showHeatLayer={showHeatLayer}
            onHeatLayerChange={handleHeatLayerChange}
            showPolygons={showPolygons}
            onPolygonsChange={handlePolygonsChange}
            minRiskLevel={minRiskLevel}
            onMinRiskLevelChange={handleMinRiskLevelChange}
          />
        </div>
      </div>
    </div>
  );
}