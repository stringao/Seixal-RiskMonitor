import apiClient from "@/lib/api-client";
import type { RiskDashboardResponse } from "@/lib/types/risk";

export async function fetchRiskDashboard(): Promise<RiskDashboardResponse> {
  const { data } = await apiClient.get<RiskDashboardResponse>("/risk/dashboard");
  return data;
}

export interface SlopePoint {
  slope: number;
  category: "flat" | "moderate" | "steep" | "very_steep";
}

export interface SlopeVisualizationFeature {
  type: "Feature";
  geometry: {
    type: "Point";
    coordinates: [number, number];
  };
  properties: SlopePoint;
}

export interface SlopeVisualizationResponse {
  type: "FeatureCollection";
  features: SlopeVisualizationFeature[];
}

export async function fetchSlopeVisualization(): Promise<SlopeVisualizationResponse> {
  const { data } = await apiClient.get<SlopeVisualizationResponse>("/risk/terrain/slope");
  return data;
}
