"use client";

interface AIReportProps {
  content: string | null;
  loading?: boolean;
}

export function AIReport({ content, loading }: Readonly<AIReportProps>) {
  if (loading) {
    return (
      <div className="bg-slate-800/60 rounded-xl p-8 space-y-4">
        <div className="h-6 w-48 bg-slate-700/60 rounded animate-pulse" />
        <div className="space-y-2">
          <div className="h-4 w-full bg-slate-700/60 rounded animate-pulse" />
          <div className="h-4 w-5/6 bg-slate-700/60 rounded animate-pulse" />
          <div className="h-4 w-4/6 bg-slate-700/60 rounded animate-pulse" />
        </div>
      </div>
    );
  }

  if (!content) {
    return (
      <div className="bg-slate-800/60 rounded-xl p-8 text-center">
        <p className="text-slate-500">No report generated yet.</p>
      </div>
    );
  }

  const renderMarkdown = (text: string) => {
    const lines = text.split("\n");
    return lines.map((line, i) => {
      if (line.startsWith("# ")) {
        return <h1 key={i} className="text-2xl font-bold text-white mt-6 mb-3">{line.slice(2)}</h1>;
      }
      if (line.startsWith("## ")) {
        return <h2 key={i} className="text-xl font-semibold text-slate-200 mt-5 mb-2">{line.slice(3)}</h2>;
      }
      if (line.startsWith("### ")) {
        return <h3 key={i} className="text-lg font-medium text-slate-300 mt-4 mb-2">{line.slice(4)}</h3>;
      }
      if (line.startsWith("- ")) {
        return <li key={i} className="text-slate-400 ml-4 list-disc">{line.slice(2)}</li>;
      }
      if (line.trim() === "") {
        return <br key={i} />;
      }
      return <p key={i} className="text-slate-400 leading-relaxed">{line}</p>;
    });
  };

  return (
    <div className="bg-slate-800/60 rounded-xl p-8">
      <article className="prose prose-invert prose-sm max-w-none">
        {renderMarkdown(content)}
      </article>
    </div>
  );
}