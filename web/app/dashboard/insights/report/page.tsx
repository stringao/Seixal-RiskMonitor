"use client";

import { useState } from "react";
import { useGenerateReport } from "@/lib/hooks/useInsights";
import { AIReport } from "@/components/insights/AIReport";
import { Button } from "@/components/ui/Button";
import { FileText, Calendar } from "lucide-react";

export default function ReportPage() {
  const { data, loading, error, generate } = useGenerateReport();
  const [from, setFrom] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split("T")[0];
  });
  const [to, setTo] = useState(() => new Date().toISOString().split("T")[0]);

  const handleGenerate = () => {
    generate(from, to);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">AI Report Generator</h1>
        <p className="text-sm text-slate-400 mt-1">Generate comprehensive markdown reports from event data</p>
      </div>

      <div className="bg-slate-800/60 rounded-xl p-6">
        <div className="flex items-center gap-2 mb-4">
          <Calendar className="w-5 h-5 text-emerald-400" />
          <h2 className="text-lg font-semibold text-white">Date Range</h2>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
          <div>
            <label className="block text-xs text-slate-400 mb-1">From</label>
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="w-full px-3 py-2 bg-slate-700/50 border border-slate-600 rounded-lg text-white text-sm focus:outline-none focus:border-emerald-500"
            />
          </div>
          <div>
            <label className="block text-xs text-slate-400 mb-1">To</label>
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="w-full px-3 py-2 bg-slate-700/50 border border-slate-600 rounded-lg text-white text-sm focus:outline-none focus:border-emerald-500"
            />
          </div>
        </div>
        <Button onClick={handleGenerate} isLoading={loading}>
          <FileText className="w-4 h-4 mr-2" />
          Generate Report
        </Button>
        {error && <p className="text-red-400 text-sm mt-2">{error}</p>}
      </div>

      <AIReport content={data} loading={loading && !data} />
    </div>
  );
}