"use client";

import { useEffect, useState, useCallback } from "react";
import {
  fetchAlerts,
  markAlertsRead,
} from "@/lib/api/alerts";
import type { AlertFilters, AlertListResponse, MarkAlertReadRequest } from "@/lib/types/alert";

export function useAlerts(filters: AlertFilters = {}) {
  const [data, setData] = useState<AlertListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  const load = useCallback(async (pageToLoad: number = 1, append: boolean = false) => {
    if (append) setLoadingMore(true);
    else setLoading(true);
    setError(null);

    try {
      const res = await fetchAlerts({ ...filters, page: pageToLoad, pageSize: 20 });
      setData(prev => {
        if (append && prev) {
          return { ...res, items: [...prev.items, ...res.items] };
        }
        return res;
      });
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load alerts");
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  }, [filters]);

  useEffect(() => {
    let active = true;
    setPage(1);
    load(1, false);

    return () => {
      active = false;
    };
  }, [filters.severity, filters.isRead]);

  useEffect(() => {
    if (page > 1) {
      load(page, true);
    }
  }, [page]);

  const loadMore = useCallback(() => {
    if (data && page * 20 < data.totalCount) {
      setPage(p => p + 1);
    }
  }, [data, page]);

  const markRead = async (request: MarkAlertReadRequest) => {
    await markAlertsRead(request);
    await load(1, false);
  };

  const refresh = useCallback(() => {
    setPage(1);
    load(1, false);
  }, [load]);

  const unreadCount = data?.items.filter((a) => !a.isRead).length ?? 0;
  const hasMore = data ? page * 20 < data.totalCount : false;

  return { data, loading, loadingMore, error, unreadCount, markRead, refresh, loadMore, hasMore };
}