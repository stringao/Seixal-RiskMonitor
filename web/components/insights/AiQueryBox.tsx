"use client";

import { useState } from "react";
import { Button } from "../ui/Button";

interface AiQueryBoxProps {
  onSubmit: (question: string, contextType: string) => Promise<void>;
  isLoading?: boolean;
}

export function AiQueryBox({ onSubmit, isLoading }: Readonly<AiQueryBoxProps>) {
  const [question, setQuestion] = useState("");
  const [contextType, setContextType] = useState("full");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!question.trim() || isLoading) return;
    await onSubmit(question, contextType);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="flex gap-2">
        <input
          type="text"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          placeholder="Faça uma pergunta sobre incêndios, risco meteorológico, hotspots..."
          disabled={isLoading}
          className="flex-1 rounded-lg border bg-slate-800 px-3 py-2.5 text-sm text-white placeholder-slate-500 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 border-slate-600"
        />
        <select
          value={contextType}
          onChange={(e) => setContextType(e.target.value)}
          className="bg-slate-700 text-slate-200 rounded-lg px-3 py-2 text-sm border border-slate-600"
          disabled={isLoading}
        >
          <option value="full">Portugal</option>
          <option value="regional">Regional</option>
          <option value="event">Evento</option>
          <option value="hotspot">Hotspot</option>
          <option value="seasonal">Sazonal</option>
        </select>
      </div>
      <Button type="submit" isLoading={isLoading}>
        Perguntar à IA
      </Button>
    </form>
  );
}