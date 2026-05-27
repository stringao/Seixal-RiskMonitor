"use client";

import Link from "next/link";
import { formatDistanceToNow } from "date-fns";
import { pt } from "date-fns/locale";
import { CheckCircle2 } from "lucide-react";
import type { Alert } from "@/lib/types/alert";

const ALERT_SEVERITY_COLORS: Record<string, string> = {
  Info: "#3b82f6",
  Warning: "#eab308",
  Danger: "#f97316",
  Critical: "#ef4444",
};

interface ActiveAlertsPanelProps {
  alerts: Alert[];
  unreadCount: number;
}

export function ActiveAlertsPanel({ alerts, unreadCount }: ActiveAlertsPanelProps) {
  const unreadAlerts = alerts.filter((a) => !a.isRead).slice(0, 5);

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl">
      <div className="flex items-center justify-between px-4 py-3 border-b border-slate-700">
        <h3 className="text-sm font-semibold text-slate-200">Alertas Activos</h3>
        {unreadCount > 0 && (
          <span className="bg-red-500/20 text-red-400 rounded-full px-2 text-xs font-medium">
            {unreadCount}
          </span>
        )}
      </div>

      {unreadAlerts.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-8 gap-2">
          <CheckCircle2 className="w-8 h-8 text-emerald-400" />
          <span className="text-sm text-emerald-400">Sem alertas por ler</span>
        </div>
      ) : (
        <>
          <ul className="divide-y divide-slate-700/50">
            {unreadAlerts.map((alert) => (
              <li key={alert.id}>
                <div
                  className="flex items-start gap-3 px-4 py-3"
                  style={{ borderLeftWidth: 2, borderLeftColor: ALERT_SEVERITY_COLORS[alert.severity] ?? "#64748b" }}
                >
                  <div className="flex-1 min-w-0">
                    <div className="text-sm text-white truncate">{alert.title}</div>
                    <div className="text-xs text-slate-500">
                      {formatDistanceToNow(new Date(alert.createdAt), {
                        addSuffix: true,
                        locale: pt,
                      })}
                    </div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
          <div className="px-4 py-3 border-t border-slate-700">
            <Link
              href="/alerts"
              className="text-sm text-emerald-400 hover:text-emerald-300 transition-colors"
            >
              Ver todos os alertas
            </Link>
          </div>
        </>
      )}
    </div>
  );
}
