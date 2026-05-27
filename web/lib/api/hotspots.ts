import apiClient from "@/lib/api-client";
import type { FireHotspot, HotspotDetail, HotspotNear } from "@/lib/types/hotspot";

export async function fetchHotspots(riskLevel?: string): Promise<FireHotspot[]> {
  const params = riskLevel ? `?riskLevel=${riskLevel}` : "";
  const { data } = await apiClient.get<FireHotspot[]>(`/insights/hotspots${params}`);
  return data;
}

export async function fetchActiveHotspots(): Promise<FireHotspot[]> {
  const { data } = await apiClient.get<FireHotspot[]>("/insights/hotspots/active");
  return data;
}

export async function fetchHotspotById(id: string): Promise<HotspotDetail> {
  const { data } = await apiClient.get<HotspotDetail>(`/insights/hotspots/${id}`);
  return data;
}

export async function fetchHotspotsNear(
  lat: number,
  lon: number,
  radiusKm: number = 10
): Promise<HotspotNear[]> {
  const { data } = await apiClient.get<HotspotNear[]>(
    `/insights/hotspots/near/${lat}/${lon}?radiusKm=${radiusKm}`
  );
  return data;
}