"use client";

import type { EventDispatch, OptimalDispatch } from "@/lib/types/resource";
import type { RiskLevel } from "@/lib/types/event";

interface ResourceAllocationPanelProps {
  eventId: string;
  eventTitle: string;
  eventSeverity: RiskLevel;
  dispatches: EventDispatch[];
  optimalDispatch: OptimalDispatch | null;
  onDispatchResource?: (resourceId: string) => void;
}

const STATUS_COLORS: Record<string, { bg: string; text: string; border: string }> = {
  EnRoute: { bg: "bg-blue-500/20", text: "text-blue-400", border: "border-blue-500" },
  OnScene: { bg: "bg-green-500/20", text: "text-green-400", border: "border-green-500" },
  Dispatched: { bg: "bg-yellow-500/20", text: "text-yellow-400", border: "border-yellow-500" },
  Returning: { bg: "bg-purple-500/20", text: "text-purple-400", border: "border-purple-500" },
  Completed: { bg: "bg-gray-500/20", text: "text-gray-400", border: "border-gray-500" },
};

const RESOURCE_TYPE_ICONS: Record<string, string> = {
  Tanker: "🚒",
  Pump: "🚑",
  Helicopter: "🚁",
  Team: "👥",
  CommandUnit: "📡",
};

export function ResourceAllocationPanel({
  eventId,
  eventTitle,
  eventSeverity,
  dispatches,
  optimalDispatch,
  onDispatchResource,
}: ResourceAllocationPanelProps) {
  const severityColors: Record<RiskLevel, string> = {
    Low: "text-green-400 bg-green-500/20 border-green-500",
    Medium: "text-yellow-400 bg-yellow-500/20 border-yellow-500",
    High: "text-orange-400 bg-orange-500/20 border-orange-500",
    Critical: "text-red-400 bg-red-500/20 border-red-500",
  };

  return (
    <div className="h-full flex flex-col bg-[#1a1a2e] text-white p-4 overflow-y-auto">
      {/* Event header */}
      <div className="mb-6">
        <div className="flex items-center gap-3 mb-2">
          <h2 className="text-xl font-bold text-white">{eventTitle}</h2>
          <span
            className={`px-2 py-0.5 rounded text-xs font-semibold border ${severityColors[eventSeverity]}`}
          >
            {eventSeverity}
          </span>
        </div>
        <p className="text-sm text-gray-400">ID: {eventId}</p>
      </div>

      {/* Incoming resources */}
      <div className="mb-6">
        <h3 className="text-sm font-semibold text-gray-300 mb-3 uppercase tracking-wide">
          Recursos a Caminho ({dispatches.length})
        </h3>

        {dispatches.length === 0 ? (
          <div className="p-4 bg-[#16213e] rounded-lg border border-gray-700 text-center">
            <p className="text-gray-400 text-sm">Nenhum recurso em trânsito</p>
          </div>
        ) : (
          <div className="space-y-3">
            {dispatches.map((dispatch) => {
              const statusStyle = STATUS_COLORS[dispatch.status] || STATUS_COLORS.Dispatched;
              const remainingMinutes = Math.max(
                0,
                Math.round(
                  new Date(dispatch.dispatchedAt).getTime() +
                    dispatch.travelTimeMinutes * 60 * 1000 -
                    Date.now()
                ) / 60000
              );

              return (
                <div
                  key={dispatch.dispatchId}
                  className={`p-4 rounded-lg border ${statusStyle.bg} ${statusStyle.border}`}
                >
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center gap-2">
                      <span className="text-lg">{RESOURCE_TYPE_ICONS[dispatch.resourceType] || "🚔"}</span>
                      <span className="font-semibold text-white">{dispatch.resourceName}</span>
                    </div>
                    <span
                      className={`text-xs font-medium px-2 py-0.5 rounded ${statusStyle.bg} ${statusStyle.text}`}
                    >
                      {dispatch.status}
                    </span>
                  </div>

                  <div className="text-sm text-gray-300 space-y-1">
                    <div className="flex justify-between">
                      <span>Estação:</span>
                      <span className="text-white">{dispatch.stationName}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Distância:</span>
                      <span className="text-white">{dispatch.distanceKm.toFixed(1)} km</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Tempo de viagem:</span>
                      <span className="text-white">{Math.round(dispatch.travelTimeMinutes)} min</span>
                    </div>
                    {dispatch.status === "EnRoute" && remainingMinutes > 0 && (
                      <div className="flex justify-between">
                        <span>ETA:</span>
                        <span className="text-white font-semibold">~{remainingMinutes} min</span>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Optimal dispatch recommendations */}
      {optimalDispatch && optimalDispatch.options.length > 0 && (
        <div className="mb-6">
          <h3 className="text-sm font-semibold text-gray-300 mb-3 uppercase tracking-wide">
            Recomendações de Dispatch
          </h3>

          <div className="space-y-3">
            {optimalDispatch.options.slice(0, 3).map((option, index) => (
              <div
                key={option.resourceId}
                className="p-4 bg-[#16213e] rounded-lg border border-gray-700"
              >
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <span className="w-6 h-6 rounded-full bg-blue-500 flex items-center justify-center text-xs font-bold">
                      {index + 1}
                    </span>
                    <span className="text-lg">{RESOURCE_TYPE_ICONS[option.resourceType] || "🚔"}</span>
                    <span className="font-semibold text-white">{option.name}</span>
                  </div>
                  <span className="text-xs text-gray-400">
                    Score: {option.priorityScore.toFixed(2)}
                  </span>
                </div>

                <div className="text-sm text-gray-300 space-y-1 mb-3">
                  <div className="flex justify-between">
                    <span>Estação:</span>
                    <span className="text-white">{option.stationName}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Distância:</span>
                    <span className="text-white">{option.distanceKm.toFixed(1)} km</span>
                  </div>
                  <div className="flex justify-between">
                    <span>ETA:</span>
                    <span className="text-white">{Math.round(option.travelTimeMinutes)} min</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Personal:</span>
                    <span className="text-white">{option.personnelCount}</span>
                  </div>
                </div>

                {onDispatchResource && (
                  <button
                    onClick={() => onDispatchResource(option.resourceId)}
                    className="w-full py-2 px-4 bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold rounded transition-colors"
                  >
                    Despachar Este Recurso
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* No recommendations */}
      {(!optimalDispatch || optimalDispatch.options.length === 0) && (
        <div className="p-4 bg-[#16213e] rounded-lg border border-gray-700 text-center">
          <p className="text-gray-400 text-sm">
            Sem recomendações disponíveis. Nenhum recurso encontrado nas proximidades.
          </p>
        </div>
      )}
    </div>
  );
}