"use client";

import { MapContainer as LeafletMap, TileLayer } from "react-leaflet";
import { MAP_CONFIG } from "@/lib/map/config";
import { EventMarker } from "./EventMarker";
import type { GeoEvent } from "@/lib/types/event";
import "leaflet/dist/leaflet.css";

interface MapInnerProps {
  events: GeoEvent[];
  loading: boolean;
}

export function MapInner({ events }: MapInnerProps) {
  return (
    <LeafletMap
      center={[MAP_CONFIG.center.lat, MAP_CONFIG.center.lng]}
      zoom={MAP_CONFIG.zoom}
      className="h-full w-full z-0"
      zoomControl={true}
    >
      <TileLayer url={MAP_CONFIG.tileUrl} attribution={MAP_CONFIG.tileAttribution} />
      {events.map((event) => (
        <EventMarker key={event.id} event={event} />
      ))}
    </LeafletMap>
  );
}
