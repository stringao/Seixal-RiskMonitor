"use client";

import { useEffect, useRef } from "react";
import { useMap } from "react-leaflet";
import L from "leaflet";
import { parseWktToLatLngs } from "./RiskZoneLayer";

export interface HeatDataPoint {
  latitude: number;
  longitude: number;
  temperature?: number;
  humidity?: number;
  windSpeed?: number;
  /** WKT point string for parsing (alternative to lat/lon) */
  wkt?: string;
}

interface HeatLayerProps {
  dataPoints: HeatDataPoint[];
  showTemperature?: boolean;
  showHumidity?: boolean;
  showWind?: boolean;
  opacity?: number;
}

// Color scales for each layer
const TEMPERATURE_COLOR = "#ef4444"; // red
const HUMIDITY_COLOR = "#3b82f6"; // blue
const WIND_COLOR = "#22c55e"; // green

// Base radius for intensity calculation
const BASE_RADIUS = 100;

/**
 * Calculate radius based on value intensity
 * Higher values = larger radius
 */
function calculateRadius(value: number, maxValue: number = 50): number {
  const intensity = Math.min(value / maxValue, 1);
  return BASE_RADIUS + intensity * BASE_RADIUS;
}

/**
 * Parse WKT POINT string to [lat, lng]
 * WKT format: POINT(lon lat)
 */
export function parseWktPoint(wkt: string): [number, number] | null {
  const match = wkt.match(/POINT\s*\(\s*([-\d.]+)\s+([-\d.]+)\s*\)/i);
  if (!match || !match[1] || !match[2]) {
    return null;
  }
  const lng = parseFloat(match[1]);
  const lat = parseFloat(match[2]);
  return [lat, lng];
}

function HeatLayerInner({
  dataPoints,
  showTemperature = true,
  showHumidity = true,
  showWind = true,
  opacity = 0.35,
}: HeatLayerProps) {
  const map = useMap();
  const layersRef = useRef<{
    temperature: L.LayerGroup | null;
    humidity: L.LayerGroup | null;
    wind: L.LayerGroup | null;
  }>({ temperature: null, humidity: null, wind: null });

  useEffect(() => {
    if (!map) return;

    // Clean up previous layers
    Object.values(layersRef.current).forEach((layer) => {
      if (layer) map.removeLayer(layer);
    });

    // Initialize layer groups
    layersRef.current = {
      temperature: L.layerGroup(),
      humidity: L.layerGroup(),
      wind: L.layerGroup(),
    };

    dataPoints.forEach((point) => {
      // Get position from lat/lon or WKT
      let lat: number;
      let lng: number;

      if (point.wkt) {
        const parsed = parseWktPoint(point.wkt);
        if (!parsed) return;
        [lat, lng] = parsed;
      } else {
        lat = point.latitude;
        lng = point.longitude;
      }

      const position: L.LatLngExpression = [lat, lng];

      // Temperature layer (red circles)
      if (showTemperature && point.temperature !== undefined) {
        const radius = calculateRadius(point.temperature, 45); // Max temp ~45°C
        const circle = L.circleMarker(position, {
          radius,
          color: TEMPERATURE_COLOR,
          fillColor: TEMPERATURE_COLOR,
          fillOpacity: opacity,
          weight: 1,
        });
        circle.bindTooltip(`🌡️ ${point.temperature}°C`, {
          permanent: false,
          direction: "top",
          className: "heat-tooltip heat-tooltip-temp",
        });
        layersRef.current.temperature?.addLayer(circle);
      }

      // Humidity layer (blue circles)
      if (showHumidity && point.humidity !== undefined) {
        const radius = calculateRadius(point.humidity, 100); // Max humidity 100%
        const circle = L.circleMarker(position, {
          radius,
          color: HUMIDITY_COLOR,
          fillColor: HUMIDITY_COLOR,
          fillOpacity: opacity,
          weight: 1,
        });
        circle.bindTooltip(`💧 ${point.humidity}%`, {
          permanent: false,
          direction: "top",
          className: "heat-tooltip heat-tooltip-humidity",
        });
        layersRef.current.humidity?.addLayer(circle);
      }

      // Wind layer (green circles)
      if (showWind && point.windSpeed !== undefined) {
        const radius = calculateRadius(point.windSpeed, 100); // Max wind ~100 km/h
        const circle = L.circleMarker(position, {
          radius,
          color: WIND_COLOR,
          fillColor: WIND_COLOR,
          fillOpacity: opacity,
          weight: 1,
        });
        circle.bindTooltip(`🌬️ ${point.windSpeed} km/h`, {
          permanent: false,
          direction: "top",
          className: "heat-tooltip heat-tooltip-wind",
        });
        layersRef.current.wind?.addLayer(circle);
      }
    });

    // Add layers to map
    if (showTemperature && layersRef.current.temperature) {
      map.addLayer(layersRef.current.temperature);
    }
    if (showHumidity && layersRef.current.humidity) {
      map.addLayer(layersRef.current.humidity);
    }
    if (showWind && layersRef.current.wind) {
      map.addLayer(layersRef.current.wind);
    }

    return () => {
      Object.values(layersRef.current).forEach((layer) => {
        if (layer) map.removeLayer(layer);
      });
    };
  }, [map, dataPoints, showTemperature, showHumidity, showWind, opacity]);

  return null;
}

export function HeatLayer(props: HeatLayerProps) {
  return <HeatLayerInner {...props} />;
}

// Re-export parseWktToLatLngs for use by RiskZoneLayer
export { parseWktToLatLngs } from "./RiskZoneLayer";
