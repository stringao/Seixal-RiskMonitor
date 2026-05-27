"use client";

import { useState, useEffect, useCallback } from "react";
import type {
  DashboardResponse,
  SituationReportResponse,
  WeeklyReportResponse,
  DashboardComparison,
} from "@/lib/types/dashboard";

interface UseAiDashboardReturn {
  dashboard: DashboardResponse | null;
  situation: SituationReportResponse | null;
  weekly: WeeklyReportResponse | null;
  comparison: DashboardComparison | null;
  loading: boolean;
  error: string | null;
  refresh: () => void;
}

export function useAiDashboard(): UseAiDashboardReturn {
  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null);
  const [situation, setSituation] = useState<SituationReportResponse | null>(null);
  const [weekly, setWeekly] = useState<WeeklyReportResponse | null>(null);
  const [comparison, setComparison] = useState<DashboardComparison | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const [dashboardRes, situationRes, weeklyRes, comparisonRes] = await Promise.all([
        fetch("/api/ai/dashboard/today").then((r) => r.json()).catch(() => null),
        fetch("/api/ai/dashboard/situation").then((r) => r.json()).catch(() => null),
        fetch("/api/ai/dashboard/weekly").then((r) => r.json()).catch(() => null),
        fetch("/api/ai/dashboard/compare").then((r) => r.json()).catch(() => null),
      ]);

      if (dashboardRes?.error) setError(dashboardRes.error);
      else if (situationRes?.error) setError(situationRes.error);
      else if (weeklyRes?.error) setError(weeklyRes.error);

      setDashboard(dashboardRes);
      setSituation(situationRes);
      setWeekly(weeklyRes);
      setComparison(comparisonRes);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch dashboard data");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    // Refresh every 5 minutes for situation, 30 minutes for others
    const situationInterval = setInterval(() => {
      fetch("/api/ai/dashboard/situation")
        .then((r) => r.json())
        .then((data) => {
          if (!data.error) setSituation(data);
        })
        .catch(() => {});
    }, 5 * 60 * 1000);

    return () => clearInterval(situationInterval);
  }, [fetchData]);

  return {
    dashboard,
    situation,
    weekly,
    comparison,
    loading,
    error,
    refresh: fetchData,
  };
}
