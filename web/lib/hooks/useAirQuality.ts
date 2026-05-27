"use client";

import { useEffect, useState, useCallback } from "react";
import {
  getAirQuality,
  getPollen,
  getRegionAirQuality,
} from "@/lib/api/environment";
import type {
  AirQualityResponse,
  PollenResponse,
  RegionAirQualityResponse,
} from "@/lib/types/environment";

// Auto-refresh interval for air quality (30 minutes)
const AIR_QUALITY_REFRESH_INTERVAL = 30 * 60 * 1000;

// Hook for fetching air quality data
export function useAirQuality(lat: number, lon: number) {
  const [data, setData] = useState<AirQualityResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setError(null);
      const result = await getAirQuality(lat, lon);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load air quality");
    } finally {
      setLoading(false);
    }
  }, [lat, lon]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);

    fetchData().then(() => {
      if (cancelled) return;
      setLoading(false);
    });

    // Set up auto-refresh
    const intervalId = setInterval(() => {
      if (!cancelled) fetchData();
    }, AIR_QUALITY_REFRESH_INTERVAL);

    return () => {
      cancelled = true;
      clearInterval(intervalId);
    };
  }, [fetchData]);

  const refetch = useCallback(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch };
}

// Hook for fetching pollen data
export function usePollen(lat: number, lon: number) {
  const [data, setData] = useState<PollenResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setError(null);
      const result = await getPollen(lat, lon);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load pollen data");
    } finally {
      setLoading(false);
    }
  }, [lat, lon]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);

    fetchData().then(() => {
      if (cancelled) return;
      setLoading(false);
    });

    const intervalId = setInterval(() => {
      if (!cancelled) fetchData();
    }, AIR_QUALITY_REFRESH_INTERVAL);

    return () => {
      cancelled = true;
      clearInterval(intervalId);
    };
  }, [fetchData]);

  const refetch = useCallback(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch };
}

// Hook for fetching regional air quality grid
export function useRegionAirQuality() {
  const [data, setData] = useState<RegionAirQualityResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    getRegionAirQuality()
      .then((result) => {
        if (!cancelled) setData(result);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load regional data");
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const refetch = useCallback(async () => {
    try {
      setError(null);
      const result = await getRegionAirQuality();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to reload regional data");
    }
  }, []);

  return { data, loading, error, refetch };
}