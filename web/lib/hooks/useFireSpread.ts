"use client";

import { useEffect, useState } from "react";
import { getFireSpread, getActiveFiresSpread } from "@/lib/api-client";
import type { FireSpreadResponse } from "@/lib/types/riskZone";

export function useFireSpread(fireEventId: string) {
  const [data, setData] = useState<FireSpreadResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!fireEventId) return;

    let cancelled = false;
    setLoading(true);
    setError(null);

    getFireSpread(fireEventId)
      .then((res) => { if (!cancelled) setData(res); })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load fire spread");
      })
      .finally(() => { if (!cancelled) setLoading(false); });

    return () => { cancelled = true; };
  }, [fireEventId]);

  // Auto-refresh every 30 minutes
  useEffect(() => {
    if (!fireEventId) return;
    const interval = setInterval(() => {
      getFireSpread(fireEventId).then((res) => setData(res)).catch(() => {});
    }, 30 * 60 * 1000);
    return () => clearInterval(interval);
  }, [fireEventId]);

  return { data, loading, error };
}

export function useActiveFiresSpread(refreshKey: number = 0) {
  const [data, setData] = useState<FireSpreadResponse[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    getActiveFiresSpread()
      .then((res) => { if (!cancelled) setData(res); })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load fire spread");
      })
      .finally(() => { if (!cancelled) setLoading(false); });

    return () => { cancelled = true; };
  }, [refreshKey]);

  // Auto-refresh every 30 minutes
  useEffect(() => {
    const interval = setInterval(() => {
      getActiveFiresSpread().then((res) => setData(res)).catch(() => {});
    }, 30 * 60 * 1000);
    return () => clearInterval(interval);
  }, []);

  return { data, loading, error };
}