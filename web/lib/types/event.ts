export type EventType = "Fire" | "Flood" | "Storm" | "Landslide" | "Industrial" | "Heatwave" | "Other";
export type RiskLevel = "Low" | "Medium" | "High" | "Critical";
export type EventSource = "ICNF" | "IPMA" | "ANEPC" | "Manual" | "AI_Detected";

export interface GeoEvent {
  id: string;
  eventType: EventType;
  title: string;
  description: string | null;
  latitude: number;
  longitude: number;
  severity: RiskLevel;
  source: EventSource;
  occurredAt: string;
  aiClassification: string | null;
  aiInsight: string | null;
  createdAt: string;
}

export interface EventListResponse {
  items: GeoEvent[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EventFilters {
  type?: EventType;
  severity?: RiskLevel;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
