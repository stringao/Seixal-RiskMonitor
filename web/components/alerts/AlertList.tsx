"use client";

import { RefreshCw } from "lucide-react";
import { AlertCard } from "./AlertCard";
import { Pagination } from "@/components/ui/Pagination";
import type { Alert } from "@/lib/types/alert";

interface AlertListProps {
  alerts: Alert[];
  loading?: boolean;
  page: number;
  pageSize: number;
  totalCount: number;
  onMarkRead?: (id: string) => void;
  onMarkAllRead?: () => void;
  onRefresh?: () => void;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
}

export function AlertList({
  alerts,
  loading,
  page,
  pageSize,
  totalCount,
  onMarkRead,
  onMarkAllRead,
  onRefresh,
  onPageChange,
  onPageSizeChange,
}: AlertListProps) {
  if (loading && alerts.length === 0) {
    return (
      <div className="flex items-center justify-center py-12">
        <RefreshCw className="w-6 h-6 text-slate-500 animate-spin" />
      </div>
    );
  }

  if (alerts.length === 0) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-8 text-center">
        <p className="text-slate-500">Nenhum alerta encontrado</p>
      </div>
    );
  }

  const unreadCount = alerts.filter((a) => !a.isRead).length;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="text-sm text-slate-400">
          {unreadCount > 0 ? `${unreadCount} não lidas` : "Todas lidas"}
        </div>
        <div className="flex items-center gap-2">
          {onRefresh && (
            <button
              onClick={onRefresh}
              className="p-2 text-slate-400 hover:text-white transition-colors"
              title="Atualizar"
            >
              <RefreshCw className="w-4 h-4" />
            </button>
          )}
          {onMarkAllRead && unreadCount > 0 && (
            <button
              onClick={onMarkAllRead}
              className="text-xs text-slate-400 hover:text-white transition-colors"
            >
              Marcar todas lidas
            </button>
          )}
        </div>
      </div>

      <div className="space-y-3">
        {alerts.map((alert) => (
          <AlertCard key={alert.id} alert={alert} onMarkRead={onMarkRead} />
        ))}
      </div>

      <Pagination
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
      />
    </div>
  );
}