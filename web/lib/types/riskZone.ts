// Risk Zone types
export type RiskLevelName = "Low" | "Medium" | "High" | "Critical";

export interface DataPoint {
  latitude: number;
  longitude: number;
  fwi: number;
  temperature: number;
  humidity: number;
  timestamp: string;
}

export interface RiskZone {
  id: string;
  riskLevelName: RiskLevelName;
  riskIndex: number;
  temperature: number;
  humidity: number;
  windSpeed: number;
  windDirection: string;
  wkt: string;
  conclusion: string;
  calculatedAt: string;
  dataPoints: DataPoint[];
}

export interface DetailedRiskZonesResponse {
  zones: RiskZone[];
  lastUpdated: string;
  totalPoints: number;
}

// Fire Spread types
export interface FireLocation {
  latitude: number;
  longitude: number;
  title: string;
}

export interface WeatherConditions {
  temperature: number;
  humidity: number;
  windSpeed: number;
  windDirection: string;
  fwi: number;
  isi: number;
}

export interface HorizonPrediction {
  hours: number;
  polygonWkt: string;
  rosKmh: number;
  areaKm2: number;
  affectedMunicipalities: string[];
  conclusion: string;
}

export interface FireSpreadResponse {
  fireEventId: string;
  fireLocation: FireLocation;
  currentWeather: WeatherConditions;
  scenario: string;
  horizons: HorizonPrediction[];
  lastUpdated: string;
}