"use client";

import { useState } from "react";
import { RefreshCw, Database, Loader2 } from "lucide-react";
import { formatDistanceToNow, format } from "date-fns";
import { pt } from "date-fns/locale";
import { syncJobs } from "@/lib/api/health";
import type { JobStatusResponse } from "@/lib/types/health";

interface SystemHealthWidgetProps {
  jobs: JobStatusResponse[];
  onRefresh?: () => void;
}

function getLatestSync(jobs: JobStatusResponse[]): Date | null {
  if (!jobs.length) return null;
  let latestTime = 0;

  for (const job of jobs) {
    if (job.lastRun) {
      const time = new Date(job.lastRun).getTime();
      if (time > latestTime) {
        latestTime = time;
      }
    }
  }

  return latestTime > 0 ? new Date(latestTime) : null;
}

export function SystemHealthWidget({ jobs, onRefresh }: SystemHealthWidgetProps) {
  const [syncing, setSyncing] = useState(false);
  const lastSync = getLatestSync(jobs);
  const lastRefresh = lastSync
    ? formatDistanceToNow(lastSync, { addSuffix: true, locale: pt })
    : "Nunca";

  const triggerSync = async () => {
    setSyncing(true);
    try {
      await syncJobs();
      onRefresh?.();
    } catch (err) {
      console.error("Sync failed:", err);
    } finally {
      setSyncing(false);
    }
  };

  return (
    <div className="bg-gradient-to-r from-slate-800/80 to-slate-800/40 border border-slate-700/50 rounded-xl p-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <Database className="w-4 h-4 text-emerald-400" />
            <span className="text-xs text-slate-400">Última sincronização</span>
          </div>
          <div className="flex items-center gap-2">
            <RefreshCw className="w-4 h-4 text-slate-500" />
            <span className="text-sm font-medium text-white">
              {lastRefresh}
            </span>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={triggerSync}
            disabled={syncing}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-emerald-600/20 hover:bg-emerald-600/30 disabled:opacity-50 text-emerald-400 text-xs rounded-lg transition-colors"
          >
            {syncing ? (
              <Loader2 className="w-3 h-3 animate-spin" />
            ) : (
              <RefreshCw className="w-3 h-3" />
            )}
            {syncing ? "Sincronizando..." : "Sincronizar"}
          </button>
          <span className="text-xs text-slate-500">
            {format(new Date(), "dd MMM yyyy, HH:mm", { locale: pt })}
          </span>
        </div>
      </div>
    </div>
  );
}
