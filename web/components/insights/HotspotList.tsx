"use client";

import { useState } from "react";
import type { FireHotspot } from "@/lib/types/hotspot";
import { HotspotCard } from "./HotspotCard";

interface HotspotListProps {
  hotspots: FireHotspot[];
  onHotspotSelect?: (hotspot: FireHotspot) => void;
  selectedId?: string;
}

const RISK_ORDER = ["Critical", "High", "Medium", "Low"];

export function HotspotList({ hotspots, onHotspotSelect, selectedId }: HotspotListProps) {
  const [filter, setFilter] = useState<string>("all");

  const filteredHotspots = filter === "all"
    ? hotspots
    : hotspots.filter((h) => h.riskLevel === filter);

  const sortedHotspots = [...filteredHotspots].sort((a, b) => {
    const riskDiff = RISK_ORDER.indexOf(a.riskLevel) - RISK_ORDER.indexOf(b.riskLevel);
    if (riskDiff !== 0) return riskDiff;
    return b.fireCount - a.fireCount;
  });

  if (hotspots.length === 0) {
    return (
      <div className="bg-slate-800/60 rounded-lg p-6 text-center">
        <p className="text-slate-400 text-sm">Nenhum hotspot identificado ainda.</p>
        <p className="text-slate-500 text-xs mt-1">
          Os hotspots são identificados após análise de 2+ anos de dados históricos.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Filter buttons */}
      <div className="flex gap-2 flex-wrap">
        {["all", "Critical", "High", "Medium", "Low"].map((level) => (
          <button
            key={level}
            onClick={() => setFilter(level)}
            className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
              filter === level
                ? "bg-blue-500 text-white"
                : "bg-slate-700 text-slate-300 hover:bg-slate-600"
            }`}
          >
            {level === "all" ? "Todos" : level === "Critical" ? "Crítico" :
             level === "High" ? "Alto" : level === "Medium" ? "Médio" : "Baixo"}
          </button>
        ))}
      </div>

      {/* Stats summary */}
      <div className="grid grid-cols-4 gap-2 text-center text-xs">
        {["Critical", "High", "Medium", "Low"].map((level) => {
          const count = hotspots.filter((h) => h.riskLevel === level).length;
          const colors: Record<string, string> = {
            Critical: "text-red-400",
            High: "text-orange-400",
            Medium: "text-yellow-400",
            Low: "text-green-400",
          };
          return (
            <div key={level} className={`bg-slate-800/40 rounded p-2 ${colors[level]}`}>
              <div className="font-bold text-lg">{count}</div>
              <div className="opacity-70">{level === "Critical" ? "Crítico" : level === "High" ? "Alto" : level === "Medium" ? "Médio" : "Baixo"}</div>
            </div>
          );
        })}
      </div>

      {/* Hotspot list */}
      <div className="space-y-2 max-h-[400px] overflow-y-auto">
        {sortedHotspots.map((hotspot) => (
          <div
            key={hotspot.id}
            onClick={() => onHotspotSelect?.(hotspot)}
            className={`cursor-pointer transition-all ${
              selectedId === hotspot.id ? "ring-2 ring-blue-500" : ""
            }`}
          >
            <HotspotCard hotspot={hotspot} compact />
          </div>
        ))}
      </div>
    </div>
  );
}