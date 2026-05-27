"use client";

interface RiskFiltersProps {
  showHeatLayer: boolean;
  onHeatLayerChange: (show: boolean) => void;
  showPolygons: boolean;
  onPolygonsChange: (show: boolean) => void;
  minRiskLevel: string;
  onMinRiskLevelChange: (level: string) => void;
}

const RISK_LEVELS = ["Low", "Medium", "High", "Critical"] as const;

const RISK_LABELS: Record<string, string> = {
  Low: "Baixo",
  Medium: "Médio",
  High: "Alto",
  Critical: "Crítico",
};

export function RiskFilters({
  showHeatLayer,
  onHeatLayerChange,
  showPolygons,
  onPolygonsChange,
  minRiskLevel,
  onMinRiskLevelChange,
}: RiskFiltersProps) {
  return (
    <div className="p-4 space-y-5">
      <h3 className="text-sm font-semibold text-slate-200 uppercase tracking-wider">
        🔥 Camada de Calor
      </h3>

      <div className="space-y-2">
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={showHeatLayer}
            onChange={(e) => onHeatLayerChange(e.target.checked)}
            className="w-4 h-4 rounded border-slate-600 bg-slate-700 text-orange-500 focus:ring-orange-500 focus:ring-offset-0"
          />
          <span className="text-sm text-slate-300">Mostrar camada de calor</span>
        </label>
      </div>

      <div className="border-t border-slate-700 pt-4">
        <h3 className="text-sm font-semibold text-slate-200 uppercase tracking-wider mb-4">
          🎯 Zonas de Risco
        </h3>

        <div className="space-y-3">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={showPolygons}
              onChange={(e) => onPolygonsChange(e.target.checked)}
              className="w-4 h-4 rounded border-slate-600 bg-slate-700 text-emerald-500 focus:ring-emerald-500 focus:ring-offset-0"
            />
            <span className="text-sm text-slate-300">Mostrar polígonos</span>
          </label>

          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">
              Nível Mínimo de Risco
            </label>
            <select
              value={minRiskLevel}
              onChange={(e) => onMinRiskLevelChange(e.target.value)}
              className="w-full bg-slate-700 text-slate-200 rounded-lg px-3 py-2 text-sm border border-slate-600 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
            >
              {RISK_LEVELS.map((level) => (
                <option key={level} value={level}>
                  {RISK_LABELS[level]}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>
    </div>
  );
}