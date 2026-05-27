import type { RiskLevel } from "./event";

export interface RiskZoneResponse {
  id: string;
  name: string;
  wkt: string;
  riskLevel: RiskLevel;
  score: number;
  calculatedAt: string;
}

export interface RiskDashboardResponse {
  totalZones: number;
  criticalZones: number;
  highRiskZones: number;
  zones: RiskZoneResponse[];
}
