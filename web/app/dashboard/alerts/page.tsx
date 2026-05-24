"use client";

import { useState } from "react";
import Link from "next/link";
import { useAlerts } from "@/lib/hooks/useAlerts";
import { AlertList } from "@/components/alerts/AlertList";
import type { AlertSeverity } from "@/lib/types/alert";
import { Settings } from "lucide-react";

export default function AlertsPage() {
  const [severityFilter, setSeverityFilter] = useState<AlertSeverity | "">("");
  const [showUnreadOnly, setShowUnreadOnly] = useState(false);
  const { data, loading, markRead, refresh } = useAlerts({
    severity: severityFilter || undefined,
    isRead: showUnreadOnly ? false : undefined,
  });

  const alerts = data?.items ?? [];

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-white">Alertas</h2>
          <p className="text-sm text-slate-400 mt-1">Gerir alertas e notificações</p>
        </div>
        <Link
          href="/dashboard/alerts/rules"
          className="inline-flex items-center gap-2 px-4 py-2 bg-slate-700 hover:bg-slate-600 text-white text-sm rounded-lg transition-colors"
        >
          <Settings className="w-4 h-4" />
          Regras
        </Link>
      </div>

      <div className="flex items-center gap-4">
        <select
          value={severityFilter}
          onChange={(e) => setSeverityFilter(e.target.value as AlertSeverity)}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-emerald-500"
        >
          <option value="">Todas severidades</option>
          <option value="Info">Info</option>
          <option value="Warning">Warning</option>
          <option value="Danger">Danger</option>
          <option value="Critical">Critical</option>
        </select>

        <label className="flex items-center gap-2 text-sm text-slate-400 cursor-pointer">
          <input
            type="checkbox"
            checked={showUnreadOnly}
            onChange={(e) => setShowUnreadOnly(e.target.checked)}
            className="rounded bg-slate-800 border-slate-700 text-emerald-500 focus:ring-emerald-500"
          />
          Não lidas
        </label>
      </div>

      <AlertList
        alerts={alerts}
        loading={loading}
        onMarkRead={(id) => markRead({ alertIds: [id] })}
        onMarkAllRead={() => markRead({ markAllRead: true })}
        onRefresh={refresh}
        hasMore={data ? data.page * data.pageSize < data.totalCount : false}
        onLoadMore={() => {}}
      />
    </div>
  );
}