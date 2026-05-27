"use client";

import { useEffect, useRef } from "react";
import { MapContainer as LeafletMap, TileLayer, useMap, Polyline, Tooltip } from "react-leaflet";
import { MAP_CONFIG } from "@/lib/map/config";
import { EventMarker } from "./EventMarker";
import { FireStationMarker } from "./FireStationMarker";
import type { GeoEvent } from "@/lib/types/event";
import type { FireStation } from "@/lib/types/fireStation";
import type { RouteInfo } from "./MapContainer";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

// Fix leaflet default icon issue in webpack
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

// OGC API endpoint for CAOP2025 municipalities
const MUNICIPIOS_API = "https://ogcapi.dgterritorio.gov.pt/collections/municipios/items";
const DISTRITOS_API = "https://ogcapi.dgterritorio.gov.pt/collections/distritos/items";
const SETUBAL_BBOX = "-9.5,38.2,-8.2,39.0";

// Route line colors by index
const ROUTE_COLORS = ["#3b82f6", "#22c55e", "#f97316"]; // blue, green, orange

interface MapContentsProps {
  events: GeoEvent[];
  fireStations: FireStation[];
  showBoundaries: boolean;
  boundaryLevel: "municipios" | "distritos" | "both";
  onFireStationClick?: (station: FireStation) => void;
  selectedIncidentId: string | null;
  onIncidentClick?: (eventId: string) => void;
  clearSelection?: () => void;
  routes: RouteInfo[];
  loadingRoutes?: boolean;
}

// ─── Child components that live INSIDE MapContainer context ───

function MapResizer() {
  const map = useMap();
  useEffect(() => {
    const timer = setTimeout(() => map.invalidateSize({ pan: false }), 100);
    return () => clearTimeout(timer);
  }, [map]);
  return null;
}

function MapClickHandler({ clearSelection }: { clearSelection?: () => void }) {
  const map = useMap();
  useEffect(() => {
    const handler = (e: L.LeafletMouseEvent) => {
      const target = e.originalEvent.target as HTMLElement;
      if (target.closest('.leaflet-marker-icon') || target.closest('.leaflet-popup')) return;
      clearSelection?.();
    };
    map.on('click', handler);
    return () => { map.off('click', handler); };
  }, [map, clearSelection]);
  return null;
}

function BoundaryLayer({ visibility, level }: { visibility: boolean; level: "municipios" | "distritos" | "both" }) {
  const map = useMap();
  const layerRef = useRef<L.LayerGroup | null>(null);

  useEffect(() => {
    if (!map) return;
    if (layerRef.current) { map.removeLayer(layerRef.current); layerRef.current = null; }
    if (!visibility) return;

    const group = L.layerGroup();
    layerRef.current = group;
    map.addLayer(group);

    const styleMunicipios = () => ({ color: "#1e40af", weight: 1.5, opacity: 0.7, fillColor: "#3b82f6", fillOpacity: 0.08 });
    const styleDistritos = () => ({ color: "#065f46", weight: 2, opacity: 0.8, fillColor: "#059669", fillOpacity: 0.05 });

    const fetchAndAdd = async () => {
      if (level === "municipios" || level === "both") {
        try {
          const res = await fetch(`${MUNICIPIOS_API}?bbox=${SETUBAL_BBOX}&f=json&limit=100`);
          if (res.ok) {
            const geojson = await res.json();
            if (geojson.features?.length) {
              group.addLayer(L.geoJSON(geojson as GeoJSON.FeatureCollection, {
                style: styleMunicipios,
                onEachFeature: (feature, layer) => {
                  const props = feature.properties as Record<string, unknown>;
                  const name = (props.municipio ?? props.distrito_ilha ?? "Municipio") as string;
                  layer.bindTooltip(name, { sticky: true, className: "boundary-tooltip", direction: "center" });
                },
              }));
            }
          }
        } catch (e) { console.error("Failed to load municipios GeoJSON", e); }
      }
      if (level === "distritos" || level === "both") {
        try {
          const res = await fetch(`${DISTRITOS_API}?bbox=${SETUBAL_BBOX}&f=json&limit=50`);
          if (res.ok) {
            const geojson = await res.json();
            if (geojson.features?.length) {
              group.addLayer(L.geoJSON(geojson as GeoJSON.FeatureCollection, {
                style: styleDistritos,
                onEachFeature: (feature, layer) => {
                  const props = feature.properties as Record<string, unknown>;
                  const name = (props.distrito_ilha ?? "Distrito") as string;
                  layer.bindTooltip(name, { sticky: true, className: "boundary-tooltip boundary-tooltip-distrito", direction: "center" });
                },
              }));
            }
          }
        } catch (e) { console.error("Failed to load distritos GeoJSON", e); }
      }
    };
    fetchAndAdd();

    return () => { if (layerRef.current) { map.removeLayer(layerRef.current); layerRef.current = null; } };
  }, [map, visibility, level]);

  return null;
}

// ─── MapContents: the component that uses useMap and lives INSIDE MapContainer ───
function MapContents({
  events, fireStations, showBoundaries, boundaryLevel,
  onFireStationClick, selectedIncidentId, onIncidentClick,
  clearSelection, routes, loadingRoutes,
}: MapContentsProps) {
  const selectedEvent = events.find(e => e.id === selectedIncidentId);

  return (
    <>
      <TileLayer url={MAP_CONFIG.tileUrl} attribution={MAP_CONFIG.tileAttribution} />
      <MapResizer />
      <MapClickHandler clearSelection={clearSelection} />
      <BoundaryLayer visibility={showBoundaries} level={boundaryLevel} />

      {loadingRoutes && selectedEvent && (
        <Polyline
          positions={[
            [selectedEvent.latitude, selectedEvent.longitude],
          ]}
          pathOptions={{ color: "#94a3b8", weight: 2, dashArray: "4, 8" }}
        />
      )}

      {routes.map((route, index) => (
        <Polyline
          key={`route-${route.station.id}`}
          positions={route.geometry}
          pathOptions={{ color: ROUTE_COLORS[index], weight: 3, dashArray: undefined }}
        >
          <Tooltip permanent direction="center" offset={[0, -10]}>
            <span style={{
              backgroundColor: ROUTE_COLORS[index], color: "white",
              padding: "2px 6px", borderRadius: "4px", fontSize: "11px", fontWeight: "bold",
            }}>
              {(route.distance / 1000).toFixed(1)} km
            </span>
          </Tooltip>
        </Polyline>
      ))}

      {events.map((event) => (
        <EventMarker
          key={event.id}
          event={event}
          isSelected={event.id === selectedIncidentId}
          onClick={onIncidentClick}
          dimmed={selectedIncidentId !== null && event.id !== selectedIncidentId}
        />
      ))}
      {fireStations.map((station) => (
        <FireStationMarker
          key={station.id}
          station={station}
          onClick={onFireStationClick}
          dimmed={selectedIncidentId !== null}
        />
      ))}
    </>
  );
}

// ─── MapInner: the outer wrapper that provides MapContainer context ───
interface MapInnerProps {
  events: GeoEvent[];
  fireStations?: FireStation[];
  showBoundaries?: boolean;
  boundaryLevel?: "municipios" | "distritos" | "both";
  onFireStationClick?: (station: FireStation) => void;
  selectedIncidentId?: string | null;
  onIncidentClick?: (eventId: string) => void;
  clearSelection?: () => void;
  routes?: RouteInfo[];
  loadingRoutes?: boolean;
}

export function MapInner({
  events, fireStations = [], showBoundaries = false,
  boundaryLevel = "municipios", onFireStationClick,
  selectedIncidentId, onIncidentClick, clearSelection,
  routes = [], loadingRoutes = false,
}: MapInnerProps) {
  return (
    <LeafletMap
      center={[MAP_CONFIG.center.lat, MAP_CONFIG.center.lng]}
      zoom={MAP_CONFIG.zoom}
      className="h-full w-full"
      zoomControl={true}
      preferCanvas={true}
    >
      <MapContents
        events={events}
        fireStations={fireStations}
        showBoundaries={showBoundaries}
        boundaryLevel={boundaryLevel}
        onFireStationClick={onFireStationClick}
        selectedIncidentId={selectedIncidentId ?? null}
        onIncidentClick={onIncidentClick}
        clearSelection={clearSelection}
        routes={routes}
        loadingRoutes={loadingRoutes}
      />
    </LeafletMap>
  );
}
