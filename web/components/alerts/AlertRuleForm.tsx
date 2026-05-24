"use client";

import { useState } from "react";
import type {
  AlertRule,
  CreateAlertRuleRequest,
  UpdateAlertRuleRequest,
} from "@/lib/types/alert";

interface AlertRuleFormProps {
  rule?: AlertRule;
  onSubmit: (data: CreateAlertRuleRequest | UpdateAlertRuleRequest) => Promise<void>;
  onCancel?: () => void;
}

const EVENT_TYPES = [
  { value: "", label: "Qualquer tipo" },
  { value: "Fire", label: "Incêndio" },
  { value: "Flood", label: "Inundação" },
  { value: "Storm", label: "Tempestade" },
  { value: "Landslide", label: "Deslizamento" },
  { value: "Industrial", label: "Industrial" },
  { value: "Heatwave", label: "Onda de calor" },
  { value: "Other", label: "Outro" },
];

const SEVERITY_LEVELS = [
  { value: "", label: "Qualquer severidade" },
  { value: "Low", label: "Baixa" },
  { value: "Medium", label: "Média" },
  { value: "High", label: "Alta" },
  { value: "Critical", label: "Crítica" },
];

export function AlertRuleForm({ rule, onSubmit, onCancel }: AlertRuleFormProps) {
  const [name, setName] = useState(rule?.name ?? "");
  const [eventType, setEventType] = useState(rule?.eventType ?? "");
  const [severityThreshold, setSeverityThreshold] = useState(rule?.severityThreshold ?? "");
  const [isActive, setIsActive] = useState(rule?.isActive ?? true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      setError("Nome é obrigatório");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      if (rule) {
        const updateData: UpdateAlertRuleRequest = {
          name: name.trim(),
          eventType: eventType || undefined,
          severityThreshold: severityThreshold || undefined,
          isActive,
        };
        await onSubmit(updateData);
      } else {
        const createData: CreateAlertRuleRequest = {
          name: name.trim(),
          eventType: eventType || undefined,
          severityThreshold: severityThreshold || undefined,
          isActive,
        };
        await onSubmit(createData);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Erro ao guardar regra");
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && (
        <div className="bg-red-500/10 border border-red-500/30 rounded-lg p-3 text-sm text-red-400">
          {error}
        </div>
      )}

      <div>
        <label className="block text-sm text-slate-400 mb-1">Nome da regra</label>
        <input
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-emerald-500"
          placeholder="Ex: Alerta Incêndios"
        />
      </div>

      <div>
        <label className="block text-sm text-slate-400 mb-1">Tipo de evento</label>
        <select
          value={eventType}
          onChange={(e) => setEventType(e.target.value)}
          className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-emerald-500"
        >
          {EVENT_TYPES.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label className="block text-sm text-slate-400 mb-1">Limiar de severidade</label>
        <select
          value={severityThreshold}
          onChange={(e) => setSeverityThreshold(e.target.value)}
          className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-emerald-500"
        >
          {SEVERITY_LEVELS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      <div className="flex items-center gap-2">
        <input
          type="checkbox"
          id="isActive"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
          className="rounded bg-slate-800 border-slate-700 text-emerald-500 focus:ring-emerald-500"
        />
        <label htmlFor="isActive" className="text-sm text-slate-300">
          Regra ativa
        </label>
      </div>

      <div className="flex items-center gap-3 pt-2">
        <button
          type="submit"
          disabled={loading}
          className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors"
        >
          {loading ? "A guardar..." : rule ? "Atualizar" : "Criar regra"}
        </button>
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="px-4 py-2 text-slate-400 hover:text-white text-sm transition-colors"
          >
            Cancelar
          </button>
        )}
      </div>
    </form>
  );
}