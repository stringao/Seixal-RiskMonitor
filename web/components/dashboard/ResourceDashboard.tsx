"use client";

import { useState } from "react";
import type { ResourceStatusSummary, StationResourceStatus } from "@/lib/types/resource";

interface ResourceDashboardProps {
  status: ResourceStatusSummary | null;
  isLoading?: boolean;
  onRefresh?: () => void;
}

const RESOURCE_TYPE_LABELS: Record<string, string> = {
  Tanker: "Tanques",
  Pump: "Bombas",
  Helicopter: "Helicópteros",
  Team: "Equipas",
  CommandUnit: "Unidades Comando",
};

const STATUS_CONFIG = {
  available: { label: "Disponível", color: "bg-green-500", textColor: "text-green-400" },
  enRoute: { label: "Em Trânsito", color: "bg-blue-500", textColor: "text-blue-400" },
  onScene: { label: "No Local", color: "bg-emerald-500", textColor: "text-emerald-400" },
};

export function ResourceDashboard({ status, isLoading = false, onRefresh }: ResourceDashboardProps) {
  const [selectedStation, setSelectedStation] = useState<StationResourceStatus | null>(null);

  if (isLoading) {
    return (
      <div className="h-full flex items-center justify-center bg-[#1a1a2e]">
        <div className="animate-pulse text-gray-400">A carregar estado dos recursos...</div>
      </div>
    );
  }

  if (!status) {
    return (
      <div className="h-full flex items-center justify-center bg-[#1a1a2e]">
        <div className="text-center">
          <p className="text-gray-400 mb-4">Não foi possível carregar os dados</p>
          {onRefresh && (
            <button
              onClick={onRefresh}
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded transition-colors"
            >
              Tentar Novamente
            </button>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col bg-[#1a1a2e] text-white overflow-y-auto">
      {/* Header */}
      <div className="p-4 border-b border-gray-700 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-white">Dashboard de Recursos</h2>
          <p className="text-sm text-gray-400">Estado atual do sistema SIREN</p>
        </div>
        <button
          onClick={onRefresh}
          className="px-3 py-1.5 bg-gray-700 hover:bg-gray-600 text-white text-sm rounded transition-colors"
        >
          Actualizar
        </button>
      </div>

      {/* Summary cards */}
      <div className="p-4 grid grid-cols-3 gap-4">
        <div className="p-4 bg-[#16213e] rounded-lg border border-gray-700">
          <div className="text-2xl font-bold text-green-400">{status.available}</div>
          <div className="text-sm text-gray-400">Recursos Disponíveis</div>
        </div>
        <div className="p-4 bg-[#16213e] rounded-lg border border-gray-700">
          <div className="text-2xl font-bold text-blue-400">{status.enRoute}</div>
          <div className="text-sm text-gray-400">Em Trânsito</div>
        </div>
        <div className="p-4 bg-[#16213e] rounded-lg border border-gray-700">
          <div className="text-2xl font-bold text-emerald-400">{status.onScene}</div>
          <div className="text-sm text-gray-400">No Local</div>
        </div>
      </div>

      {/* Total resources bar */}
      <div className="px-4 pb-4">
        <div className="p-4 bg-[#16213e] rounded-lg border border-gray-700">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-semibold text-gray-300">Total de Recursos</span>
            <span className="text-lg font-bold text-white">{status.totalResources}</span>
          </div>
          <div className="w-full h-3 bg-gray-700 rounded-full overflow-hidden flex">
            <div
              className="h-full bg-green-500 transition-all"
              style={{ width: `${(status.available / status.totalResources) * 100}%` }}
            />
            <div
              className="h-full bg-blue-500 transition-all"
              style={{ width: `${(status.enRoute / status.totalResources) * 100}%` }}
            />
            <div
              className="h-full bg-emerald-500 transition-all"
              style={{ width: `${(status.onScene / status.totalResources) * 100}%` }}
            />
          </div>
          <div className="flex justify-between mt-2 text-xs text-gray-400">
            <span className="flex items-center gap-1">
              <span className="w-2 h-2 rounded-full bg-green-500" /> Disponível
            </span>
            <span className="flex items-center gap-1">
              <span className="w-2 h-2 rounded-full bg-blue-500" /> Em Trânsito
            </span>
            <span className="flex items-center gap-1">
              <span className="w-2 h-2 rounded-full bg-emerald-500" /> No Local
            </span>
          </div>
        </div>
      </div>

      {/* By Type breakdown */}
      <div className="px-4 pb-4">
        <h3 className="text-sm font-semibold text-gray-300 mb-3 uppercase tracking-wide">
          Por Tipo de Recurso
        </h3>
        <div className="space-y-2">
          {Object.entries(status.byType).map(([type, typeStatus]) => (
            <div
              key={type}
              className="p-3 bg-[#16213e] rounded-lg border border-gray-700 flex items-center justify-between"
            >
              <div className="flex items-center gap-3">
                <span className="text-2xl">
                  {type === "Tanker" && "🚒"}
                  {type === "Pump" && "🚑"}
                  {type === "Helicopter" && "🚁"}
                  {type === "Team" && "👥"}
                  {type === "CommandUnit" && "📡"}
                </span>
                <span className="font-medium text-white">
                  {RESOURCE_TYPE_LABELS[type] || type}
                </span>
              </div>
              <div className="flex items-center gap-4 text-sm">
                <div className="text-center">
                  <div className="text-green-400 font-semibold">{typeStatus.available}</div>
                  <div className="text-gray-400 text-xs">Disp.</div>
                </div>
                <div className="text-center">
                  <div className="text-blue-400 font-semibold">{typeStatus.enRoute}</div>
                  <div className="text-gray-400 text-xs">Transito</div>
                </div>
                <div className="text-center">
                  <div className="text-emerald-400 font-semibold">{typeStatus.onScene}</div>
                  <div className="text-gray-400 text-xs">Local</div>
                </div>
                <div className="text-center px-2 border-l border-gray-600">
                  <div className="text-white font-semibold">{typeStatus.total}</div>
                  <div className="text-gray-400 text-xs">Total</div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* By Station */}
      <div className="px-4 pb-4 flex-1">
        <h3 className="text-sm font-semibold text-gray-300 mb-3 uppercase tracking-wide">
          Por Estação
        </h3>
        <div className="space-y-2 max-h-64 overflow-y-auto">
          {status.byStation.map((station) => (
            <div
              key={station.stationId}
              className="p-3 bg-[#16213e] rounded-lg border border-gray-700 cursor-pointer hover:bg-[#1a2744] transition-colors"
              onClick={() => setSelectedStation(selectedStation?.stationId === station.stationId ? null : station)}
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <span className="font-medium text-white">{station.stationName}</span>
                </div>
                <div className="flex items-center gap-3 text-sm">
                  <span className="text-green-400 font-semibold">{station.availableCount}</span>
                  <span className="text-gray-400">/</span>
                  <span className="text-blue-400 font-semibold">{station.deployedCount}</span>
                  <span className="text-gray-400">/</span>
                  <span className="text-white">{station.totalResources}</span>
                </div>
              </div>

              {/* Expandable details */}
              {selectedStation?.stationId === station.stationId && (
                <div className="mt-3 pt-3 border-t border-gray-700">
                  <div className="grid grid-cols-2 gap-2 text-sm">
                    <div>
                      <span className="text-gray-400">Latitude:</span>
                      <span className="text-white ml-1">{station.latitude.toFixed(4)}</span>
                    </div>
                    <div>
                      <span className="text-gray-400">Longitude:</span>
                      <span className="text-white ml-1">{station.longitude.toFixed(4)}</span>
                    </div>
                    <div>
                      <span className="text-gray-400">Disponíveis:</span>
                      <span className="text-green-400 ml-1">{station.availableCount}</span>
                    </div>
                    <div>
                      <span className="text-gray-400">Despiegados:</span>
                      <span className="text-blue-400 ml-1">{station.deployedCount}</span>
                    </div>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}