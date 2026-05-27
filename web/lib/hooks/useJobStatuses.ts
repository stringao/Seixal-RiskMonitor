"use client";

import { useEffect, useState } from "react";
import { fetchJobStatuses } from "@/lib/api/health";
import type { JobStatusResponse } from "@/lib/types/health";

export function useJobStatuses(refreshKey: number = 0) {
  const [data, setData] = useState<JobStatusResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    fetchJobStatuses()
      .then((res) => {
        if (!cancelled) setData(res);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load job statuses");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [refreshKey]);

  return { data, loading, error };
}
