"use client";

import { useMemo } from "react";
import {
  AlertTriangle,
  Clock,
  ShieldAlert,
  Flame,
  Droplets,
  CloudLightning,
} from "lucide-react";
import type { GeoEvent } from "@/lib/types/event";
import type { AlertListResponse } from "@/lib/types/alert";
import type { RiskDashboardResponse } from "@/lib/types/risk";
import { EVENT_TYPE_LABELS, SEVERITY_COLORS } from "@/lib/map/config";

interface StatsCardsProps {
  events: GeoEvent[];
  riskData: RiskDashboardResponse | null;
  alertData: AlertListResponse | null;
}

const TYPE_ICONS: Record<string, React.ReactNode> = {
  Fire: <Flame className="w-5 h-5 text-orange-400" />,
  Flood: <Droplets className="w-5 h-5 text-blue-400" />,
  Storm: <CloudLightning className="w-5 h-5 text-yellow-400" />,
};

export function StatsCards({ events, riskData, alertData }: StatsCardsProps) {
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

    const unreadAlerts = alertData?.items.filter((a) => !a.isRead).length ?? 0;

    return { total: events.length, byType, bySeverity, last24h, unreadAlerts };
  }, [events, alertData]);

  const topType = Object.entries(stats.byType).sort((a, b) => b[1] - a[1])[0];
  const criticalCount = stats.bySeverity["Critical"] ?? 0;
  const riskZones = riskData?.totalZones ?? 0;
  const criticalZones = riskData?.criticalZones ?? 0;
  const activeAlerts = alertData?.totalCount ?? 0;

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <StatCard
        label="Total Eventos"
        value={stats.total}
        icon={<AlertTriangle className="w-5 h-5 text-emerald-400" />}
        accentColor="#22c55e"
      />
      <StatCard
        label="Últimas 24h"
        value={stats.last24h}
        icon={<Clock className="w-5 h-5 text-yellow-400" />}
        accentColor="#eab308"
      />
      <StatCard
        label="Críticos"
        value={criticalCount}
        icon={<ShieldAlert className="w-5 h-5" style={{ color: SEVERITY_COLORS.Critical }} />}
        accentColor={SEVERITY_COLORS.Critical}
      />
      <StatCard
        label="Zonas Risco"
        value={riskZones}
        icon={<AlertTriangle className="w-5 h-5 text-orange-400" />}
        accentColor="#f97316"
      />
    </div>
  );
}

function StatCard({
  label,
  value,
  icon,
  accentColor,
}: {
  label: string;
  value: React.ReactNode;
  icon: React.ReactNode;
  accentColor: string;
}) {
  return (
    <div
      className="relative overflow-hidden bg-gradient-to-br from-slate-800/90 to-slate-800/50 border border-slate-700/80 rounded-2xl p-4 group hover:border-slate-600/80 transition-all duration-300"
    >
      {/* Glow accent */}
      <div
        className="absolute top-0 left-0 w-16 h-16 rounded-full blur-2xl opacity-15"
        style={{ backgroundColor: accentColor }}
      />

      <div className="relative z-10 flex items-start justify-between">
        <div className="p-2 bg-slate-900/60 rounded-xl border border-slate-700/50">
          {icon}
        </div>
      </div>

      <div className="relative z-10 mt-3">
        <div className="text-2xl font-bold text-white tracking-tight">{value}</div>
        <div className="text-xs text-slate-400 mt-0.5">{label}</div>
      </div>

      <div
        className="absolute bottom-0 left-0 right-0 h-0.5 opacity-40"
        style={{ background: `linear-gradient(90deg, ${accentColor}, transparent)` }}
      />
    </div>
  );
}
