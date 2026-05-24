"use client";

import { useMemo } from "react";
import type { GeoEvent } from "@/lib/types/event";
import { EVENT_TYPE_LABELS, SEVERITY_COLORS } from "@/lib/map/config";
import { AlertTriangle, Flame, Droplets, CloudLightning } from "lucide-react";

interface StatsCardsProps {
  events: GeoEvent[];
}

const TYPE_ICONS: Record<string, React.ReactNode> = {
  Fire: <Flame className="w-5 h-5 text-orange-400" />,
  Flood: <Droplets className="w-5 h-5 text-blue-400" />,
  Storm: <CloudLightning className="w-5 h-5 text-yellow-400" />,
};

export function StatsCards({ events }: StatsCardsProps) {
  const stats = useMemo(() => {
    const byType: Record<string, number> = {};
    const bySeverity: Record<string, number> = {};
    let last24h = 0;
    const now = Date.now();

    for (const e of events) {
      byType[e.eventType] = (byType[e.eventType] ?? 0) + 1;
      bySeverity[e.severity] = (bySeverity[e.severity] ?? 0) + 1;
      if (now - new Date(e.occurredAt).getTime() < 86_400_000) last24h++;
    }

    return { total: events.length, byType, bySeverity, last24h };
  }, [events]);

  const topType = Object.entries(stats.byType).sort((a, b) => b[1] - a[1])[0];
  const criticalCount = stats.bySeverity["Critical"] ?? 0;

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <StatCard
        label="Total Eventos"
        value={stats.total}
        icon={<AlertTriangle className="w-5 h-5 text-emerald-400" />}
      />
      <StatCard
        label="Últimas 24h"
        value={stats.last24h}
        icon={<AlertTriangle className="w-5 h-5 text-yellow-400" />}
      />
      <StatCard
        label="Tipo Mais Comum"
        value={topType ? EVENT_TYPE_LABELS[topType[0]] ?? topType[0] : "—"}
        icon={TYPE_ICONS[topType?.[0] ?? ""] ?? <AlertTriangle className="w-5 h-5 text-slate-400" />}
      />
      <StatCard
        label="Críticos"
        value={criticalCount}
        icon={<AlertTriangle className="w-5 h-5" style={{ color: SEVERITY_COLORS.Critical }} />}
      />
    </div>
  );
}

function StatCard({ label, value, icon }: { label: string; value: React.ReactNode; icon: React.ReactNode }) {
  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4 flex items-center gap-4">
      <div className="p-2 bg-slate-700/50 rounded-lg">{icon}</div>
      <div>
        <div className="text-2xl font-bold text-white">{value}</div>
        <div className="text-xs text-slate-400">{label}</div>
      </div>
    </div>
  );
}