"use client";

import { useEffect, useState } from "react";
import {
  fetchAlerts,
  markAlertsRead,
} from "@/lib/api/alerts";
import type { AlertFilters, AlertListResponse, MarkAlertReadRequest } from "@/lib/types/alert";

export function useAlerts(filters: AlertFilters = {}) {
  const [data, setData] = useState<AlertListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unreadCount, setUnreadCount] = useState(0);

  const load = async () => {
    setLoading(true);
    setError(null);

    try {
      const res = await fetchAlerts(filters);
      setData(res);
      setUnreadCount(res.items.filter((a) => !a.isRead).length);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load alerts");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let active = true;

    fetchAlerts(filters)
      .then((res) => {
        if (active) {
          setData(res);
          setUnreadCount(res.items.filter((a) => !a.isRead).length);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load alerts");
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [filters.severity, filters.isRead, filters.page, filters.pageSize]);

  const markRead = async (request: MarkAlertReadRequest) => {
    await markAlertsRead(request);
    await load();
  };

  return { data, loading, error, unreadCount, markRead, refresh: load };
}