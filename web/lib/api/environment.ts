import apiClient from "@/lib/api-client";
import type {
  ApiAirQualityResponse,
  ApiPollenResponse,
  RegionAirQualityResponse,
  AirQualityResponse,
  PollenResponse,
} from "@/lib/types/environment";
import {
  adaptAirQuality,
  adaptPollen,
} from "@/lib/types/environment";

// Air Quality endpoint — adapts raw API response to UI-friendly format
export async function getAirQuality(
  lat: number,
  lon: number
): Promise<AirQualityResponse> {
  const { data } = await apiClient.get<ApiAirQualityResponse>(
    `/environment/air-quality/${lat}/${lon}`
  );
  return adaptAirQuality(data);
}

// Pollen endpoint — adapts raw API response to UI-friendly format
export async function getPollen(lat: number, lon: number): Promise<PollenResponse> {
  const { data } = await apiClient.get<ApiPollenResponse>(
    `/environment/pollen/${lat}/${lon}`
  );
  return adaptPollen(data);
}

// Regional grid endpoint
export async function getRegionAirQuality(): Promise<RegionAirQualityResponse> {
  const { data } = await apiClient.get<RegionAirQualityResponse>(
    "/environment/air-quality/region"
  );
  return data;
}
