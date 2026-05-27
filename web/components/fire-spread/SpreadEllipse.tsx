"use client";

import { useEffect, useRef } from "react";
import { useMap } from "react-leaflet";
import L from "leaflet";
import type { HorizonPrediction } from "@/lib/types/riskZone";

interface SpreadEllipseProps {
  prediction: HorizonPrediction;
  color: string;
  onClick?: () => void;
}

function parsePolygonWkt(wkt: string): L.LatLngExpression[] {
  const inner = wkt
    .replace(/^POLYGON\s*\(\s*\(/i, "")
    .replace(/\)\s*$/, "")
    .trim();

  const pairs = inner.split(",").map((pair) => {
    const parts = pair.trim().split(/\s+/);
    if (parts.length < 2) return null;
    const lon = parseFloat(parts[0]);
    const lat = parseFloat(parts[1]);
    if (isNaN(lon) || isNaN(lat)) return null;
    return [lat, lon] as L.LatLngExpression;
  });

  return pairs.filter((p): p is L.LatLngExpression => p !== null);
}

export function SpreadEllipse({ prediction, color, onClick }: SpreadEllipseProps) {
  const map = useMap();
  const layerRef = useRef<L.Polygon | null>(null);

  useEffect(() => {
    if (!map) return;

    try {
      const latLngs = parsePolygonWkt(prediction.polygonWkt);

      if (latLngs.length === 0) {
        console.warn("No valid coordinates in polygon WKT:", prediction.polygonWkt);
        return;
      }

      const hasNaN = latLngs.some(
        (ll) => !ll || !Array.isArray(ll) || isNaN(ll[0] as number) || isNaN(ll[1] as number)
      );
      if (hasNaN) {
        console.warn("NaN detected in polygon coordinates:", latLngs);
        return;
      }

      const polygon = L.polygon(latLngs, {
        color,
        fillColor: color,
        fillOpacity: 0.2,
        weight: 2,
      });

      if (onClick) {
        polygon.on("click", onClick);
      }

      polygon.addTo(map);
      layerRef.current = polygon;
    } catch (err) {
      console.error("Error creating polygon:", err);
    }

    return () => {
      if (layerRef.current) {
        layerRef.current.remove();
        layerRef.current = null;
      }
    };
  }, [map, prediction, color, onClick]);

  return null;
}