"use client";

import { useEffect, useState, useCallback } from "react";
import { Marker, Tooltip } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

export interface SatelliteFireDetection {
  id: string;
  latitude: number;
  longitude: number;
  captureTime: string;
  source: string;
  fireRadiativePower: number | null;
  brightnessTemperature: number | null;
  isActive: boolean;
  detectionConfidence: string | null;
}

interface SatelliteLayerProps {
  detections: SatelliteFireDetection[];
  visible: boolean;
  onDetectionClick?: (detection: SatelliteFireDetection) => void;
}

// Fire icon for satellite detections
function createFireIcon(confidence: string | null, isActive: boolean): L.DivIcon {
  const baseColor = isActive ? "#ef4444" : "#888888";
  const pulseClass = isActive ? "satellite-pulse" : "";

  return L.divIcon({
    className: "satellite-fire-marker",
    html: `<div class="${pulseClass}" style="background-color:${baseColor};width:16px;height:16px;border-radius:50%;border:2px solid white;box-shadow:0 0 8px ${baseColor}80;cursor:pointer;"></div>`,
    iconSize: [16, 16],
    iconAnchor: [8, 8],
  });
}

// Confidence badge color
function getConfidenceColor(confidence: string | null): string {
  return confidence === "high" ? "#22c55e" : confidence === "nominal" ? "#eab308" : "#94a3b8";
}

function formatCaptureTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));

  if (diffHours < 1) return "Agora";
  if (diffHours < 24) return `${diffHours}h atrás`;
  const diffDays = Math.floor(diffHours / 24);
  return `${diffDays}d atrás`;
}

export function SatelliteLayer({ detections, visible, onDetectionClick }: SatelliteLayerProps) {
  if (!visible || detections.length === 0) return null;

  return (
    <>
      {detections.map((detection) => (
        <Marker
          key={detection.id}
          position={[detection.latitude, detection.longitude]}
          icon={createFireIcon(detection.detectionConfidence, detection.isActive)}
          eventHandlers={{
            click: () => onDetectionClick?.(detection),
          }}
        >
          <Tooltip direction="top" offset={[0, -10]}>
            <div className="text-sm" style={{ fontFamily: "system-ui, sans-serif" }}>
              <div className="flex items-center gap-2 mb-1">
                <span
                  className="w-3 h-3 rounded-full"
                  style={{ backgroundColor: getConfidenceColor(detection.detectionConfidence) }}
                />
                <span className="font-bold text-slate-800">
                  {detection.isActive ? "Fogo Ativo" : "Fogo Detetado"}
                </span>
              </div>
              <div className="text-slate-600 space-y-0.5 text-xs">
                <div className="flex gap-2">
                  <span className="font-medium text-slate-500">Hora:</span>
                  <span>{formatCaptureTime(detection.captureTime)}</span>
                </div>
                {detection.fireRadiativePower && (
                  <div className="flex gap-2">
                    <span className="font-medium text-slate-500">FRP:</span>
                    <span>{detection.fireRadiativePower.toFixed(1)} MW</span>
                  </div>
                )}
                {detection.brightnessTemperature && (
                  <div className="flex gap-2">
                    <span className="font-medium text-slate-500">Temp:</span>
                    <span>{detection.brightnessTemperature.toFixed(0)} K</span>
                  </div>
                )}
                <div className="flex gap-2">
                  <span className="font-medium text-slate-500">Fonte:</span>
                  <span>VIIRS/SNPP</span>
                </div>
                {detection.detectionConfidence && (
                  <div className="flex gap-2">
                    <span className="font-medium text-slate-500">Confiança:</span>
                    <span className="capitalize">{detection.detectionConfidence}</span>
                  </div>
                )}
              </div>
            </div>
          </Tooltip>
        </Marker>
      ))}
    </>
  );
}

// Standalone hook for fetching satellite fire data
export function useSatelliteFires() {
  const [detections, setDetections] = useState<SatelliteFireDetection[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchLatest = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch("/api/environment/satellite/fires/latest");
      if (!response.ok) throw new Error("Failed to fetch satellite fire data");
      const data = await response.json();
      setDetections(data.images || []);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unknown error");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLatest();
    const interval = setInterval(fetchLatest, 10 * 60 * 1000);
    return () => clearInterval(interval);
  }, [fetchLatest]);

  return { detections, loading, error, refetch: fetchLatest };
}
