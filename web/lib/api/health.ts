import apiClient from "@/lib/api-client";
import type { JobStatusResponse } from "@/lib/types/health";

export async function fetchJobStatuses(): Promise<JobStatusResponse[]> {
  const { data } = await apiClient.get<JobStatusResponse[]>("/health/jobs");
  return data;
}

export async function syncJobs(): Promise<void> {
  await apiClient.post("/health/jobs/sync", {});
}
