"use client";

import { TrendingUp, TrendingDown, Minus, LucideIcon } from "lucide-react";

const TREND_CONFIG: Record<string, { icon: LucideIcon; color: string; bgColor: string; label: string }> = {
  increasing: {
    icon: TrendingUp,
    color: "text-red-400",
    bgColor: "bg-red-500/20",
    label: "Aumento",
  },
  decreasing: {
    icon: TrendingDown,
    color: "text-emerald-400",
    bgColor: "bg-emerald-500/20",
    label: "Diminuição",
  },
  stable: {
    icon: Minus,
    color: "text-slate-400",
    bgColor: "bg-slate-500/20",
    label: "Estável",
  },
};

interface RiskTrendBadgeProps {
  trend: "increasing" | "decreasing" | "stable";
  showLabel?: boolean;
  size?: "sm" | "md" | "lg";
  percentChange?: number;
}

export function RiskTrendBadge({
  trend,
  showLabel = true,
  size = "md",
  percentChange,
}: RiskTrendBadgeProps) {
  const config = TREND_CONFIG[trend] ?? TREND_CONFIG.stable;
  const Icon = config.icon;

  const sizeClasses = {
    sm: "text-xs gap-1",
    md: "text-sm gap-1.5",
    lg: "text-base gap-2",
  };

  const iconSizes = {
    sm: "w-3 h-3",
    md: "w-4 h-4",
    lg: "w-5 h-5",
  };

  return (
    <div
      className={`inline-flex items-center ${sizeClasses[size]} ${config.bgColor} px-2 py-1 rounded-full`}
    >
      <Icon className={`${iconSizes[size]} ${config.color}`} />
      {showLabel && (
        <span className={`font-medium ${config.color}`}>
          {config.label}
          {percentChange !== undefined && (
            <span className="ml-1 opacity-75">
              ({percentChange > 0 ? "+" : ""}{percentChange.toFixed(1)}%)
            </span>
          )}
        </span>
      )}
    </div>
  );
}

interface RiskTrendInlineProps {
  trend: "increasing" | "decreasing" | "stable";
  label: string;
  currentValue: string | number;
  previousValue?: string | number;
}

export function RiskTrendInline({ trend, label, currentValue, previousValue }: RiskTrendInlineProps) {
  const config = TREND_CONFIG[trend] ?? TREND_CONFIG.stable;
  const Icon = config.icon;

  return (
    <div className="flex items-center justify-between py-1">
      <div className="flex items-center gap-2">
        <Icon className={`w-4 h-4 ${config.color}`} />
        <span className="text-sm text-slate-400">{label}</span>
      </div>
      <div className="flex items-center gap-2">
        <span className="text-sm font-medium text-white">{currentValue}</span>
        {previousValue !== undefined && (
          <span className="text-xs text-slate-500">
            (antes: {previousValue})
          </span>
        )}
      </div>
    </div>
  );
}
