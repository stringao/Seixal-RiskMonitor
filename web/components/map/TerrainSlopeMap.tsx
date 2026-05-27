"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { Layers, X } from "lucide-react";
import { MAP_CONFIG } from "@/lib/map/config";
import { fetchSlopeVisualization, type SlopeVisualizationResponse } from "@/lib/api/risk";
import { MapContainer as LeafletMap, TileLayer, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

// Slope category type and validation
type SlopeCategory = "flat" | "moderate" | "steep" | "very_steep";

const VALID_CATEGORIES: SlopeCategory[] = ["flat", "moderate", "steep", "very_steep"];

function isValidSlopeCategory(val: string): val is SlopeCategory {
  return VALID_CATEGORIES.includes(val as SlopeCategory);
}

// Slope color scale (green to red)
const SLOPE_COLORS: Record<SlopeCategory, string> = {
  flat: "#22c55e",      // Green: <10°
  moderate: "#eab308",   // Yellow: 10-25°
  steep: "#f97316",      // Orange: 25-40°
  very_steep: "#ef4444", // Red: 40°+
};

const SLOPE_LABELS: Record<SlopeCategory, string> = {
  flat: "Plano (<10°)",
  moderate: "Moderado (10-25°)",
  steep: "Inclinado (25-40°)",
  very_steep: "Muito Inclinado (>40°)",
};

interface SlopeLayerProps {
  data: SlopeVisualizationResponse | null;
  visible: boolean;
}

function SlopeLayer({ data, visible }: SlopeLayerProps) {
  const map = useMap();
  const layerRef = useRef<L.LayerGroup | null>(null);

  useEffect(() => {
    // Clean up previous layer
    if (layerRef.current) {
      map.removeLayer(layerRef.current);
      layerRef.current = null;
    }

    if (!visible || !data?.features?.length) return;

    // Use canvas renderer for better performance with many points
    const canvasRenderer = L.canvas({ padding: 0.5 });
    const layerGroup = L.layerGroup().addTo(map);
    layerRef.current = layerGroup;

    data.features.forEach((feature) => {
      const { coordinates } = feature.geometry;
      const { slope, category } = feature.properties;

      // Use validated category
      const validCategory = isValidSlopeCategory(category) ? category : "flat";
      const color = SLOPE_COLORS[validCategory];

      const marker = L.circleMarker([coordinates[1], coordinates[0]], {
        renderer: canvasRenderer,
        radius: 6,
        color: color,
        weight: 1,
        opacity: 0.8,
        fillColor: color,
        fillOpacity: 0.5 + (slope / 90) * 0.4,
      });

      marker.bindTooltip(`
        <div style="font-family: system-ui, sans-serif; font-size: 12px;">
          <strong>Declive:</strong> ${slope.toFixed(1)}°
          <br/><span style="color: ${color};">${SLOPE_LABELS[validCategory]}</span>
        </div>
      `, { className: "slope-tooltip" });

      layerGroup.addLayer(marker);
    });

    return () => {
      // Cleanup is handled by next effect run or unmount
    };
  }, [map, data, visible]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (layerRef.current) {
        map.removeLayer(layerRef.current);
      }
    };
  }, [map]);

  return null;
}

interface TerrainSlopeMapProps {
  className?: string;
}

export function TerrainSlopeMap({ className = "" }: TerrainSlopeMapProps) {
  const [showSlope, setShowSlope] = useState(true);
  const [slopeData, setSlopeData] = useState<SlopeVisualizationResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [panelOpen, setPanelOpen] = useState(false);

  // Fetch slope data
  useEffect(() => {
    const loadSlopeData = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await fetchSlopeVisualization();
        setSlopeData(data);
      } catch {
        setError("Failed to load terrain data");
      } finally {
        setLoading(false);
      }
    };
    loadSlopeData();
  }, []);

  return (
    <div className={`flex w-full h-full relative ${className}`}>
      <LeafletMap
        center={[MAP_CONFIG.center.lat, MAP_CONFIG.center.lng]}
        zoom={MAP_CONFIG.zoom}
        className="h-full w-full"
        zoomControl={true}
        preferCanvas={true}
      >
        <TileLayer url={MAP_CONFIG.tileUrl} attribution={MAP_CONFIG.tileAttribution} />
        <SlopeLayer data={slopeData} visible={showSlope} />
      </LeafletMap>

      {/* Loading/Error states */}
      {loading && (
        <div className="absolute top-4 left-4 z-[1000] bg-slate-800/95 backdrop-blur-sm border border-slate-700 rounded-xl px-4 py-2 text-white text-sm">
          A carregar dados de relevo...
        </div>
      )}
      {error && (
        <div className="absolute top-4 left-4 z-[1000] bg-amber-500/90 backdrop-blur-sm border border-amber-400 rounded-xl px-4 py-2 text-white text-sm">
          ⚠️ Dados de terreno não disponíveis. Execute o processamento SRTM para ativar a camada de declive.
        </div>
      )}

      {/* Controls */}
      <button
        onClick={() => setPanelOpen(!panelOpen)}
        className="absolute top-4 right-4 z-[1000] bg-slate-800/95 backdrop-blur-sm border border-slate-700 rounded-xl p-3 text-white hover:bg-slate-700"
      >
        {panelOpen ? <X className="w-5 h-5" /> : <Layers className="w-5 h-5" />}
      </button>

      <div
        className={`absolute top-4 right-16 z-[1000] bg-slate-800/95 backdrop-blur-sm border border-slate-700 rounded-xl overflow-hidden transition-all duration-300 ${
          panelOpen ? "w-72 opacity-100" : "w-0 opacity-0 pointer-events-none"
        }`}
      >
        <div className="w-72 p-4 space-y-4">
          <h3 className="text-white font-semibold text-sm border-b border-slate-600 pb-2">
            Camadas de Terreno
          </h3>

          {/* Slope layer toggle */}
          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={showSlope}
              onChange={(e) => setShowSlope(e.target.checked)}
              className="w-4 h-4 rounded border-slate-500 text-blue-500 focus:ring-blue-500"
            />
            <span className="text-white text-sm">Declive do Terreno</span>
          </label>

          {/* Slope legend */}
          {showSlope && (
            <div className="border-t border-slate-600 pt-3 mt-3">
              <h4 className="text-slate-400 text-xs font-medium mb-2">Legenda de Declive</h4>
              <div className="space-y-1.5">
                {(Object.keys(SLOPE_LABELS) as SlopeCategory[]).map((key) => (
                  <div key={key} className="flex items-center gap-2">
                    <div
                      className="w-4 h-4 rounded-full"
                      style={{ backgroundColor: SLOPE_COLORS[key] }}
                    />
                    <span className="text-slate-300 text-xs">{SLOPE_LABELS[key]}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}