export interface FireReportResponse {
  id: string;
  geoEventId: string;
  status: "InProgress" | "Completed" | "Failed";
  startedAt: string;
  completedAt: string | null;
  generatedBy: string;
  summary: string | null;
  probableCause: string | null;
  causeConfidence: number | null;
  peakFireTime: string | null;
  peakFireAreaHa: number | null;
  totalAreaHa: number;
  affectedAreas: string[];
  evacuationCount: number | null;
  structuresDestroyed: number | null;
  firefightersDeployed: number | null;
  durationHours: number | null;
  fwiConditions: FwiConditionResponse | null;
  weatherConditions: WeatherConditionResponse | null;
  timeline: TimelineEntryResponse[] | null;
  generatedContent: string | null;
  createdAt: string;
}

export interface FwiConditionResponse {
  startFwi: number;
  peakFwi: number;
  averageFwi: number;
  windDirection: string | null;
}

export interface WeatherConditionResponse {
  avgTemp: number;
  maxTemp: number;
  minHumidity: number;
  totalPrecipitation: number;
  dominantWindDir: string | null;
}

export interface TimelineEntryResponse {
  time: string;
  event: string;
  description: string;
  areaHa: number | null;
  fwi: number | null;
}

export interface FireTimelineResponse {
  eventId: string;
  eventTitle: string;
  occurredAt: string;
  timeline: TimelineEntryResponse[];
}

export interface SeasonalStatisticsResponse {
  id: string;
  year: number;
  month: number;
  monthLabel: string;
  region: string;
  totalFires: number;
  totalAreaHa: number;
  largestFireHa: number;
  averageFwi: number;
  averageTemperature: number;
  totalPrecipitationMm: number;
  peakFireDay: string | null;
  fireCauseBreakdown: Record<string, number> | null;
  dailyFireCounts: number[] | null;
  calculatedAt: string;
}

export interface TrendAnalysisResponse {
  years: number[];
  firesPerYear: number[];
  areaPerYear: number[];
  trendDirection: string;
  trendDescription: string;
  averageFiresPerYear: number;
  averageAreaPerYear: number;
}

export interface SeasonComparisonResponse {
  currentFires: number | null;
  historicalAvgFires: number | null;
  currentAreaHa: number | null;
  historicalAvgAreaHa: number | null;
  comparison: string;
  comparisonDescription: string;
  fireChange: number | null;
  areaChange: number | null;
  fwiChange: number | null;
  year: number;
  month: number;
  region: string;
}
