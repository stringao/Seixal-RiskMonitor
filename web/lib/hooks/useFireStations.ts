"use client";

import { useEffect, useState } from "react";
import { fetchFireStations } from "@/lib/api/fireStations";
import type { FireStationListResponse } from "@/lib/types/fireStation";

export function useFireStations(filters: {
  type?: string;
  county?: string;
  district?: string;
  incluirInativos?: boolean;
} = {}, refreshKey: number = 0) {
  const [data, setData] = useState<FireStationListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    fetchFireStations(filters)
      .then((res) => {
        if (!cancelled) setData(res);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load fire stations");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [filters.type, filters.county, filters.district, filters.incluirInativos, refreshKey]);

  return { data, loading, error };
}