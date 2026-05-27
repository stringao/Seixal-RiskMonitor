"use client";

import { useState, useCallback } from "react";
import { useEvents } from "@/lib/hooks/useEvents";
import { useRiskDashboard } from "@/lib/hooks/useRiskDashboard";
import { useAlerts } from "@/lib/hooks/useAlerts";
import { useJobStatuses } from "@/lib/hooks/useJobStatuses";
import { useAirQuality, usePollen } from "@/lib/hooks/useAirQuality";
import { StatsCards } from "@/components/dashboard/StatsCards";
import { RecentEvents } from "@/components/dashboard/RecentEvents";
import { EventTypeChart } from "@/components/dashboard/EventTypeChart";
import { EventsTimeChart } from "@/components/dashboard/EventsTimeChart";
import { SeverityDistribution } from "@/components/dashboard/SeverityDistribution";
import { ActiveAlertsPanel } from "@/components/dashboard/ActiveAlertsPanel";
import { SystemHealthWidget } from "@/components/dashboard/SystemHealthWidget";
import { AirQualityPanel } from "@/components/environment/AirQualityPanel";

export default function DashboardPage() {
  const [refreshKey, setRefreshKey] = useState(0);
  const { data: eventsData, loading: eventsLoading, error } = useEvents({ pageSize: 200 }, refreshKey);
  const { data: riskData, loading: riskLoading } = useRiskDashboard(refreshKey);
  const { data: alertData, loading: alertsLoading } = useAlerts();
  const { data: jobsData } = useJobStatuses(refreshKey);

  // Use Setúbal area coordinates (38.5, -8.9) as default per requirements
  const { data: airQualityData, loading: airQualityLoading } = useAirQuality(38.5, -8.9);
  const { data: pollenData, loading: pollenLoading } = usePollen(38.5, -8.9);

  const handleRefresh = useCallback(() => {
    setRefreshKey(k => k + 1);
  }, []);

  const loading = eventsLoading || alertsLoading || riskLoading || airQualityLoading || pollenLoading;

  if (loading) {
    return (
      <div className="p-6 space-y-6">
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {[...Array(8)].map((_, i) => (
            <div key={i} className="h-24 bg-slate-800/60 rounded-xl animate-pulse" />
          ))}
        </div>
        <div className="grid grid-cols-1 xl:grid-cols-5 gap-6">
          <div className="xl:col-span-3 h-72 bg-slate-800/60 rounded-xl animate-pulse" />
          <div className="xl:col-span-2 space-y-6">
            <div className="h-52 bg-slate-800/60 rounded-xl animate-pulse" />
            <div className="h-60 bg-slate-800/60 rounded-xl animate-pulse" />
          </div>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="h-64 bg-slate-800/60 rounded-xl animate-pulse" />
          <div className="h-64 bg-slate-800/60 rounded-xl animate-pulse" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-64 text-red-400">
        Erro ao carregar dados: {error}
      </div>
    );
  }

  const events = eventsData?.items ?? [];
  const alerts = alertData?.items ?? [];
  const unreadCount = alerts.filter((a) => !a.isRead).length;

  return (
    <div className="p-6 space-y-6">
      <StatsCards events={events} riskData={riskData} alertData={alertData} />

      <div className="grid grid-cols-1 xl:grid-cols-5 gap-6">
        <div className="xl:col-span-3">
          <EventsTimeChart events={events} />
        </div>
        <div className="xl:col-span-2 space-y-6">
          <SeverityDistribution events={events} />
          <EventTypeChart events={events} />
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <RecentEvents events={events} />
        <ActiveAlertsPanel alerts={alerts} unreadCount={unreadCount} />
      </div>

      {/* Air Quality & Pollen Panel */}
      <AirQualityPanel
        airQualityData={airQualityData}
        pollenData={pollenData}
        loading={airQualityLoading || pollenLoading}
      />

      {jobsData.length > 0 && <SystemHealthWidget jobs={jobsData} onRefresh={handleRefresh} />}
    </div>
  );
}
