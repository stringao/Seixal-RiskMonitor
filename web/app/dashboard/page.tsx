"use client";

import { useEvents } from "@/lib/hooks/useEvents";
import { StatsCards } from "@/components/dashboard/StatsCards";
import { RecentEvents } from "@/components/dashboard/RecentEvents";
import { EventTypeChart } from "@/components/dashboard/EventTypeChart";

export default function DashboardPage() {
  const { data, loading, error } = useEvents({ pageSize: 100 });

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-24 bg-slate-800/60 rounded-xl animate-pulse" />
          ))}
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

  const events = data?.items ?? [];

  return (
    <div className="space-y-6">
      <StatsCards events={events} />
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <RecentEvents events={events} />
        <EventTypeChart events={events} />
      </div>
    </div>
  );
}
