"use client";

import { useEffect } from "react";
import { MapContainer, TileLayer, useMap, Marker, Tooltip } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { MAP_CONFIG } from "@/lib/map/config";
import type { FireSpreadResponse } from "@/lib/types/riskZone";
import { SpreadEllipse } from "./SpreadEllipse";

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

const HORIZON_COLORS: Record<number, string> = {
  1: "#22c55e",
  2: "#eab308",
  4: "#f97316",
  8: "#ef4444",
  12: "#991b1b",
};

interface FireSpreadMapProps {
  fireSpread: FireSpreadResponse;
  selectedHorizon: number;
  onHorizonChange: (horizon: number) => void;
}

function FireSpreadMapContents({ fireSpread, selectedHorizon, onHorizonChange }: FireSpreadMapProps) {
  const map = useMap();

  useEffect(() => {
    if (map && fireSpread.fireLocation) {
      map.setView(
        [fireSpread.fireLocation.latitude, fireSpread.fireLocation.longitude],
        11,
        { animate: true }
      );
    }
  }, [map, fireSpread.fireLocation]);

  const fireIcon = new L.Icon({
    iconUrl: "https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-red.png",
    iconRetinaUrl: "https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-red.png",
    shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
    iconSize: [25, 41],
    iconAnchor: [12, 41],
    popupAnchor: [1, -34],
  });

  return (
    <>
      <TileLayer url={MAP_CONFIG.tileUrl} attribution={MAP_CONFIG.tileAttribution} />
      <Marker
        position={[fireSpread.fireLocation.latitude, fireSpread.fireLocation.longitude]}
        icon={fireIcon}
      >
        <Tooltip permanent direction="top" offset={[0, -30]}>
          <span className="bg-red-600 text-white px-2 py-1 rounded text-sm font-bold">
            {fireSpread.fireLocation.title}
          </span>
        </Tooltip>
      </Marker>

      {fireSpread.horizons.map((prediction) => {
        const hours = prediction.hours;
        const color = HORIZON_COLORS[hours] || "#888888";
        return (
          <SpreadEllipse
            key={hours}
            prediction={prediction}
            color={color}
            onClick={() => onHorizonChange(hours)}
          />
        );
      })}
    </>
  );
}

export function FireSpreadMap({ fireSpread, selectedHorizon, onHorizonChange }: FireSpreadMapProps) {
  return (
    <MapContainer
      center={[fireSpread.fireLocation.latitude, fireSpread.fireLocation.longitude]}
      zoom={11}
      className="h-full w-full"
      zoomControl={true}
      preferCanvas={true}
    >
      <FireSpreadMapContents
        fireSpread={fireSpread}
        selectedHorizon={selectedHorizon}
        onHorizonChange={onHorizonChange}
      />
    </MapContainer>
  );
}