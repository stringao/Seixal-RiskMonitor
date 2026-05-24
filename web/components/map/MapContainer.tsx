"use client";

import dynamic from "next/dynamic";
import { useState, useCallback } from "react";
import { useEvents } from "@/lib/hooks/useEvents";
import { EventFilters } from "@/lib/types/event";
import { MapFilters } from "./MapFilters";
import { Legend } from "./Legend";

const MapInner = dynamic(() => import("./MapInner").then((m) => m.MapInner), {
  ssr: false,
  loading: () => (
    <div className="h-full w-full bg-slate-900 flex items-center justify-center">
      <div className="text-slate-400">A carregar mapa...</div>
    </div>
  ),
});

export default function MapContainer() {
  const [filters, setFilters] = useState<EventFilters>({});
  const { data, loading, error } = useEvents(filters);

  const handleFilterChange = useCallback((newFilters: EventFilters) => {
    setFilters(newFilters);
  }, []);

  return (
    <div className="flex h-full">
      <div className="flex-1 relative">
        {error && (
          <div className="absolute top-4 left-1/2 -translate-x-1/2 z-[1000] bg-red-500/90 text-white px-4 py-2 rounded-lg text-sm">
            {error}
          </div>
        )}
        <MapInner events={data?.items ?? []} loading={loading} />
        <div className="absolute bottom-6 right-6 z-[1000]">
          <Legend />
        </div>
      </div>
      <div className="w-72 bg-slate-800/80 border-l border-slate-700 overflow-y-auto">
        <MapFilters filters={filters} onChange={handleFilterChange} />
      </div>
    </div>
  );
}
