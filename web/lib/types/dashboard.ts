export interface DashboardResponse {
  riskLevel: "Low" | "Medium" | "High" | "Critical";
  trend: "increasing" | "decreasing" | "stable";
  confidence: number;
  keyFactors: KeyFactorDto[];
  summary: string;
  recommendations: string[];
  sources: string[];
  generatedAt: string;
}

export interface KeyFactorDto {
  factor: string;
  value: string;
  description: string;
}

export interface WeeklyReportResponse {
  weekNumber: number;
  year: number;
  dayByDayBreakdown: DayBreakdownDto[];
  comparisonPreviousWeek: ComparisonDto;
  comparisonFiveYearAverage: ComparisonDto;
  weatherForecastImpact: string;
  strategicRecommendations: string[];
  summary: string;
}

export interface DayBreakdownDto {
  date: string;
  dayName: string;
  eventCount: number;
  dominantRisk: string;
  weatherSummary: string;
}

export interface ComparisonDto {
  changePercent: number;
  changeDescription: string;
  isIncrease: boolean;
}

export interface SituationReportResponse {
  activeEvents: SituationEventDto[];
  currentFwi: CurrentFwiDto;
  hotspots: SituationHotspotDto[];
  weatherTrend: string;
  overallRiskLevel: string;
  immediateRecommendations: string[];
  generatedAt: string;
}

export interface SituationEventDto {
  id: string;
  title: string;
  eventType: string;
  severity: string;
  occurredAt: string;
}

export interface CurrentFwiDto {
  value: number;
  riskLevel: string;
  trend: string;
  averageRegion: number;
}

export interface SituationHotspotDto {
  id: string;
  name: string;
  riskLevel: string;
  fireCount: number;
}

export interface EventSummaryResponse {
  topEvents: TopEventDto[];
  patterns: string[];
  severityDistribution: SeverityDistributionDto;
  sourceBreakdown: SourceBreakdownDto;
}

export interface TopEventDto {
  id: string;
  title: string;
  eventType: string;
  severity: string;
  occurredAt: string;
  location: string;
  significance: string;
}

export interface SeverityDistributionDto {
  critical: number;
  high: number;
  medium: number;
  low: number;
}

export interface SourceBreakdownDto {
  icnf: number;
  anepc: number;
  ipma: number;
  manual: number;
  aiDetected: number;
}

export interface HotspotAnalysisResponse {
  concerningHotspots: ConcerningHotspotDto[];
  recentActivity: string;
  historicalAverage: number;
  recommendations: string[];
}

export interface ConcerningHotspotDto {
  id: string;
  name: string;
  currentFwi: number;
  riskLevel: string;
  concernReason: string;
}

export interface DashboardComparison {
  currentWeekEvents: number;
  previousWeekEvents: number;
  weekOverWeekChange: number;
  currentFwiAverage: number;
  historicalFwiAverage: number;
  fwiTrend: "increasing" | "decreasing" | "stable";
  generatedAt: string;
}
