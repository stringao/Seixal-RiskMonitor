"use client";

import dynamic from "next/dynamic";
import { useState, useEffect } from "react";
import { useActiveFiresSpread } from "@/lib/hooks/useFireSpread";
import { SpreadPanel } from "@/components/fire-spread/SpreadPanel";
import type { FireSpreadResponse } from "@/lib/types/riskZone";

// Dynamic import for Leaflet map (no SSR)
const FireSpreadMapInner = dynamic(
  () => import("@/components/fire-spread/FireSpreadMap").then((mod) => mod.FireSpreadMap),
  {
    ssr: false,
    loading: () => (
      <div className="flex items-center justify-center h-full bg-[#1a1a2e]">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[#ef4444]" />
      </div>
    ),
  }
);

export default function FireSpreadPage() {
  const [selectedFireEventId, setSelectedFireEventId] = useState<string | null>(null);
  const [selectedHorizon, setSelectedHorizon] = useState<number>(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const { data: activeFires, loading, error } = useActiveFiresSpread(refreshKey);

  const selectedFire = activeFires?.find((f) => f.fireEventId === selectedFireEventId) || null;

  // Auto-select first fire when data loads
  useEffect(() => {
    if (activeFires && activeFires.length > 0 && !selectedFireEventId) {
      setSelectedFireEventId(activeFires[0].fireEventId);
    }
  }, [activeFires, selectedFireEventId]);

  const handleRefresh = () => setRefreshKey((k) => k + 1);

  return (
      <div className="h-full flex bg-[#0f0f1a]">
        {/* Map area - 70% */}
        <div className="flex-1 h-full relative">
          {loading ? (
            <div className="flex items-center justify-center h-full bg-[#1a1a2e]">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[#ef4444]" />
            </div>
          ) : error ? (
            <div className="flex items-center justify-center h-full bg-[#1a1a2e]">
              <div className="text-center">
                <p className="text-red-400 mb-2">Erro ao carregar dados</p>
                <p className="text-gray-400 text-sm">{error}</p>
                <button
                  onClick={handleRefresh}
                  className="mt-4 px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded text-sm transition-colors"
                >
                  Tentar novamente
                </button>
              </div>
            </div>
          ) : activeFires && activeFires.length > 0 && selectedFire ? (
            <FireSpreadMapInner
              fireSpread={selectedFire}
              selectedHorizon={selectedHorizon}
              onHorizonChange={setSelectedHorizon}
            />
          ) : (
            <div className="flex items-center justify-center h-full bg-[#1a1a2e]">
              <div className="text-center">
                <p className="text-gray-400 mb-2">Nenhum incêndio ativo</p>
                <p className="text-gray-500 text-sm">Não há incêndios ativos para visualizar</p>
              </div>
            </div>
          )}
        </div>

        {/* Panel area - 30% */}
        <div className="w-[400px] h-full overflow-hidden">
          {selectedFire ? (
            <SpreadPanel
              fireSpread={selectedFire}
              selectedHorizon={selectedHorizon}
              onHorizonChange={setSelectedHorizon}
            />
          ) : (
            <div className="h-full flex items-center justify-center bg-[#1a1a2e] p-4">
              <div className="text-center">
                <p className="text-gray-400 mb-2">Selecione um incêndio</p>
                <p className="text-gray-500 text-sm">Utilize o selector para escolher um incêndio ativo</p>
              </div>
            </div>
          )}
        </div>

        {/* Fire selector dropdown - only shown when multiple fires */}
        {activeFires && activeFires.length > 1 && (
          <div className="absolute top-4 left-1/2 transform -translate-x-1/2 z-[1000]">
            <select
              value={selectedFireEventId || ""}
              onChange={(e) => setSelectedFireEventId(e.target.value)}
              className="bg-[#1a1a2e] text-white border border-gray-600 rounded-lg px-4 py-2 text-sm shadow-lg focus:outline-none focus:ring-2 focus:ring-red-500"
            >
              {activeFires.map((fire) => (
                <option key={fire.fireEventId} value={fire.fireEventId}>
                  {fire.fireLocation.title}
                </option>
              ))}
            </select>
          </div>
        )}
      </div>
  );
}
