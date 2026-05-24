"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { fetchEventById } from "@/lib/api/events";
import type { GeoEvent } from "@/lib/types/event";
import { SEVERITY_COLORS, EVENT_TYPE_LABELS } from "@/lib/map/config";
import { format } from "date-fns";
import { pt } from "date-fns/locale";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";

export default function EventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [event, setEvent] = useState<GeoEvent | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    fetchEventById(id)
      .then(setEvent)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "Failed"))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="animate-pulse text-slate-400">A carregar...</div>;
  if (error) return <div className="text-red-400">{error}</div>;
  if (!event) return <div className="text-slate-400">Evento não encontrado</div>;

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <Link href="/dashboard" className="inline-flex items-center gap-2 text-slate-400 hover:text-white transition-colors">
        <ArrowLeft className="w-4 h-4" />
        Voltar
      </Link>

      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 space-y-4">
        <div className="flex items-center gap-3">
          <span
            className="w-4 h-4 rounded-full"
            style={{ backgroundColor: SEVERITY_COLORS[event.severity] }}
          />
          <h2 className="text-xl font-bold text-white">{event.title}</h2>
        </div>

        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <span className="text-slate-500">Tipo</span>
            <div className="text-white">{EVENT_TYPE_LABELS[event.eventType]}</div>
          </div>
          <div>
            <span className="text-slate-500">Severidade</span>
            <div className="text-white">{event.severity}</div>
          </div>
          <div>
            <span className="text-slate-500">Fonte</span>
            <div className="text-white">{event.source}</div>
          </div>
          <div>
            <span className="text-slate-500">Data</span>
            <div className="text-white">
              {format(new Date(event.occurredAt), "d MMMM yyyy HH:mm", { locale: pt })}
            </div>
          </div>
          <div>
            <span className="text-slate-500">Localização</span>
            <div className="text-white">{event.latitude.toFixed(5)}, {event.longitude.toFixed(5)}</div>
          </div>
        </div>

        {event.description && (
          <div>
            <span className="text-slate-500 text-sm">Descrição</span>
            <p className="text-slate-200 mt-1">{event.description}</p>
          </div>
        )}

        {event.aiClassification && (
          <div className="bg-indigo-500/10 border border-indigo-500/30 rounded-lg p-4">
            <div className="text-xs font-semibold text-indigo-400 mb-1">Classificação IA</div>
            <div className="text-sm text-indigo-200">{event.aiClassification}</div>
            {event.aiInsight && <p className="text-xs text-indigo-300 mt-2">{event.aiInsight}</p>}
          </div>
        )}
      </div>
    </div>
  );
}
