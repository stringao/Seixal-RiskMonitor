"use client";

import { useState } from "react";
import { useGenerateReport } from "@/lib/hooks/useInsights";
import { AIReport } from "@/components/insights/AIReport";
import { Button } from "@/components/ui/Button";
import { FileText, Calendar, Copy, Download } from "lucide-react";

export default function ReportPage() {
  const { data, loading, error, generate } = useGenerateReport();
  const [copied, setCopied] = useState(false);
  const [from, setFrom] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split("T")[0];
  });
  const [to, setTo] = useState(() => new Date().toISOString().split("T")[0]);

  const handleGenerate = () => {
    generate(from, to);
  };

  const handleCopy = async () => {
    if (!data) return;
    await navigator.clipboard.writeText(data);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDownload = () => {
    if (!data) return;
    const blob = new Blob([data], { type: "text/markdown" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `relatorio-ai-${from}-${to}.md`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Gerador de Relatórios AI</h1>
        <p className="text-sm text-slate-400 mt-1">Gere relatórios markdown abrangentes a partir dos dados de eventos</p>
      </div>

      <div className="bg-slate-800/60 rounded-xl p-6">
        <div className="flex items-center gap-2 mb-4">
          <Calendar className="w-5 h-5 text-emerald-400" />
          <h2 className="text-lg font-semibold text-white">Intervalo de Datas</h2>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
          <div>
            <label className="block text-xs text-slate-400 mb-1">De</label>
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="w-full px-3 py-2 bg-slate-700/50 border border-slate-600 rounded-lg text-white text-sm focus:outline-none focus:border-emerald-500"
            />
          </div>
          <div>
            <label className="block text-xs text-slate-400 mb-1">Até</label>
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
          Gerar Relatório
        </Button>
        {error && (
          <div className="mt-3 p-3 bg-slate-700/50 rounded-lg border border-slate-600">
            {error.includes("not configured") || error.includes("API key") || error.includes("401") || error.includes("403") ? (
              <p className="text-sm text-slate-300">
                Funcionalidades de IA não configuradas. Configure a API key nas{" "}
                <a href="/settings" className="text-emerald-400 hover:underline">
                  Definições
                </a>
                .
              </p>
            ) : (
              <p className="text-red-400 text-sm">{error}</p>
            )}
          </div>
        )}
      </div>

      {data && (
        <div className="flex items-center gap-2">
          <Button variant="secondary" onClick={handleCopy}>
            <Copy className="w-4 h-4 mr-2" />
            {copied ? "Copiado!" : "Copiar"}
          </Button>
          <Button variant="secondary" onClick={handleDownload}>
            <Download className="w-4 h-4 mr-2" />
            Descarregar
          </Button>
        </div>
      )}

      <AIReport content={data} loading={loading && !data} />
    </div>
  );
}
