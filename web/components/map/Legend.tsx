import { SEVERITY_COLORS } from "@/lib/map/config";

const items = [
  { label: "Baixo", color: SEVERITY_COLORS.Low },
  { label: "Médio", color: SEVERITY_COLORS.Medium },
  { label: "Alto", color: SEVERITY_COLORS.High },
  { label: "Crítico", color: SEVERITY_COLORS.Critical },
];

export function Legend() {
  return (
    <div className="bg-slate-800/90 backdrop-blur-sm border border-slate-700 rounded-lg p-3 text-xs">
      <div className="font-semibold text-slate-300 mb-2">Severidade</div>
      <div className="space-y-1.5">
        {items.map((item) => (
          <div key={item.label} className="flex items-center gap-2">
            <span
              className="w-3 h-3 rounded-full shrink-0"
              style={{ backgroundColor: item.color }}
            />
            <span className="text-slate-400">{item.label}</span>
          </div>
        ))}
      </div>
    </div>
  );
}