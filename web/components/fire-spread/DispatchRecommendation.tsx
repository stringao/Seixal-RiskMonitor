"use client";

import type { DispatchOption } from "@/lib/types/resource";
import type { RiskLevel } from "@/lib/types/event";

interface DispatchRecommendationProps {
  eventId: string;
  eventTitle: string;
  eventSeverity: RiskLevel;
  recommendedResources: DispatchOption[];
  onDispatch: (resourceId: string) => void;
  isLoading?: boolean;
}

export function DispatchRecommendation({
  eventId,
  eventTitle,
  eventSeverity,
  recommendedResources,
  onDispatch,
  isLoading = false,
}: DispatchRecommendationProps) {
  const severityColors: Record<RiskLevel, { bg: string; border: string; text: string }> = {
    Low: { bg: "bg-green-500/10", border: "border-green-500/50", text: "text-green-400" },
    Medium: { bg: "bg-yellow-500/10", border: "border-yellow-500/50", text: "text-yellow-400" },
    High: { bg: "bg-orange-500/10", border: "border-orange-500/50", text: "text-orange-400" },
    Critical: { bg: "bg-red-500/10", border: "border-red-500/50", text: "text-red-400" },
  };

  if (isLoading) {
    return (
      <div className="p-4 bg-[#16213e] rounded-lg border border-gray-700">
        <div className="animate-pulse space-y-3">
          <div className="h-4 bg-gray-700 rounded w-3/4"></div>
          <div className="h-8 bg-gray-700 rounded"></div>
          <div className="h-8 bg-gray-700 rounded"></div>
        </div>
      </div>
    );
  }

  if (recommendedResources.length === 0) {
    return (
      <div className="p-4 bg-[#16213e] rounded-lg border border-gray-700 text-center">
        <p className="text-gray-400 text-sm">
          Nenhum recurso recomendado disponível para este evento.
        </p>
      </div>
    );
  }

  const colors = severityColors[eventSeverity];

  return (
    <div className={`p-4 rounded-lg border ${colors.bg} ${colors.border}`}>
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-white">Recomendações de Dispatch</h3>
        <span className={`text-xs font-medium px-2 py-0.5 rounded ${colors.bg} ${colors.text}`}>
          {eventSeverity}
        </span>
      </div>

      <p className="text-sm text-gray-400 mb-4">
        Para o evento <span className="text-white font-medium">{eventTitle}</span>
      </p>

      <div className="space-y-3">
        {recommendedResources.map((resource, index) => (
          <div
            key={resource.resourceId}
            className="p-3 bg-[#1a1a2e] rounded border border-gray-700"
          >
            <div className="flex items-center gap-3 mb-2">
              <div
                className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${
                  index === 0
                    ? "bg-yellow-500 text-black"
                    : index === 1
                    ? "bg-gray-400 text-black"
                    : index === 2
                    ? "bg-amber-700 text-white"
                    : "bg-gray-600 text-white"
                }`}
              >
                {index + 1}
              </div>
              <div className="flex-1">
                <span className="font-semibold text-white">{resource.name}</span>
                <span className="text-gray-400 text-sm ml-2">({resource.resourceType})</span>
              </div>
              <span className="text-xs text-gray-500">
                Score: {resource.priorityScore.toFixed(2)}
              </span>
            </div>

            <div className="grid grid-cols-2 gap-2 text-sm mb-3">
              <div>
                <span className="text-gray-400">Estação:</span>
                <span className="text-white ml-1">{resource.stationName}</span>
              </div>
              <div>
                <span className="text-gray-400">Distância:</span>
                <span className="text-white ml-1">{resource.distanceKm.toFixed(1)} km</span>
              </div>
              <div>
                <span className="text-gray-400">ETA:</span>
                <span className="text-white ml-1">{Math.round(resource.travelTimeMinutes)} min</span>
              </div>
              <div>
                <span className="text-gray-400">Personal:</span>
                <span className="text-white ml-1">{resource.personnelCount}</span>
              </div>
            </div>

            <button
              onClick={() => onDispatch(resource.resourceId)}
              className="w-full py-2 px-3 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 disabled:cursor-not-allowed text-white text-sm font-semibold rounded transition-colors"
            >
              {index === 0 ? "Despachar Principal" : `Despachar Opção ${index + 1}`}
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}