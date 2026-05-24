"use client";

import { useEffect, useState } from "react";
import { fetchEvents } from "@/lib/api/events";
import type { EventFilters, EventListResponse } from "@/lib/types/event";

export function useEvents(filters: EventFilters = {}) {
  const [data, setData] = useState<EventListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    fetchEvents(filters)
      .then((res) => {
        if (!cancelled) setData(res);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load events");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [filters.type, filters.severity, filters.from, filters.to, filters.page, filters.pageSize]);

  return { data, loading, error };
}
