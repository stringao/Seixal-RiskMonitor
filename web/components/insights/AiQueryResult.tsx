"use client";

interface Source {
  id: string;
  type: "GeoEvent" | "FireHotspot" | "WeatherData" | "FireReport";
  label: string;
}

interface AiQueryResultProps {
  answer: string | null;
  sources?: string[];
  confidence?: number;
  language?: string;
  isLoading?: boolean;
  error?: string | null;
}

export function AiQueryResult({
  answer,
  sources = [],
  confidence,
  language,
  isLoading,
  error,
}: Readonly<AiQueryResultProps>) {
  if (isLoading) {
    return (
      <div className="bg-slate-800/60 rounded-xl p-6 space-y-3">
        <div className="flex items-center gap-2">
          <span className="h-4 w-4 animate-spin rounded-full border-2 border-emerald-500/30 border-t-emerald-500" />
          <span className="text-slate-400 text-sm">A processar pergunta...</span>
        </div>
        <div className="space-y-2">
          <div className="h-4 w-full bg-slate-700/60 rounded animate-pulse" />
          <div className="h-4 w-5/6 bg-slate-700/60 rounded animate-pulse" />
          <div className="h-4 w-4/6 bg-slate-700/60 rounded animate-pulse" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-900/20 border border-red-800 rounded-xl p-4">
        <p className="text-red-400 text-sm">{error}</p>
      </div>
    );
  }

  if (!answer) {
    return null;
  }

  const parseSources = (srcs: string[]): Source[] => {
    return srcs.map((src) => {
      const [type, id] = src.split(":");
      return {
        id,
        type: type as Source["type"],
        label: `${type}:${id.slice(0, 8)}...`,
      };
    });
  };

  return (
    <div className="bg-slate-800/60 rounded-xl p-6 space-y-4">
      {/* Header with confidence and language */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="text-emerald-400 text-sm font-medium">Resposta da IA</span>
          {language && (
            <span className="text-slate-500 text-xs uppercase">{language}</span>
          )}
        </div>
        {confidence !== undefined && (
          <div className="flex items-center gap-2">
            <span className="text-slate-500 text-xs">Confiança:</span>
            <div className="w-16 h-1.5 bg-slate-700 rounded-full overflow-hidden">
              <div
                className="h-full bg-emerald-500 rounded-full transition-all"
                style={{ width: `${confidence * 100}%` }}
              />
            </div>
            <span className="text-slate-400 text-xs">{(confidence * 100).toFixed(0)}%</span>
          </div>
        )}
      </div>

      {/* Answer content with markdown-like rendering */}
      <div className="prose prose-invert prose-sm max-w-none">
        {answer.split("\n").map((line, i) => {
          if (line.startsWith("# ")) {
            return (
              <h1 key={i} className="text-xl font-bold text-white mt-2 mb-2">
                {line.slice(2)}
              </h1>
            );
          }
          if (line.startsWith("## ")) {
            return (
              <h2 key={i} className="text-lg font-semibold text-slate-200 mt-3 mb-2">
                {line.slice(3)}
              </h2>
            );
          }
          if (line.startsWith("- ")) {
            return (
              <li key={i} className="text-slate-400 ml-4 list-disc">
                {line.slice(2)}
              </li>
            );
          }
          if (line.trim() === "") {
            return <br key={i} />;
          }
          return (
            <p key={i} className="text-slate-300 leading-relaxed">
              {line}
            </p>
          );
        })}
      </div>

      {/* Sources */}
      {sources.length > 0 && (
        <div className="border-t border-slate-700 pt-3">
          <span className="text-slate-500 text-xs font-medium uppercase tracking-wide">
            Fontes
          </span>
          <div className="flex flex-wrap gap-2 mt-2">
            {parseSources(sources).map((source) => (
              <span
                key={source.id}
                className="inline-flex items-center gap-1 px-2 py-1 bg-slate-700/60 rounded text-xs text-slate-400"
                title={source.id}
              >
                <span className="w-2 h-2 rounded-full bg-emerald-500/60" />
                {source.type}:{source.id.slice(0, 6)}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}