"use client";

import { useState } from "react";
import { AiQueryBox } from "./AiQueryBox";
import { AiQueryResult } from "./AiQueryResult";

interface Message {
  id: string;
  question: string;
  answer: string | null;
  sources: string[];
  confidence?: number;
  language?: string;
  isLoading?: boolean;
  error?: string | null;
  timestamp: Date;
}

interface AiChatWidgetProps {
  initialOpen?: boolean;
}

export function AiChatWidget({ initialOpen = false }: Readonly<AiChatWidgetProps>) {
  const [isOpen, setIsOpen] = useState(initialOpen);
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (question: string, contextType: string) => {
    const messageId = crypto.randomUUID();
    const newMessage: Message = {
      id: messageId,
      question,
      answer: null,
      sources: [],
      isLoading: true,
      error: null,
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, newMessage]);
    setIsLoading(true);

    try {
      const response = await fetch("/api/ai/query", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ question, contextType }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || `Request failed with status ${response.status}`);
      }

      const data = await response.json();

      setMessages((prev) =>
        prev.map((msg) =>
          msg.id === messageId
            ? {
                ...msg,
                answer: data.answer,
                sources: data.sources || [],
                confidence: data.confidence,
                language: data.language,
                isLoading: false,
              }
            : msg
        )
      );
    } catch (err) {
      setMessages((prev) =>
        prev.map((msg) =>
          msg.id === messageId
            ? {
                ...msg,
                isLoading: false,
                error: err instanceof Error ? err.message : "Unknown error occurred",
              }
            : msg
        )
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      {/* Floating button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className={`fixed bottom-6 right-6 z-50 w-14 h-14 rounded-full bg-emerald-500 text-white shadow-lg flex items-center justify-center transition-all hover:bg-emerald-400 hover:scale-105 ${
          isOpen ? "rotate-0" : ""
        }`}
        aria-label={isOpen ? "Fechar assistente AI" : "Abrir assistente AI"}
      >
        {isOpen ? (
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        ) : (
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
            />
          </svg>
        )}
      </button>

      {/* Chat panel */}
      {isOpen && (
        <div className="fixed bottom-24 right-6 z-50 w-96 max-w-[calc(100vw-3rem)] bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[70vh]">
          {/* Header */}
          <div className="bg-slate-800 px-4 py-3 border-b border-slate-700 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-full bg-emerald-500/20 flex items-center justify-center">
                <svg className="w-4 h-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
                  />
                </svg>
              </div>
              <div>
                <h3 className="text-white font-medium text-sm">Assistente GeoRisk AI</h3>
                <p className="text-slate-500 text-xs">Análise de risco de incêndio</p>
              </div>
            </div>
            <button
              onClick={() => setIsOpen(false)}
              className="text-slate-400 hover:text-white transition-colors"
              aria-label="Fechar"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* Messages */}
          <div className="flex-1 overflow-y-auto p-4 space-y-4">
            {messages.length === 0 ? (
              <div className="text-center py-8">
                <div className="w-12 h-12 mx-auto mb-3 rounded-full bg-slate-800 flex items-center justify-center">
                  <svg className="w-6 h-6 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
                    />
                  </svg>
                </div>
                <p className="text-slate-400 text-sm">
                  Faça uma pergunta sobre eventos de incêndio ou dados meteorológicos
                </p>
                <p className="text-slate-600 text-xs mt-1">
                  Ex: &quot;Qual foi o maior incêndio na região de Setúbal?&quot;
                </p>
              </div>
            ) : (
              messages.map((message) => (
                <div key={message.id} className="space-y-2">
                  {/* Question */}
                  <div className="bg-slate-800 rounded-lg px-3 py-2">
                    <p className="text-slate-400 text-xs mb-1">Você perguntou:</p>
                    <p className="text-white text-sm">{message.question}</p>
                  </div>

                  {/* Answer */}
                  <AiQueryResult
                    answer={message.answer}
                    sources={message.sources}
                    confidence={message.confidence}
                    language={message.language}
                    isLoading={message.isLoading}
                    error={message.error}
                  />
                </div>
              ))
            )}
          </div>

          {/* Input */}
          <div className="p-4 border-t border-slate-700 bg-slate-800/50">
            <AiQueryBox onSubmit={handleSubmit} isLoading={isLoading} />
          </div>
        </div>
      )}
    </>
  );
}