"use client";

import { Lightbulb, AlertTriangle, Info, X } from "lucide-react";
import { useState } from "react";

const ALERT_CONFIG: Record<string, { icon: React.ReactNode; bgColor: string; borderColor: string; iconColor: string }> = {
  High: {
    icon: <AlertTriangle className="w-5 h-5" />,
    bgColor: "bg-red-950/50",
    borderColor: "border-red-500/50",
    iconColor: "text-red-400",
  },
  Critical: {
    icon: <AlertTriangle className="w-5 h-5" />,
    bgColor: "bg-red-950/70",
    borderColor: "border-red-500/70",
    iconColor: "text-red-400",
  },
  Medium: {
    icon: <Lightbulb className="w-5 h-5" />,
    bgColor: "bg-yellow-950/50",
    borderColor: "border-yellow-500/50",
    iconColor: "text-yellow-400",
  },
  Low: {
    icon: <Info className="w-5 h-5" />,
    bgColor: "bg-blue-950/50",
    borderColor: "border-blue-500/50",
    iconColor: "text-blue-400",
  },
};

interface AigRecommendationAlertProps {
  recommendations: string[];
  riskLevel?: string;
  summary?: string;
  collapsible?: boolean;
}

export function AigRecommendationAlert({
  recommendations,
  riskLevel = "Medium",
  summary,
  collapsible = true,
}: AigRecommendationAlertProps) {
  const [isExpanded, setIsExpanded] = useState(true);

  if (!recommendations || recommendations.length === 0) return null;

  const config = ALERT_CONFIG[riskLevel] ?? ALERT_CONFIG.Medium;

  return (
    <div
      className={`rounded-xl border ${config.borderColor} ${config.bgColor} overflow-hidden`}
    >
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3">
        <div className="flex items-center gap-2">
          <div className={config.iconColor}>{config.icon}</div>
          <div>
            <div className="text-sm font-semibold text-white">Recomendações AI</div>
            {summary && (
              <div className="text-xs text-slate-400 mt-0.5">{summary}</div>
            )}
          </div>
        </div>
        {collapsible && (
          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="p-1 hover:bg-white/10 rounded transition-colors"
          >
            <X className={`w-4 h-4 text-slate-400 transition-transform ${isExpanded ? "" : "rotate-45"}`} />
          </button>
        )}
      </div>

      {/* Content */}
      {(isExpanded || !collapsible) && (
        <div className="px-4 pb-4">
          <ul className="space-y-2">
            {recommendations.map((rec, index) => (
              <li key={index} className="flex items-start gap-2">
                <div className="mt-1.5 w-1.5 h-1.5 rounded-full bg-current opacity-50" />
                <span className="text-sm text-slate-300 flex-1">{rec}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

interface AigInsightBannerProps {
  message: string;
  type?: "info" | "warning" | "critical";
  onDismiss?: () => void;
}

export function AigInsightBanner({
  message,
  type = "info",
  onDismiss,
}: AigInsightBannerProps) {
  const typeConfig = {
    info: "bg-blue-950/50 border-blue-500/30 text-blue-300",
    warning: "bg-yellow-950/50 border-yellow-500/30 text-yellow-300",
    critical: "bg-red-950/50 border-red-500/30 text-red-300",
  };

  const iconConfig = {
    info: <Info className="w-4 h-4" />,
    warning: <AlertTriangle className="w-4 h-4" />,
    critical: <AlertTriangle className="w-4 h-4" />,
  };

  return (
    <div
      className={`flex items-center justify-between gap-3 px-4 py-3 rounded-lg border ${typeConfig[type]}`}
    >
      <div className="flex items-center gap-2">
        {iconConfig[type]}
        <span className="text-sm font-medium">{message}</span>
      </div>
      {onDismiss && (
        <button
          onClick={onDismiss}
          className="p-1 hover:bg-white/10 rounded transition-colors"
        >
          <X className="w-4 h-4 opacity-50" />
        </button>
      )}
    </div>
  );
}
