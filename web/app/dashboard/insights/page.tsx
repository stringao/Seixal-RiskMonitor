"use client";

import { useDetectPatterns } from "@/lib/hooks/useInsights";
import { PatternCard } from "@/components/insights/PatternCard";
import { AlertTriangle, BarChart3, TrendingUp } from "lucide-react";

export default function InsightsPage() {
  const { data, loading, error } = useDetectPatterns();

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">AI Insights</h1>
          <p className="text-sm text-slate-400 mt-1">Pattern detection and risk analysis powered by AI</p>
        </div>
        <div className="flex items-center gap-2 text-xs text-slate-500">
          <TrendingUp className="w-4 h-4" />
          <span>Last 7 days</span>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-4">
          <div className="flex items-center gap-2">
            <BarChart3 className="w-5 h-5 text-emerald-400" />
            <h2 className="text-lg font-semibold text-white">Detected Patterns</h2>
          </div>

          {loading && (
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="bg-slate-800/60 rounded-lg p-5 animate-pulse">
                  <div className="h-4 w-32 bg-slate-700/60 rounded mb-3" />
                  <div className="h-3 w-full bg-slate-700/60 rounded mb-2" />
                  <div className="h-3 w-5/6 bg-slate-700/60 rounded" />
                </div>
              ))}
            </div>
          )}

          {error && (
            <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-6 text-center">
              <p className="text-red-400">Failed to load patterns: {error}</p>
            </div>
          )}

          {!loading && !error && data && data.patterns.length === 0 && (
            <div className="bg-slate-800/60 rounded-xl p-8 text-center">
              <AlertTriangle className="w-8 h-8 text-slate-600 mx-auto mb-3" />
              <p className="text-slate-500">No patterns detected in the last 7 days.</p>
            </div>
          )}

          {!loading && !error && data && data.patterns.length > 0 && (
            <div className="space-y-4">
              {data.patterns.map((pattern, idx) => (
                <PatternCard key={idx} pattern={pattern} />
              ))}
            </div>
          )}
        </div>

        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <TrendingUp className="w-5 h-5 text-emerald-400" />
            <h2 className="text-lg font-semibold text-white">Quick Actions</h2>
          </div>
          <div className="bg-slate-800/60 rounded-lg p-4 space-y-3">
            <a
              href="/dashboard/insights/report"
              className="flex items-center gap-3 p-3 rounded-lg bg-slate-700/50 hover:bg-slate-700 transition-colors"
            >
              <BarChart3 className="w-4 h-4 text-emerald-400" />
              <span className="text-sm text-slate-200">Generate Report</span>
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}