// Air Quality Index levels
export interface AirQualityIndex {
  value: number;
  level: "VeryGood" | "Good" | "Medium" | "Poor" | "Bad";
  label: string;
  color: string;
}

// Pollutant concentration
export interface Pollutant {
  name: string;
  symbol: string;
  value: number;
  unit: string;
  level: string;
  color: string;
}

// Pollen species breakdown
export interface PollenSpecies {
  name: string;
  value: number;
  level: "Low" | "Moderate" | "High" | "VeryHigh";
  levelLabel: string;
  trend?: "rising" | "falling" | "stable";
}

// Air Quality API response
export interface AirQualityResponse {
  aqi: number;
  aqiLevel: AirQualityIndex;
  pollutants: Pollutant[];
  pollen?: {
    index: number;
    level: string;
    label: string;
  };
  healthRisk: string;
  timestamp: string;
}

// Pollen API response
export interface PollenResponse {
  pollenIndex: number;
  level: string;
  label: string;
  breakdown: PollenSpecies[];
  timestamp: string;
}

// Regional air quality grid point
export interface AirQualityGridPoint {
  lat: number;
  lon: number;
  aqi: number;
  level: string;
}

// Regional air quality response
export interface RegionAirQualityResponse {
  grid: AirQualityGridPoint[];
  timestamp: string;
  bounds: {
    minLat: number;
    maxLat: number;
    minLon: number;
    maxLon: number;
  };
}

// AQI Level configurations
export const AQI_LEVELS: Record<string, { label: string; color: string; min: number; max: number }> = {
  VeryGood: { label: "Muito Bom", color: "#00E400", min: 1, max: 1 },
  Good: { label: "Bom", color: "#FFFF00", min: 2, max: 2 },
  Medium: { label: "Médio", color: "#FF7E00", min: 3, max: 3 },
  Poor: { label: "Fraco", color: "#FF0000", min: 4, max: 4 },
  Bad: { label: "Muito Mau", color: "#8F3F97", min: 5, max: 5 },
};

// Pollen Level configurations
export const POLLEN_LEVELS: Record<string, { label: string; color: string }> = {
  Low: { label: "Baixo", color: "#00E400" },
  Moderate: { label: "Moderado", color: "#FFFF00" },
  High: { label: "Alto", color: "#FF7E00" },
  VeryHigh: { label: "Muito Alto", color: "#FF0000" },
};

// Helper to get AQI level from value
export function getAQILevel(value: number): AirQualityIndex {
  const entry = Object.entries(AQI_LEVELS).find(([, config]) => value >= config.min && value <= config.max);
  const [key, config] = entry ?? ["VeryGood", AQI_LEVELS.VeryGood];
  return {
    value,
    level: key as AirQualityIndex["level"],
    label: config.label,
    color: config.color,
  };
}

// Helper to get pollen level info
export function getPollenLevelInfo(level: string): { label: string; color: string } {
  return POLLEN_LEVELS[level] ?? { label: level, color: "#FFFF00" };
}