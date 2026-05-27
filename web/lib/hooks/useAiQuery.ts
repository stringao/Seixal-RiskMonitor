"use client";

import { useState, useCallback } from "react";

interface AiQueryResponse {
  answer: string;
  sources: string[];
  confidence: number;
  language: string;
}

interface AiQueryState {
  data: AiQueryResponse | null;
  loading: boolean;
  error: string | null;
}

export function useAiQuery() {
  const [state, setState] = useState<AiQueryState>({
    data: null,
    loading: false,
    error: null,
  });

  const query = useCallback(async (question: string, contextType: string = "full") => {
    const controller = new AbortController();
    setState({ data: null, loading: true, error: null });

    try {
      const response = await fetch("/api/ai/query", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ question, contextType }),
        signal: controller.signal,
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || `Request failed with status ${response.status}`);
      }

      const data = await response.json();
      setState({ data, loading: false, error: null });
    } catch (err) {
      if (err instanceof Error && err.name !== "AbortError") {
        setState({ data: null, loading: false, error: err.message });
      }
    }

    return () => controller.abort();
  }, []);

  const reset = useCallback(() => {
    setState({ data: null, loading: false, error: null });
  }, []);

  return { ...state, query, reset };
}

interface CompareResponse {
  targetEventId: string;
  similarEvents: Array<{
    eventId: string;
    score: number;
    title?: string;
    occurredAt: string;
    severity: string;
  }>;
  analysis: string;
}

export function useCompareEvents() {
  const [data, setData] = useState<CompareResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const compare = useCallback(async (eventId: string, topN: number = 5) => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`/api/ai/compare/${eventId}?topN=${topN}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
      });

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      const result = await response.json();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Comparison failed");
    } finally {
      setLoading(false);
    }
  }, []);

  return { data, loading, error, compare };
}

interface SummaryResponse {
  totalEvents: number;
  fireEvents: number;
  activeHotspots: number;
  currentRiskLevel: string;
  weatherTrend: string;
  fwi: number;
  temperature: number;
  windSpeed: number;
  summaryText: string;
}

export function useSummary() {
  const [data, setData] = useState<SummaryResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchSummary = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch("/api/ai/summary", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      });

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      const result = await response.json();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Summary generation failed");
    } finally {
      setLoading(false);
    }
  }, []);

  return { data, loading, error, fetchSummary };
}