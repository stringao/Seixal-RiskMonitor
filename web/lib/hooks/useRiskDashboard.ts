"use client";

import { useEffect, useState } from "react";
import { fetchRiskDashboard } from "@/lib/api/risk";
import type { RiskDashboardResponse } from "@/lib/types/risk";

export function useRiskDashboard(refreshKey: number = 0) {
  const [data, setData] = useState<RiskDashboardResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    fetchRiskDashboard()
      .then((res) => {
        if (!cancelled) setData(res);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load risk data");
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
