import apiClient from "@/lib/api-client";
import type { FireStationListResponse } from "@/lib/types/fireStation";

export async function fetchFireStations(filters: {
  type?: string;
  county?: string;
  district?: string;
  incluirInativos?: boolean;
}): Promise<FireStationListResponse> {
  const params = new URLSearchParams();
  if (filters.type) params.set("type", filters.type);
  if (filters.county) params.set("county", filters.county);
  if (filters.district) params.set("district", filters.district);
  if (filters.incluirInativos) params.set("includeInactive", "true");

  const { data } = await apiClient.get<FireStationListResponse>(`/fire-stations?${params.toString()}`);
  return data;
}

export async function fetchNearestFireStations(lat: number, lng: number, count = 5) {
  const { data } = await apiClient.get(`/fire-stations/nearest?lat=${lat}&lng=${lng}&count=${count}`);
  return data;
}