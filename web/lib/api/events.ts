import apiClient from "@/lib/api-client";
import type { EventFilters, EventListResponse, GeoEvent } from "@/lib/types/event";

export async function fetchEvents(filters: EventFilters = {}): Promise<EventListResponse> {
  const params = new URLSearchParams();
  if (filters.type) params.set("type", filters.type);
  if (filters.severity) params.set("severity", filters.severity);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));

  const { data } = await apiClient.get<EventListResponse>(`/events?${params.toString()}`);
  return data;
}

export async function fetchEventById(id: string): Promise<GeoEvent> {
  const { data } = await apiClient.get<GeoEvent>(`/events/${id}`);
  return data;
}
