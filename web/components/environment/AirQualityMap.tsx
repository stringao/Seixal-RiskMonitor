"use client";

import { useEffect, useRef } from "react";
import {
  MapContainer as LeafletMap,
  TileLayer,
  Marker,
  Popup,
  Circle,
  useMap,
} from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { MAP_CONFIG } from "@/lib/map/config";
import { AQI_LEVELS } from "@/lib/types/environment";
import type { AirQualityResponse, PollenResponse } from "@/lib/types/environment";

// Fix leaflet default icon issue in webpack
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

// Custom AQI monitoring station icon
function createStationIcon(aqi: number, color: string): L.DivIcon {
  const size = 44;
  return L.divIcon({
    className: "aqi-station-marker",
    html: `
      <div style="position:relative;width:${size}px;height:${size}px;">
        <svg width="${size}" height="${size}" viewBox="0 0 44 44" fill="none" xmlns="http://www.w3.org/2000/svg">
          <circle cx="22" cy="22" r="20" fill="${color}22" stroke="${color}" stroke-width="2.5"/>
          <circle cx="22" cy="22" r="12" fill="${color}" opacity="0.3"/>
          <circle cx="22" cy="22" r="6" fill="${color}"/>
          <text x="22" y="26" text-anchor="middle" fill="white" font-size="10" font-weight="bold" font-family="system-ui">${aqi}</text>
        </svg>
        <div style="position:absolute;bottom:-20px;left:50%;transform:translateX(-50%);white-space:nowrap;
          background:${color};color:white;font-size:9px;font-weight:600;padding:1px 6px;border-radius:4px;
          font-family:system-ui;">Seixal</div>
      </div>`,
    iconSize: [size, size + 24],
    iconAnchor: [size / 2, size / 2],
    popupAnchor: [0, -(size / 2)],
  });
}

interface AirQualityMapProps {
  airQualityData: AirQualityResponse | null;
  pollenData: PollenResponse | null;
}

function MapController({ center }: { center: [number, number] }) {
  const map = useMap();
  const hasCentered = useRef(false);

  useEffect(() => {
    if (!hasCentered.current) {
      map.setView(center, 13, { animate: true });
      hasCentered.current = true;
    }
  }, [map, center]);

  return null;
}

export function AirQualityMap({ airQualityData, pollenData }: AirQualityMapProps) {
  const center: [number, number] = [MAP_CONFIG.center.lat, MAP_CONFIG.center.lng];

  const aqiValue = airQualityData?.aqi ?? 1;
  const aqiColor = airQualityData?.aqiLevel?.color ?? "#00E400";
  const aqiLabel = airQualityData?.aqiLevel?.label ?? "N/A";
  const healthRisk = airQualityData?.healthRisk ?? "N/A";

  const pollenIndex = pollenData?.pollenIndex ?? 1;
  const pollenLabel = pollenData?.label ?? "N/A";
  const dominantPollen = pollenData?.dominantPollen ?? "N/A";

  return (
    <div className="bg-gradient-to-br from-slate-800/40 to-slate-800/20 border border-slate-700/50 rounded-2xl overflow-hidden">
      <div className="px-5 py-3 border-b border-slate-700/50 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <circle cx="8" cy="8" r="7" fill={aqiColor} opacity="0.3" />
            <circle cx="8" cy="8" r="4" fill={aqiColor} />
          </svg>
          <span className="text-sm font-medium text-slate-300">
            Estação de Monitorização — Seixal
          </span>
        </div>
        <div className="flex items-center gap-3 text-xs">
          <span className="flex items-center gap-1">
            <span className="w-2 h-2 rounded-full" style={{ backgroundColor: aqiColor }} />
            <span className="text-slate-400">AQI: {aqiLabel}</span>
          </span>
          <span className="flex items-center gap-1">
            <span className="w-2 h-2 rounded-full bg-yellow-400" />
            <span className="text-slate-400">Polén: {pollenLabel}</span>
          </span>
        </div>
      </div>

      <div style={{ height: "400px", width: "100%" }}>
        <LeafletMap
          center={center}
          zoom={13}
          className="h-full w-full"
          zoomControl={false}
          attributionControl={false}
          preferCanvas={true}
        >
          <MapController center={center} />

          <TileLayer
            url={MAP_CONFIG.tileUrl}
            attribution={MAP_CONFIG.tileAttribution}
          />

          {/* Influence radius circle */}
          <Circle
            center={center}
            radius={2000}
            pathOptions={{
              color: aqiColor,
              fillColor: aqiColor,
              fillOpacity: 0.08,
              weight: 1.5,
              dashArray: "6 4",
            }}
          />

          {/* Monitoring station marker */}
          <Marker
            position={center}
            icon={createStationIcon(aqiValue, aqiColor)}
          >
            <Popup>
              <div style={{ fontFamily: "system-ui, sans-serif", minWidth: "220px" }}>
                <div style={{ fontWeight: 700, fontSize: "14px", marginBottom: "8px", color: "#f1f5f9" }}>
                  📍 Estação de Monitorização do Seixal
                </div>

                <div style={{ display: "grid", gap: "6px", fontSize: "12px" }}>
                  <div style={{ display: "flex", justifyContent: "space-between" }}>
                    <span style={{ color: "#94a3b8" }}>Qualidade do Ar</span>
                    <span style={{ color: aqiColor, fontWeight: 600 }}>{aqiLabel} ({aqiValue}/5)</span>
                  </div>
                  <div style={{ display: "flex", justifyContent: "space-between" }}>
                    <span style={{ color: "#94a3b8" }}>Risco Saúde</span>
                    <span style={{ color: "#fbbf24", fontWeight: 600 }}>{healthRisk}</span>
                  </div>
                  <div style={{ display: "flex", justifyContent: "space-between" }}>
                    <span style={{ color: "#94a3b8" }}>Índice Polén</span>
                    <span style={{ color: "#facc15", fontWeight: 600 }}>{pollenLabel} ({pollenIndex}/5)</span>
                  </div>
                  <div style={{ display: "flex", justifyContent: "space-between" }}>
                    <span style={{ color: "#94a3b8" }}>Polén Dominante</span>
                    <span style={{ color: "#e2e8f0", fontWeight: 500 }}>{dominantPollen}</span>
                  </div>
                </div>

                {airQualityData && airQualityData.pollutants.length > 0 && (
                  <div style={{ marginTop: "8px", paddingTop: "8px", borderTop: "1px solid #334155", fontSize: "11px" }}>
                    <div style={{ color: "#94a3b8", marginBottom: "4px" }}>Poluentes:</div>
                    {airQualityData.pollutants.slice(0, 4).map((p) => (
                      <div key={p.symbol} style={{ display: "flex", justifyContent: "space-between", marginBottom: "2px" }}>
                        <span style={{ color: "#cbd5e1" }}>{p.symbol}</span>
                        <span style={{ color: p.color }}>{p.value} {p.unit}</span>
                      </div>
                    ))}
                  </div>
                )}

                <div style={{ marginTop: "8px", fontSize: "10px", color: "#64748b" }}>
                  Coordenadas: {center[0].toFixed(4)}°N, {Math.abs(center[1]).toFixed(4)}°W
                </div>
              </div>
            </Popup>
          </Marker>

          {/* Legend overlay */}
          <div className="leaflet-bottom leaflet-left" style={{ pointerEvents: "auto" }}>
            <div
              style={{
                background: "rgba(15, 23, 42, 0.9)",
                backdropFilter: "blur(8px)",
                border: "1px solid rgba(51, 65, 85, 0.5)",
                borderRadius: "8px",
                padding: "8px 12px",
                margin: "10px",
                fontSize: "11px",
              }}
            >
              <div style={{ color: "#94a3b8", fontWeight: 600, marginBottom: "4px" }}>Índice AQI</div>
              {Object.entries(AQI_LEVELS).map(([, config]) => (
                <div key={config.label} style={{ display: "flex", alignItems: "center", gap: "6px", marginBottom: "2px" }}>
                  <span
                    style={{
                      width: "10px",
                      height: "10px",
                      borderRadius: "50%",
                      backgroundColor: config.color,
                      display: "inline-block",
                    }}
                  />
                  <span style={{ color: "#cbd5e1" }}>{config.label}</span>
                </div>
              ))}
            </div>
          </div>
        </LeafletMap>
      </div>
    </div>
  );
}
