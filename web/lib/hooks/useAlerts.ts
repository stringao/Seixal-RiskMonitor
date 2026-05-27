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
  const [pageSize, setPageSize] = useState(5);

  const load = useCallback(async (pageToLoad: number, size: number, append: boolean = false) => {
    if (append) setLoadingMore(true);
    else setLoading(true);
    setError(null);

    try {
      const res = await fetchAlerts({ ...filters, page: pageToLoad, pageSize: size });
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

  // Initial load and reload when filters change
  useEffect(() => {
    setPage(1);
    load(1, pageSize, false);
  }, [filters.severity, filters.isRead, pageSize]);

  // Load more when page changes (pagination)
  useEffect(() => {
    if (page > 1) {
      load(page, pageSize, true);
    }
  }, [page]);

  const loadMore = useCallback(() => {
    if (data && page * pageSize < data.totalCount) {
      setPage(p => p + 1);
    }
  }, [data, page, pageSize]);

  const markRead = async (request: MarkAlertReadRequest) => {
    await markAlertsRead(request);
    load(1, pageSize, false);
  };

  const refresh = useCallback(() => {
    setPage(1);
    load(1, pageSize, false);
  }, [load, pageSize]);

  const changePageSize = useCallback((newSize: number) => {
    setPageSize(newSize);
    setPage(1);
  }, []);

  const changePage = useCallback((newPage: number) => {
    setPage(newPage);
  }, []);

  const unreadCount = data?.items.filter((a) => !a.isRead).length ?? 0;
  const hasMore = data ? page * pageSize < data.totalCount : false;

  return {
    data,
    loading,
    loadingMore,
    error,
    unreadCount,
    markRead,
    refresh,
    loadMore,
    hasMore,
    page,
    pageSize,
    setPage: changePage,
    setPageSize: changePageSize,
  };
}