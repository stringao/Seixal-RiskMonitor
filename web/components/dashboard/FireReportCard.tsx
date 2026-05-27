"use client";

import { useState } from "react";
import {
  Flame,
  Clock,
  MapPin,
  AlertTriangle,
  TrendingUp,
  Wind,
  Droplets,
  Thermometer,
  User,
  CheckCircle,
  XCircle,
  Loader2,
} from "lucide-react";
import type { FireReportResponse } from "@/lib/types/fireReport";

interface FireReportCardProps {
  report: FireReportResponse;
  loading?: boolean;
}

const CAUSE_LABELS: Record<string, string> = {
  natural: "Natural",
  accidental: "Acidental",
  intentional: "Intencional",
  unknown: "Desconhecida",
};

const CAUSE_COLORS: Record<string, string> = {
  natural: "text-green-400",
  accidental: "text-yellow-400",
  intentional: "text-red-400",
  unknown: "text-slate-400",
};

const STATUS_CONFIG: Record<
  string,
  { icon: React.ReactNode; color: string; label: string }
> = {
  Completed: {
    icon: <CheckCircle className="w-4 h-4" />,
    color: "text-emerald-400",
    label: "Concluído",
  },
  Failed: {
    icon: <XCircle className="w-4 h-4" />,
    color: "text-red-400",
    label: "Falhou",
  },
  InProgress: {
    icon: <Loader2 className="w-4 h-4 animate-spin" />,
    color: "text-yellow-400",
    label: "Em Processamento",
  },
};

export function FireReportCard({ report, loading }: FireReportCardProps) {
  const [expanded, setExpanded] = useState(false);

  if (loading) {
    return (
      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 animate-pulse">
        <div className="h-6 w-48 bg-slate-700/60 rounded mb-4" />
        <div className="space-y-3">
          <div className="h-4 w-full bg-slate-700/60 rounded" />
          <div className="h-4 w-5/6 bg-slate-700/60 rounded" />
          <div className="h-4 w-4/6 bg-slate-700/60 rounded" />
        </div>
      </div>
    );
  }

  const statusConfig = STATUS_CONFIG[report.status] || STATUS_CONFIG.InProgress;

  return (
    <div className="bg-slate-800/60 border border-slate-700 rounded-xl overflow-hidden">
      {/* Header */}
      <div className="p-4 border-b border-slate-700/50">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-orange-500/20 rounded-lg">
              <Flame className="w-5 h-5 text-orange-400" />
            </div>
            <div>
              <h3 className="font-semibold text-white">
                Relatório de Análise
              </h3>
              <p className="text-xs text-slate-400 mt-0.5">
                ID: {report.id.slice(0, 8)}...
              </p>
            </div>
          </div>
          <div className={`flex items-center gap-1.5 text-sm ${statusConfig.color}`}>
            {statusConfig.icon}
            <span>{statusConfig.label}</span>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="p-4 space-y-4">
        {/* Summary */}
        {report.summary && (
          <p className="text-sm text-slate-300 leading-relaxed">
            {report.summary}
          </p>
        )}

        {/* Stats Grid */}
        <div className="grid grid-cols-2 gap-3">
          <StatItem
            icon={<MapPin className="w-4 h-4 text-slate-400" />}
            label="Área Total"
            value={`${report.totalAreaHa.toFixed(2)} ha`}
          />
          <StatItem
            icon={<TrendingUp className="w-4 h-4 text-slate-400" />}
            label="Área de Pico"
            value={
              report.peakFireAreaHa
                ? `${report.peakFireAreaHa.toFixed(2)} ha`
                : "N/A"
            }
          />
          <StatItem
            icon={<Clock className="w-4 h-4 text-slate-400" />}
            label="Duração"
            value={
              report.durationHours
                ? `${report.durationHours.toFixed(1)}h`
                : "N/A"
            }
          />
          <StatItem
            icon={<AlertTriangle className="w-4 h-4 text-slate-400" />}
            label="Causa Provável"
            value={
              report.probableCause
                ? CAUSE_LABELS[report.probableCause] || report.probableCause
                : "N/A"
            }
            valueColor={report.probableCause ? CAUSE_COLORS[report.probableCause] : undefined}
          />
        </div>

        {/* Cause Confidence */}
        {report.causeConfidence && (
          <div className="space-y-1">
            <div className="flex justify-between text-xs">
              <span className="text-slate-400">Confiança da Análise</span>
              <span className="text-slate-300">
                {(report.causeConfidence * 100).toFixed(0)}%
              </span>
            </div>
            <div className="h-1.5 bg-slate-700 rounded-full overflow-hidden">
              <div
                className="h-full bg-emerald-500 rounded-full transition-all"
                style={{ width: `${report.causeConfidence * 100}%` }}
              />
            </div>
          </div>
        )}

        {/* Affected Areas */}
        {report.affectedAreas.length > 0 && (
          <div>
            <span className="text-xs text-slate-400">Áreas Afetadas</span>
            <div className="flex flex-wrap gap-1.5 mt-1">
              {report.affectedAreas.map((area, i) => (
                <span
                  key={i}
                  className="text-xs px-2 py-0.5 bg-slate-700/60 text-slate-300 rounded-full"
                >
                  {area}
                </span>
              ))}
            </div>
          </div>
        )}

        {/* FWI Conditions */}
        {report.fwiConditions && (
          <div className="bg-slate-900/40 rounded-lg p-3 space-y-2">
            <span className="text-xs font-medium text-slate-400 uppercase tracking-wide">
              Condições FWI
            </span>
            <div className="grid grid-cols-3 gap-2 text-xs">
              <div>
                <span className="text-slate-500">FWI Inicial</span>
                <p className="text-white font-medium">
                  {report.fwiConditions.startFwi.toFixed(1)}
                </p>
              </div>
              <div>
                <span className="text-slate-500">FWI Pico</span>
                <p className="text-orange-400 font-medium">
                  {report.fwiConditions.peakFwi.toFixed(1)}
                </p>
              </div>
              <div>
                <span className="text-slate-500">FWI Médio</span>
                <p className="text-white font-medium">
                  {report.fwiConditions.averageFwi.toFixed(1)}
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Weather Conditions */}
        {report.weatherConditions && (
          <div className="bg-slate-900/40 rounded-lg p-3 space-y-2">
            <span className="text-xs font-medium text-slate-400 uppercase tracking-wide">
              Condições Meteorológicas
            </span>
            <div className="grid grid-cols-4 gap-2 text-xs">
              <div className="flex items-center gap-1">
                <Thermometer className="w-3 h-3 text-red-400" />
                <span className="text-slate-400">Max</span>
                <span className="text-white ml-auto">
                  {report.weatherConditions.maxTemp.toFixed(0)}°
                </span>
              </div>
              <div className="flex items-center gap-1">
                <Droplets className="w-3 h-3 text-blue-400" />
                <span className="text-slate-400">Humid</span>
                <span className="text-white ml-auto">
                  {report.weatherConditions.minHumidity.toFixed(0)}%
                </span>
              </div>
              <div className="flex items-center gap-1">
                <Wind className="w-3 h-3 text-slate-400" />
                <span className="text-slate-400">Dir</span>
                <span className="text-white ml-auto">
                  {report.weatherConditions.dominantWindDir || "N/A"}
                </span>
              </div>
              <div className="flex items-center gap-1">
                <Droplets className="w-3 h-3 text-blue-300" />
                <span className="text-slate-400">Prec</span>
                <span className="text-white ml-auto">
                  {report.weatherConditions.totalPrecipitation.toFixed(1)}
                </span>
              </div>
            </div>
          </div>
        )}

        {/* Generated Content (Expandable) */}
        {report.generatedContent && (
          <div>
            <button
              onClick={() => setExpanded(!expanded)}
              className="text-xs text-emerald-400 hover:text-emerald-300 transition-colors"
            >
              {expanded ? "▼ Ocultar" : "▶ Mostrar"} Relatório Completo
            </button>
            {expanded && (
              <div className="mt-3 p-3 bg-slate-900/40 rounded-lg text-xs text-slate-300 whitespace-pre-wrap max-h-64 overflow-y-auto">
                {report.generatedContent}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Footer */}
      <div className="px-4 py-2 bg-slate-900/30 border-t border-slate-700/50 flex justify-between text-xs text-slate-500">
        <span className="flex items-center gap-1">
          <User className="w-3 h-3" />
          Gerado por {report.generatedBy}
        </span>
        <span>
          {report.completedAt
            ? `Atualizado ${new Date(report.completedAt).toLocaleDateString("pt-BR")}`
            : `Iniciado ${new Date(report.startedAt).toLocaleDateString("pt-BR")}`}
        </span>
      </div>
    </div>
  );
}

function StatItem({
  icon,
  label,
  value,
  valueColor,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  valueColor?: string;
}) {
  return (
    <div className="bg-slate-900/40 rounded-lg p-2.5">
      <div className="flex items-center gap-1.5 mb-1">
        {icon}
        <span className="text-xs text-slate-400">{label}</span>
      </div>
      <p className={`text-sm font-medium ${valueColor || "text-white"}`}>{value}</p>
    </div>
  );
}
