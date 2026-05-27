import apiClient from "@/lib/api-client";
import type { RiskDashboardResponse } from "@/lib/types/risk";

export async function fetchRiskDashboard(): Promise<RiskDashboardResponse> {
  const { data } = await apiClient.get<RiskDashboardResponse>("/risk/dashboard");
  return data;
}
