import { SEVERITY_COLORS, EVENT_TYPE_LABELS } from "@/lib/map/config";

const TYPE_PATHS: Record<string, string> = {
  Fire: "M17.66 11.2C17.43 10.9 17.15 10.64 16.89 10.38C16.22 9.78 15.46 9.35 14.82 8.72C13.33 7.26 13 4.85 13.95 3C13 3.23 12.17 3.75 11.46 4.32C8.87 6.4 7.85 10.07 9.07 13.22C9.11 13.32 9.15 13.42 9.15 13.55C9.15 13.77 9 13.97 8.8 14.05C8.57 14.15 8.33 14.09 8.14 13.93C8.08 13.88 8.04 13.83 8 13.76C6.87 12.33 6.69 10.28 7.45 8.64C5.78 10 4.87 12.3 5 14.47C5.06 15.58 5.5 16.68 6 17.69C7.08 19.44 8.95 20.89 10.96 21.68C11.36 21.78 11.78 21.61 12 21.35C12.22 21.09 12.35 20.7 12.22 20.36C12.1 20.05 11.94 19.76 11.8 19.47C11.14 18.19 11.11 16.71 11.6 15.39C12.24 13.57 13.81 12.23 15.68 12.23C16.43 12.23 17.13 12.38 17.77 12.67C17.94 12.74 18.12 12.75 18.28 12.71C18.45 12.67 18.6 12.58 18.72 12.44C18.84 12.31 18.91 12.15 18.91 11.97C18.87 11.72 18.78 11.5 18.66 11.31L17.66 11.2ZM16.5 14C15.28 14 14.18 13.36 13.55 12.35C13.8 12.05 14.17 11.88 14.57 11.88C15.47 11.88 16.2 12.6 16.2 13.5C16.2 13.9 16.03 14.27 15.72 14.5C15.97 14.21 16.28 14.09 16.5 14Z",
  Flood: "M12 2C6.48 2 2 6.48 2 12C2 17.52 6.48 22 12 22C17.52 22 22 17.52 22 12C22 6.48 17.52 2 12 2ZM12 20C7.58 20 4 16.42 4 12C4 7.58 7.58 4 12 4C16.42 4 20 7.58 20 12C20 16.42 16.42 20 12 20ZM7 13V15H9V13H7ZM11 13V17H13V13H11Z",
  Storm: "M19 8C19.55 8 20 7.55 20 7C20 6.45 19.55 6 19 6C18.45 6 18 6.45 18 7C18 7.55 18.45 8 19 8ZM19 10C18.04 10 17.36 10.68 17.36 11.64C17.36 12.6 18.04 13.28 19 13.28C19.96 13.28 20.64 12.6 20.64 11.64C20.64 10.68 19.96 10 19 10ZM15.45 8.15C15.15 7.85 14.7 7.85 14.4 8.15L10.4 12.15C10.1 12.45 10.1 12.9 10.4 13.2L11.8 14.6C12.1 14.9 12.55 14.9 12.85 14.6L16.85 10.6C17.15 10.3 17.15 9.85 16.85 9.55L15.45 8.15ZM4 15.5L2 22H22L20 15.5L17 18.5L14 15.5L11 18.5L8 15.5L5 18.5L4 15.5Z",
  Landslide: "M14 2H6C4.9 2 4 2.9 4 4V20C4 21.1 4.9 22 6 22H18C19.1 22 20 21.1 20 20V8L14 2ZM16 16H13V13H11V16H8L12 20L16 16Z",
  Industrial: "M19 3H5C3.9 3 3 3.9 3 5V19C3 20.1 3.9 21 5 21H19C20.1 21 21 20.1 21 19V5C21 3.9 20.1 3 19 3ZM19 19H5V5H19V19ZM7 17H9V10H7ZM11 17H13V7H11ZM15 17H17V13H15Z",
  Heatwave: "M12 7C9.24 7 7 9.24 7 12C7 14.76 9.24 17 12 17C14.76 17 17 14.76 17 12C17 9.24 14.76 7 12 7ZM12 15C10.34 15 9 13.66 9 12C9 10.34 10.34 9 12 9C13.66 9 15 10.34 15 12C15 13.66 13.66 15 12 15ZM11 2V5H13V2ZM13 22V19H11V22ZM5 13V11H2V13ZM22 13H19V11H22Z",
  Other: "M12 2C6.48 2 2 6.48 2 12C2 17.52 6.48 22 12 22C17.52 22 22 17.52 22 12C22 6.48 17.52 2 12 2ZM13 17H11V15H13ZM13 13H11V7H13Z",
};

const TYPE_DESCRIPTIONS: Record<string, string> = {
  Fire: "Incêndio florestal e urbano",
  Flood: "Inundação e cheia",
  Storm: "Tempestade e ventos fortes",
  Landslide: "Deslizamento de terra",
  Industrial: "Acidente industrial",
  Heatwave: "Onda de calor",
  Other: "Outro tipo de evento",
};

const severityItems = [
  { label: "Baixo", color: SEVERITY_COLORS.Low },
  { label: "Médio", color: SEVERITY_COLORS.Medium },
  { label: "Alto", color: SEVERITY_COLORS.High },
  { label: "Crítico", color: SEVERITY_COLORS.Critical },
];

const FIRE_STATION_TYPES: Record<string, { label: string; path: string }> = {
  Voluntarios: {
    label: "Voluntários",
    path: "M3 13l1-7h2l1 4h2l1-4h2l1 7h2v8h-2v-5h-2v-2h-2v2h-2v5H3v-8z M9 2c1.5 0 2.7.8 3.3 2M9 6c1 0 1.8.5 2.2 1.3",
  },
  Sapadores: {
    label: "Sapadores",
    path: "M2 22h20V12l-6-4V4H8v4L2 12v10z M8 4v4h2V4 M14 4v4h2V4",
  },
  Mistos: {
    label: "Mistos",
    path: "M12 3L2 12h3v8h14v-8h3L12 3z M12 7c1.5 0 2.7.8 3.3 2",
  },
};

const STATION_ICON_COLOR = "#1e40af";

export function Legend() {
  return (
    <div className="bg-slate-800/95 backdrop-blur-sm border border-slate-700 rounded-xl p-5 text-xs shadow-xl w-80">
      <div className="font-semibold text-slate-200 mb-4 flex items-center gap-2 text-sm">
        <svg className="w-4 h-4 text-emerald-400" viewBox="0 0 24 24" fill="currentColor">
          <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />
        </svg>
        Legenda
      </div>

      <div className="space-y-4">
        {/* Severidade */}
        <div>
          <div className="text-[10px] uppercase tracking-wider text-slate-500 font-semibold mb-2">
            Severidade
          </div>
          <div className="space-y-1.5">
            {severityItems.map((item) => (
              <div key={item.label} className="flex items-center gap-2.5">
                <span
                  className="w-3.5 h-3.5 rounded-full shrink-0 ring-1 ring-white/20"
                  style={{ backgroundColor: item.color }}
                />
                <span className="text-slate-300">{item.label}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Tipo de Evento */}
        <div className="pt-4 border-t border-slate-700">
          <div className="text-[10px] uppercase tracking-wider text-slate-500 font-semibold mb-3">
            Tipo de Evento
          </div>
          <div className="space-y-2.5">
            {Object.entries(TYPE_PATHS).map(([type, path]) => (
              <div key={type} className="flex items-center gap-3">
                <svg
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  className="shrink-0"
                  style={{ color: SEVERITY_COLORS.Critical }}
                >
                  <circle cx="12" cy="12" r="11" fill="currentColor" opacity="0.15" />
                  <circle cx="12" cy="12" r="8" fill="currentColor" opacity="0.25" />
                  <path d={path} fill="white" transform="translate(2,2) scale(0.83)" />
                </svg>
                <div className="flex flex-col min-w-0">
                  <span className="text-slate-200 text-[12px] font-medium leading-none mb-0.5">
                    {EVENT_TYPE_LABELS[type]}
                  </span>
                  <span className="text-slate-500 text-[10px] leading-tight">
                    {TYPE_DESCRIPTIONS[type]}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Postos de Bombeiros */}
        <div className="pt-4 border-t border-slate-700">
          <div className="text-[10px] uppercase tracking-wider text-slate-500 font-semibold mb-3">
            Postos de Bombeiros
          </div>
          <div className="space-y-2.5">
            {Object.entries(FIRE_STATION_TYPES).map(([key, data]) => (
              <div key={key} className="flex items-center gap-3">
                <svg
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  className="shrink-0"
                  style={{ color: STATION_ICON_COLOR }}
                >
                  <circle cx="12" cy="12" r="11" fill="currentColor" opacity="0.15" />
                  <circle cx="12" cy="12" r="8" fill="currentColor" opacity="0.25" />
                  <path d={data.path} fill="white" stroke="none" transform="translate(0,1) scale(0.9)" />
                </svg>
                <span className="text-slate-200 text-[12px] font-medium">
                  {data.label}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
