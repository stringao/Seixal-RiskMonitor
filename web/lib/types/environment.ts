// ─── Real API Response Types (from .NET backend) ──────────────────────────

// Current air quality readings from Open-Meteo
export interface ApiCurrentAirQuality {
  timestamp: string;
  pm10: number | null;
  pm25: number | null;
  nitrogenDioxide: number | null;
  ozone: number | null;
  sulphurDioxide: number | null;
  carbonMonoxide: number | null;
  dust: number | null;
  aerosolOpticalDepth: number | null;
}

// Today's aggregated air quality summary
export interface ApiTodaySummary {
  pm10: number | null;
  pm25: number | null;
  nitrogenDioxide: number | null;
  ozone: number | null;
  sulphurDioxide: number | null;
  carbonMonoxide: number | null;
  aqiValue: number;
  aqiCategory: string;
  pollenIndex: number;
  pollenCategory: string;
  healthRiskLevel: string;
}

// Daily forecast entry
export interface ApiForecastDay {
  date: string;
  pm10: number | null;
  pm25: number | null;
  nitrogenDioxide: number | null;
  ozone: number | null;
  aqiValue: number;
  aqiCategory: string;
  pollenIndex: number;
  pollenCategory: string;
}

// Real API response shape for /air-quality/{lat}/{lon}
export interface ApiAirQualityResponse {
  latitude: number;
  longitude: number;
  current: ApiCurrentAirQuality | null;
  todayAverage: ApiTodaySummary | null;
  forecast: ApiForecastDay[];
}

// Real API response shape for /pollen/{lat}/{lon}
export interface ApiPollenResponse {
  pollenIndex: number;
  pollenCategory: string;
  dominantPollen: string;
  current: {
    grassPollen: number | null;
    olivePollen: number | null;
    alderPollen: number | null;
    birchPollen: number | null;
    mugwortPollen: number | null;
    ragweedPollen: number | null;
  } | null;
  breakdown: Record<string, number>;
}

// ─── Frontend Component Types (adapted for UI components) ─────────────────

// Air Quality Index levels
export interface AirQualityIndex {
  value: number;
  level: "VeryGood" | "Good" | "Medium" | "Poor" | "Bad";
  label: string;
  color: string;
}

// Pollutant concentration (for UI display)
export interface Pollutant {
  name: string;
  symbol: string;
  value: number;
  unit: string;
  level: string;
  color: string;
}

// Pollen species breakdown (for UI display)
export interface PollenSpecies {
  name: string;
  value: number;
  level: "Low" | "Moderate" | "High" | "VeryHigh";
  levelLabel: string;
  trend?: "rising" | "falling" | "stable";
}

// Adapted air quality response for UI components
export interface AirQualityResponse {
  aqi: number;
  aqiLevel: AirQualityIndex;
  pollutants: Pollutant[];
  healthRisk: string;
  timestamp: string;
  // Keep raw data for detailed pages
  raw: ApiAirQualityResponse;
}

// Adapted pollen response for UI components
export interface PollenResponse {
  pollenIndex: number;
  level: string;
  label: string;
  breakdown: PollenSpecies[];
  dominantPollen: string;
  timestamp: string;
  raw: ApiPollenResponse;
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

// Chart data point
export interface AirQualityChartDataPoint {
  timestamp: string;
  aqi: number;
  pollen?: number;
}

// ─── AQI Level configurations ─────────────────────────────────────────────

export const AQI_LEVELS: Record<string, { label: string; color: string; min: number; max: number }> = {
  VeryGood: { label: "Muito Bom", color: "#00E400", min: 1, max: 1 },
  Good: { label: "Bom", color: "#FFFF00", min: 2, max: 2 },
  Medium: { label: "Médio", color: "#FF7E00", min: 3, max: 3 },
  Poor: { label: "Fraco", color: "#FF0000", min: 4, max: 4 },
  Bad: { label: "Muito Mau", color: "#8F3F97", min: 5, max: 5 },
};

export const POLLEN_LEVELS: Record<string, { label: string; color: string }> = {
  None: { label: "Nenhum", color: "#00E400" },
  Low: { label: "Baixo", color: "#00E400" },
  Moderate: { label: "Moderado", color: "#FFFF00" },
  High: { label: "Alto", color: "#FF7E00" },
  VeryHigh: { label: "Muito Alto", color: "#FF0000" },
};

// ─── Helper functions ─────────────────────────────────────────────────────

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

export function getPollenLevelInfo(level: string): { label: string; color: string } {
  return POLLEN_LEVELS[level] ?? { label: level, color: "#FFFF00" };
}

// Get pollutant severity color based on value thresholds
function getPollutantColor(value: number, thresholds: number[]): string {
  const colors = ["#00E400", "#FFFF00", "#FF7E00", "#FF0000", "#8F3F97"];
  for (let i = 0; i < thresholds.length; i++) {
    if (value <= thresholds[i]) return colors[i];
  }
  return colors[colors.length - 1];
}

// Get pollutant level label
function getPollutantLevel(value: number, thresholds: number[]): string {
  const levels = ["Muito Bom", "Bom", "Médio", "Fraco", "Mau"];
  for (let i = 0; i < thresholds.length; i++) {
    if (value <= thresholds[i]) return levels[i];
  }
  return levels[levels.length - 1];
}

// ─── API → UI Adapters ────────────────────────────────────────────────────

export function adaptAirQuality(apiData: ApiAirQualityResponse): AirQualityResponse {
  const today = apiData.todayAverage;
  const current = apiData.current;

  const aqiValue = today?.aqiValue ?? 1;
  const aqiCategory = today?.aqiCategory ?? "VeryGood";

  // Map AQI category string to our level format
  const levelMap: Record<string, "VeryGood" | "Good" | "Medium" | "Poor" | "Bad"> = {
    "Very Good": "VeryGood",
    "Good": "Good",
    "Medium": "Medium",
    "Poor": "Poor",
    "Bad": "Bad",
  };

  const aqiLevel: AirQualityIndex = {
    value: aqiValue,
    level: levelMap[aqiCategory] ?? "Good",
    label: AQI_LEVELS[levelMap[aqiCategory] ?? "Good"]?.label ?? "Bom",
    color: AQI_LEVELS[levelMap[aqiCategory] ?? "Good"]?.color ?? "#FFFF00",
  };

  // Build pollutants list from current readings
  const pollutants: Pollutant[] = [];

  const addPollutant = (
    name: string, symbol: string, value: number | null, unit: string,
    thresholds: number[]
  ) => {
    if (value !== null && value !== undefined) {
      pollutants.push({
        name,
        symbol,
        value: Math.round(value * 10) / 10,
        unit,
        level: getPollutantLevel(value, thresholds),
        color: getPollutantColor(value, thresholds),
      });
    }
  };

  addPollutant("PM10", "PM10", current?.pm10 ?? today?.pm10 ?? null, "µg/m³", [20, 35, 50, 100]);
  addPollutant("PM2.5", "PM2.5", current?.pm25 ?? today?.pm25 ?? null, "µg/m³", [10, 20, 25, 50]);
  addPollutant("Dióxido de Azoto", "NO₂", current?.nitrogenDioxide ?? today?.nitrogenDioxide ?? null, "µg/m³", [40, 90, 120, 230]);
  addPollutant("Ozono", "O₃", current?.ozone ?? today?.ozone ?? null, "µg/m³", [50, 100, 130, 240]);
  addPollutant("Dióxido de Enxofre", "SO₂", current?.sulphurDioxide ?? today?.sulphurDioxide ?? null, "µg/m³", [50, 100, 200, 350]);
  addPollutant("Monóxido de Carbono", "CO", current?.carbonMonoxide ?? today?.carbonMonoxide ?? null, "µg/m³", [200, 400, 800, 1000]);

  return {
    aqi: aqiValue,
    aqiLevel,
    pollutants,
    healthRisk: today?.healthRiskLevel ?? "Low",
    timestamp: current?.timestamp ?? new Date().toISOString(),
    raw: apiData,
  };
}

export function adaptPollen(apiData: ApiPollenResponse): PollenResponse {
  // Convert API pollen category to our PollenSpecies format
  const pollenNames: Record<string, string> = {
    grass: "Erva",
    olive: "Oliveira",
    alder: "Amieiro",
    birch: "Bétula",
    mugwort: "Artemísia",
    ragweed: "Ambrosia",
  };

  const getPollenLevel = (value: number): "Low" | "Moderate" | "High" | "VeryHigh" => {
    if (value <= 10) return "Low";
    if (value <= 30) return "Moderate";
    if (value <= 60) return "High";
    return "VeryHigh";
  };

  const breakdown: PollenSpecies[] = Object.entries(apiData.breakdown)
    .map(([key, value]) => {
      const level = getPollenLevel(value);
      const levelInfo = POLLEN_LEVELS[level] ?? POLLEN_LEVELS.Low;
      return {
        name: pollenNames[key] ?? key,
        value: Math.round(value * 10) / 10,
        level,
        levelLabel: levelInfo.label,
      };
    })
    .filter((s) => s.value > 0)
    .sort((a, b) => b.value - a.value);

  const pollenLevelInfo = getPollenLevelInfo(apiData.pollenCategory);

  return {
    pollenIndex: apiData.pollenIndex,
    level: apiData.pollenCategory,
    label: pollenLevelInfo.label,
    breakdown,
    dominantPollen: apiData.dominantPollen,
    timestamp: new Date().toISOString(),
    raw: apiData,
  };
}

// Build chart data from forecast
export function buildChartDataFromForecast(forecast: ApiForecastDay[]): AirQualityChartDataPoint[] {
  return forecast.map((day) => ({
    timestamp: new Date(day.date).toLocaleDateString("pt-PT", { weekday: "short", day: "numeric" }),
    aqi: day.aqiValue,
    pollen: day.pollenIndex,
  }));
}
