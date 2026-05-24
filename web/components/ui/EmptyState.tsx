"use client";

import { AlertTriangle, FileX, SearchX, BellOff } from "lucide-react";

interface EmptyStateProps {
  title: string;
  description?: string;
  icon?: "events" | "alerts" | "search" | "notifications";
  action?: {
    label: string;
    onClick: () => void;
  };
}

const icons = {
  events: FileX,
  alerts: BellOff,
  search: SearchX,
  notifications: AlertTriangle,
};

export function EmptyState({ title, description, icon = "events", action }: EmptyStateProps) {
  const Icon = icons[icon];

  return (
    <div className="flex flex-col items-center justify-center py-16 px-6 text-center">
      <div className="w-16 h-16 rounded-full bg-slate-800/80 flex items-center justify-center mb-6">
        <Icon className="w-8 h-8 text-slate-500" />
      </div>
      <h3 className="text-lg font-semibold text-slate-200 mb-2">{title}</h3>
      {description && (
        <p className="text-sm text-slate-400 max-w-sm mb-6">{description}</p>
      )}
      {action && (
        <button
          onClick={action.onClick}
          className="px-4 py-2 bg-emerald-500/20 text-emerald-400 rounded-lg text-sm font-medium hover:bg-emerald-500/30 transition-colors"
        >
          {action.label}
        </button>
      )}
    </div>
  );
}