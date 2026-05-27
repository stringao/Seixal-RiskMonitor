"use client";

import dynamic from "next/dynamic";
import { useState, useCallback, useMemo } from "react";
import { Filter, X, Info } from "lucide-react";
import { useEvents } from "@/lib/hooks/useEvents";
import { useFireStations } from "@/lib/hooks/useFireStations";
import { EventFilters } from "@/lib/types/event";
import { MapFilters } from "./MapFilters";
import { Legend } from "./Legend";
import type { GeoEvent } from "@/lib/types/event";
import type { FireStation } from "@/lib/types/fireStation";

const MapInner = dynamic(() => import("./MapInner").then((m) => m.MapInner), {
  ssr: false,
  loading:
  () => (
    <div className="h-full w-full bg-slate-900 flex items-center justify-center">
      <div className="text-slate-400">A carregar mapa...</div>
    </div>
  ),
});

export default function MapContainer() {
  const [filters, setFilters] = useState<EventFilters>({});
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [legendOpen, setLegendOpen] = useState(false);
  const [showBoundaries, setShowBoundaries] = useState(false);
  const [showFireStations, setShowFireStations] = useState(false);
  const [boundaryLevel, setBoundaryLevel] = useState<"municipios" | "distritos" | "both">("municipios");
  const [selectedIncidentId, setSelectedIncidentId] = useState<string | null>(null);
  const { data, error } = useEvents(filters);
  const { data: fireStationsData } = useFireStations({});

  // Calculate nearest 3 fire stations to selected incident
  const nearestStations = useMemo(() => {
    if (!selectedIncidentId) return [];
    
    const selectedEvent = data?.items.find(e => e.id === selectedIncidentId);
    if (!selectedEvent) return [];
    
    const stations = fireStationsData?.items ?? [];
    if (stations.length === 0) return [];
    
    // Calculate Euclidean distance and sort
    const stationsWithDistance = stations.map(station => ({
      station,
      distance: Math.sqrt(
        Math.pow(station.latitude - selectedEvent.latitude, 2) +
        Math.pow(station.longitude - selectedEvent.longitude, 2)
      )
    }));
    
    return stationsWithDistance
      .sort((a, b) => a.distance - b.distance)
      .slice(0, 3);
  }, [selectedIncidentId, data?.items, fireStationsData?.items]);

  const handleFilterChange = useCallback((newFilters: EventFilters) => {
    setFilters(newFilters);
  }, []);

  const handleBoundaryToggle = useCallback((show: boolean) => {
    setShowBoundaries(show);
  }, []);

  const handleBoundaryLevelChange = useCallback((level: "municipios" | "distritos" | "both") => {
    setBoundaryLevel(level);
  }, []);

  const handleFireStationsToggle = useCallback((show: boolean) => {
    setShowFireStations(show);
  }, []);

  const handleIncidentClick = useCallback((eventId: string) => {
    setSelectedIncidentId(eventId);
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedIncidentId(null);
  }, []);

  return (
    <div className="flex w-full h-full relative">
      <MapInner
        events={data?.items ?? []}
        fireStations={showFireStations ? (fireStationsData?.items ?? []) : []}
        showBoundaries={showBoundaries}
        boundaryLevel={boundaryLevel}
        selectedIncidentId={selectedIncidentId}
        onIncidentClick={handleIncidentClick}
        clearSelection={clearSelection}
        nearestStations={nearestStations}
      />
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
        <div className="w-72 p-4">
          <MapFilters
            filters={filters}
            onChange={handleFilterChange}
            showBoundaries={showBoundaries}
            onBoundaryToggle={handleBoundaryToggle}
            boundaryLevel={boundaryLevel}
            onBoundaryLevelChange={handleBoundaryLevelChange}
            showFireStations={showFireStations}
            onFireStationsToggle={handleFireStationsToggle}
          />
        </div>
      </div>
      <div className="absolute bottom-4 right-4 z-[1000] flex flex-col gap-2">
        <div
          className={`absolute bottom-14 right-0 z-[1000] overflow-hidden transition-all duration-300 ${
            legendOpen ? "w-80 opacity-100" : "w-0 opacity-0 pointer-events-none"
          }`}
        >
          <Legend />
        </div>
        <button
          onClick={() => setLegendOpen(!legendOpen)}
          className="bg-slate-800/95 backdrop-blur-sm border border-slate-700 rounded-xl p-3 text-white hover:bg-slate-700 self-end"
        >
          <Info className="w-5 h-5" />
        </button>
      </div>
    </div>
  );
}
