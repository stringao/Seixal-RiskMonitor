export interface JobStatusResponse {
  name: string;
  description: string;
  lastRun: string | null;
  status: string;
  error: string | null;
  source: string | null;
  displayName: string | null;
  itemsSyncedLastRun: number;
  lastError: string | null;
}
