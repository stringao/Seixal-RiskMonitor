export const MAP_CONFIG = {
  center: { lat: 38.6267, lng: -9.1048 } as const,
  zoom: 13,
  tileUrl: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
  tileAttribution:
    '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
} as const;

export const SEVERITY_COLORS: Record<string, string> = {
  Low: "#22c55e",
  Medium: "#eab308",
  High: "#f97316",
  Critical: "#ef4444",
};

export const EVENT_TYPE_LABELS: Record<string, string> = {
  Fire: "Incêndio",
  Flood: "Inundação",
  Storm: "Tempestade",
  Landslide: "Deslizamento",
  Industrial: "Industrial",
  Heatwave: "Onda de Calor",
  Other: "Outro",
};
