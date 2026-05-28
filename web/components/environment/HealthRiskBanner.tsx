"use client";

import { useState } from "react";
import { AlertTriangle, X, ChevronDown, ChevronUp } from "lucide-react";

interface HealthRiskBannerProps {
  pollenLevel?: string;
  pollenIndex?: number;
  airQualityLevel?: string;
  airQualityAqi?: number;
  message?: string;
}

const SEVERITY_CONFIG = {
  low: {
    bgColor: "bg-emerald-500/10",
    borderColor: "border-emerald-500/30",
    textColor: "text-emerald-400",
    iconColor: "text-emerald-400",
    label: "Baixo",
  },
  moderate: {
    bgColor: "bg-yellow-500/10",
    borderColor: "border-yellow-500/30",
    textColor: "text-yellow-400",
    iconColor: "text-yellow-400",
    label: "Moderado",
  },
  high: {
    bgColor: "bg-orange-500/10",
    borderColor: "border-orange-500/30",
    textColor: "text-orange-400",
    iconColor: "text-orange-400",
    label: "Elevado",
  },
  veryHigh: {
    bgColor: "bg-red-500/10",
    borderColor: "border-red-500/30",
    textColor: "text-red-400",
    iconColor: "text-red-400",
    label: "Muito Elevado",
  },
};

function getSeverity(level: string | undefined, index: number | undefined): keyof typeof SEVERITY_CONFIG {
  if (!level) return "low";
  if (level === "VeryHigh" || (index && index >= 4)) return "veryHigh";
  if (level === "High" || (index && index >= 3)) return "high";
  if (level === "Moderate" || (index && index >= 2)) return "moderate";
  return "low";
}

export function HealthRiskBanner({
  pollenLevel,
  pollenIndex,
  airQualityLevel,
  airQualityAqi,
  message,
}: HealthRiskBannerProps) {
  const [dismissed, setDismissed] = useState(false);
  const [expanded, setExpanded] = useState(false);

  // Determine if we should show the banner
  const shouldShow = (pollenIndex && pollenIndex >= 3) || (airQualityAqi && airQualityAqi >= 3);

  if (!shouldShow || dismissed) return null;

  const severity = getSeverity(pollenLevel, pollenIndex);
  const config = SEVERITY_CONFIG[severity] ?? SEVERITY_CONFIG.low;

  return (
    <div
      className={`${config.bgColor} border ${config.borderColor} rounded-xl overflow-hidden`}
    >
      {/* Main content */}
      <div className="p-4">
        <div className="flex items-start gap-3">
          <AlertTriangle className={`w-5 h-5 ${config.iconColor} flex-shrink-0 mt-0.5`} />
          <div className="flex-1">
            <div className="flex items-center justify-between">
              <h4 className={`font-semibold ${config.textColor}`}>
                Aviso para alérgicos
              </h4>
              <div className="flex items-center gap-2">
                {pollenLevel && (
                  <span className={`text-xs px-2 py-0.5 rounded-full bg-slate-800/50 ${config.textColor}`}>
                    Polén {config.label}
                  </span>
                )}
                <button
                  onClick={() => setDismissed(true)}
                  className="text-slate-400 hover:text-white transition-colors"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>
            </div>
            <p className="text-sm text-slate-300 mt-1">
              {message ||
                (pollenIndex && pollenIndex >= 3
                  ? `Elevado para polén. Concentrações altas esperadas nas próximas 24h.`
                  : airQualityAqi && airQualityAqi >= 3
                  ? `Qualidade do ar ${config.label.toLowerCase()}. Máximo cuidado recomendado.`
                  : "Condições atmosféricas desfavoráveis para alérgicos.")}
            </p>
          </div>
        </div>
      </div>

      {/* Expandable details */}
      <button
        onClick={() => setExpanded(!expanded)}
        className="w-full px-4 py-2 bg-slate-800/30 border-t border-slate-700/30 flex items-center justify-between text-xs text-slate-400 hover:text-slate-300 transition-colors"
      >
        <span>Saber mais</span>
        {expanded ? (
          <ChevronUp className="w-4 h-4" />
        ) : (
          <ChevronDown className="w-4 h-4" />
        )}
      </button>

      {expanded && (
        <div className="px-4 pb-4 pt-2 border-t border-slate-700/30">
          <div className="grid grid-cols-2 gap-4 text-sm">
            {pollenLevel && (
              <div>
                <span className="text-slate-400">Índice de Polén:</span>
                <p className={`font-medium ${config.textColor}`}>
                  {pollenIndex}/5 - {config.label}
                </p>
              </div>
            )}
            {airQualityAqi && (
              <div>
                <span className="text-slate-400">Qualidade do Ar:</span>
                <p className={`font-medium ${config.textColor}`}>
                  {airQualityAqi}/5
                </p>
              </div>
            )}
          </div>
          <div className="mt-3 p-3 bg-slate-900/40 rounded-lg text-xs text-slate-300">
            Recomenda-se evitar exposição prolongada ao ar livre, especialmente durante
            as horas de maior concentração (10h-16h). Mantenha janelas fechadas e
            considere o uso de filtros de ar em ambientes internos.
          </div>
        </div>
      )}
    </div>
  );
}