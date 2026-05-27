import apiClient from "@/lib/api-client";
import type {
  AirQualityResponse,
  PollenResponse,
  RegionAirQualityResponse,
} from "@/lib/types/environment";

// Air Quality endpoint
export async function getAirQuality(
  lat: number,
  lon: number
): Promise<AirQualityResponse> {
  const { data } = await apiClient.get(`/environment/air-quality/${lat}/${lon}`);
  return data;
}

// Pollen endpoint
export async function getPollen(lat: number, lon: number): Promise<PollenResponse> {
  const { data } = await apiClient.get(`/environment/pollen/${lat}/${lon}`);
  return data;
}

// Regional grid endpoint
export async function getRegionAirQuality(): Promise<RegionAirQualityResponse> {
  const { data } = await apiClient.get("/environment/air-quality/region");
  return data;
}