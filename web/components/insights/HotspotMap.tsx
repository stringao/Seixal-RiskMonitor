"use client";

import { useState, useEffect } from "react";
import type { FireHotspot, HotspotDetail } from "@/lib/types/hotspot";
import { fetchHotspotById } from "@/lib/api/hotspots";
import { HotspotCard } from "./HotspotCard";

interface HotspotMapProps {
  hotspots: FireHotspot[];
  selectedHotspot?: FireHotspot;
  onHotspotSelect?: (hotspot: FireHotspot) => void;
}

export function HotspotMap({ hotspots, selectedHotspot, onHotspotSelect }: HotspotMapProps) {
  const [detail, setDetail] = useState<HotspotDetail | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (selectedHotspot) {
      setLoading(true);
      fetchHotspotById(selectedHotspot.id)
        .then(setDetail)
        .finally(() => setLoading(false));
    } else {
      setDetail(null);
    }
  }, [selectedHotspot]);

  return (
    <div className="space-y-4">
      {/* Mini map placeholder - in production, this would integrate with Leaflet */}
      <div className="bg-slate-800/60 rounded-lg p-4">
        <h3 className="text-sm font-medium text-white mb-3">Mapa de Densidade</h3>
        <div className="bg-slate-900 rounded-lg h-48 flex items-center justify-center">
          {hotspots.length > 0 ? (
            <div className="text-center text-slate-400 text-sm">
              <p className="text-lg mb-1">🗺️</p>
              <p>{hotspots.length} hotspots carregados</p>
              <p className="text-xs mt-1">Selecione um hotspot para ver detalhes</p>
            </div>
          ) : (
            <div className="text-center text-slate-500 text-sm">
              <p>Nenhum hotspot disponível</p>
            </div>
          )}
        </div>

        {/* Legend */}
        <div className="mt-3 flex items-center gap-4 justify-center text-xs">
          {["Critical", "High", "Medium", "Low"].map((level) => {
            const colors: Record<string, string> = {
              Critical: "#ef4444",
              High: "#f97316",
              Medium: "#eab308",
              Low: "#22c55e",
            };
            return (
              <div key={level} className="flex items-center gap-1.5">
                <span
                  className="w-2 h-2 rounded-full"
                  style={{ backgroundColor: colors[level] }}
                />
                <span className="text-slate-400">
                  {level === "Critical" ? "Crítico" :
                   level === "High" ? "Alto" :
                   level === "Medium" ? "Médio" : "Baixo"}
                </span>
              </div>
            );
          })}
        </div>
      </div>

      {/* Selected hotspot detail */}
      {selectedHotspot && (
        <div className="bg-slate-800/60 rounded-lg p-4">
          <h3 className="text-sm font-medium text-white mb-3">Detalhes do Hotspot</h3>
          {loading ? (
            <div className="animate-pulse space-y-2">
              <div className="h-20 bg-slate-700 rounded"></div>
            </div>
          ) : detail ? (
            <div className="space-y-4">
              <HotspotCard hotspot={detail} />

              {/* Seasonal pattern chart */}
              {detail.seasonalPattern && (
                <div className="bg-slate-900/50 rounded-lg p-4">
                  <h4 className="text-xs font-medium text-slate-500 uppercase tracking-wider mb-3">
                    Padrão Sazonal (Incêndios por Mês)
                  </h4>
                  <div className="flex items-end gap-1 h-24">
                    {Array.from({ length: 12 }, (_, i) => {
                      const month = i + 1;
                      const firesByMonth = detail.seasonalPattern as Record<number, number>;
                      const count = firesByMonth[month] ?? 0;
                      const maxCount = Math.max(...Object.values(firesByMonth), 1);
                      const height = (count / maxCount) * 100;

                      return (
                        <div key={month} className="flex-1 flex flex-col items-center gap-1">
                          <div
                            className="w-full bg-blue-500/60 rounded-t transition-all"
                            style={{ height: `${Math.max(4, height)}%` }}
                            title={`${count} incêndios`}
                          />
                          <span className="text-[10px] text-slate-500">
                            {["J", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D"][i]}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* Recent events */}
              {detail.recentEvents.length > 0 && (
                <div className="bg-slate-900/50 rounded-lg p-4">
                  <h4 className="text-xs font-medium text-slate-500 uppercase tracking-wider mb-3">
                    Eventos Recentes
                  </h4>
                  <div className="space-y-2 max-h-48 overflow-y-auto">
                    {detail.recentEvents.slice(0, 5).map((event) => (
                      <div key={event.id} className="flex items-center gap-2 text-xs">
                        <span className="w-2 h-2 rounded-full bg-orange-500"></span>
                        <span className="text-slate-300 flex-1 truncate">{event.title}</span>
                        <span className="text-slate-500">
                          {new Date(event.occurredAt).toLocaleDateString("pt-PT")}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          ) : (
            <p className="text-slate-400 text-sm">Erro ao carregar detalhes.</p>
          )}
        </div>
      )}
    </div>
  );
}