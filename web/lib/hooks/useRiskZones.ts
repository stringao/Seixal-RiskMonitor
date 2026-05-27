"use client";

import { useEffect, useState } from "react";
import { getDetailedRiskZones } from "@/lib/api-client";
import type { DetailedRiskZonesResponse } from "@/lib/types/riskZone";

export function useRiskZones(minRiskLevel?: string, refreshKey: number = 0) {
  const [data, setData] = useState<DetailedRiskZonesResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    getDetailedRiskZones(minRiskLevel)
      .then((res) => {
        if (!cancelled) setData(res);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load risk zones");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => { cancelled = true; };
  }, [minRiskLevel, refreshKey]);

  // Auto-refresh every 30 minutes
  useEffect(() => {
    const interval = setInterval(() => {
      getDetailedRiskZones(minRiskLevel)
        .then((res) => setData(res))
        .catch(() => {});
    }, 30 * 60 * 1000);
    return () => clearInterval(interval);
  }, [minRiskLevel]);

  return { data, loading, error };
}