"use client";

import { Bell, AlertTriangle, AlertCircle, Info, XCircle } from "lucide-react";
import type { Alert } from "@/lib/types/alert";
import { format } from "date-fns";
import { pt } from "date-fns/locale";

const SEVERITY_CONFIG = {
  Info: { icon: Info, color: "text-blue-400", bg: "bg-blue-500/10", border: "border-blue-500/30" },
  Warning: { icon: AlertTriangle, color: "text-yellow-400", bg: "bg-yellow-500/10", border: "border-yellow-500/30" },
  Danger: { icon: AlertCircle, color: "text-orange-400", bg: "bg-orange-500/10", border: "border-orange-500/30" },
  Critical: { icon: XCircle, color: "text-red-400", bg: "bg-red-500/10", border: "border-red-500/30" },
} as const;

interface AlertCardProps {
  alert: Alert;
  onMarkRead?: (id: string) => void;
}

export function AlertCard({ alert, onMarkRead }: AlertCardProps) {
  const config = SEVERITY_CONFIG[alert.severity] ?? SEVERITY_CONFIG.Info;
  const Icon = config.icon;

  return (
    <div
      className={`${config.bg} border ${config.border} rounded-xl p-4 transition-all ${
        alert.isRead ? "opacity-60" : ""
      }`}
    >
      <div className="flex items-start gap-3">
        <Icon className={`w-5 h-5 ${config.color} shrink-0 mt-0.5`} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between gap-2">
            <h3 className="text-sm font-semibold text-white truncate">{alert.title}</h3>
            {!alert.isRead && (
              <span className={`w-2 h-2 rounded-full ${config.color} shrink-0`} />
            )}
          </div>
          <p className="text-xs text-slate-400 mt-1 line-clamp-2">{alert.message}</p>
          <div className="flex items-center justify-between mt-3">
            <span className="text-xs text-slate-500">
              {format(new Date(alert.createdAt), "d MMM HH:mm", { locale: pt })}
            </span>
            {!alert.isRead && onMarkRead && (
              <button
                onClick={() => onMarkRead(alert.id)}
                className="text-xs text-slate-400 hover:text-white transition-colors"
              >
                Marcar lida
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}