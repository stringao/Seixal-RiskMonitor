"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft, Plus, Pencil, Trash2, Power } from "lucide-react";
import {
  fetchAlertRules,
  createAlertRule,
  updateAlertRule,
  deleteAlertRule,
  toggleAlertRule,
} from "@/lib/api/alerts";
import { AlertRuleForm } from "@/components/alerts/AlertRuleForm";
import type {
  AlertRule,
  CreateAlertRuleRequest,
  UpdateAlertRuleRequest,
} from "@/lib/types/alert";
import { format } from "date-fns";
import { pt } from "date-fns/locale";

const EVENT_TYPE_LABELS: Record<string, string> = {
  Fire: "Incêndio",
  Flood: "Inundação",
  Storm: "Tempestade",
  Landslide: "Deslizamento",
  Industrial: "Industrial",
  Heatwave: "Onda de calor",
  Other: "Outro",
};

export default function AlertRulesPage() {
  const [rules, setRules] = useState<AlertRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingRule, setEditingRule] = useState<AlertRule | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);

  const loadRules = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetchAlertRules();
      setRules(res.items);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Erro ao carregar regras");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadRules();
  }, []);

  const handleCreate = async (data: CreateAlertRuleRequest) => {
    await createAlertRule(data);
    setShowCreateForm(false);
    await loadRules();
  };

  const handleUpdate = async (data: UpdateAlertRuleRequest) => {
    if (!editingRule) return;
    await updateAlertRule(editingRule.id, data);
    setEditingRule(null);
    await loadRules();
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Eliminar esta regra?")) return;
    await deleteAlertRule(id);
    await loadRules();
  };

  const handleToggle = async (id: string) => {
    await toggleAlertRule(id);
    await loadRules();
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <div className="flex items-center gap-4">
        <Link
          href="/dashboard/alerts"
          className="p-2 text-slate-400 hover:text-white transition-colors"
        >
          <ArrowLeft className="w-5 h-5" />
        </Link>
        <div className="flex-1">
          <h2 className="text-xl font-bold text-white">Regras de Alerta</h2>
          <p className="text-sm text-slate-400 mt-1">
            Configurar regras automáticas para geração de alertas
          </p>
        </div>
        {!showCreateForm && !editingRule && (
          <button
            onClick={() => setShowCreateForm(true)}
            className="inline-flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-medium rounded-lg transition-colors"
          >
            <Plus className="w-4 h-4" />
            Nova regra
          </button>
        )}
      </div>

      {(showCreateForm || editingRule) && (
        <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6">
          <h3 className="text-lg font-semibold text-white mb-4">
            {editingRule ? "Editar regra" : "Criar nova regra"}
          </h3>
          <AlertRuleForm
            rule={editingRule ?? undefined}
            onSubmit={async (data) => {
              if (editingRule) {
                await handleUpdate(data as UpdateAlertRuleRequest);
              } else {
                await handleCreate(data as CreateAlertRuleRequest);
              }
            }}
            onCancel={() => {
              setEditingRule(null);
              setShowCreateForm(false);
            }}
          />
        </div>
      )}

      {error && (
        <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-4 text-sm text-red-400">
          {error}
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-slate-500">A carregar...</div>
      ) : rules.length === 0 ? (
        <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-8 text-center">
          <p className="text-slate-500">Nenhuma regra configurada</p>
          <button
            onClick={() => setShowCreateForm(true)}
            className="mt-4 text-sm text-emerald-400 hover:text-emerald-300 transition-colors"
          >
            Criar primeira regra
          </button>
        </div>
      ) : (
        <div className="bg-slate-800/60 border border-slate-700 rounded-xl divide-y divide-slate-700">
          {rules.map((rule) => (
            <div
              key={rule.id}
              className="p-4 flex items-center gap-4 hover:bg-slate-700/20 transition-colors"
            >
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <h4 className="text-sm font-semibold text-white truncate">
                    {rule.name}
                  </h4>
                  {!rule.isActive && (
                    <span className="text-xs text-slate-500 bg-slate-700 px-2 py-0.5 rounded">
                      Inativa
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-3 mt-1 text-xs text-slate-500">
                  {rule.eventType && (
                    <span>{EVENT_TYPE_LABELS[rule.eventType] ?? rule.eventType}</span>
                  )}
                  {rule.severityThreshold && (
                    <span>≥ {rule.severityThreshold}</span>
                  )}
                  <span>
                    {format(new Date(rule.createdAt), "d MMM yyyy", { locale: pt })}
                  </span>
                </div>
              </div>
              <div className="flex items-center gap-1">
                <button
                  onClick={() => handleToggle(rule.id)}
                  className="p-2 text-slate-400 hover:text-white transition-colors"
                  title={rule.isActive ? "Desativar" : "Ativar"}
                >
                  <Power className="w-4 h-4" />
                </button>
                <button
                  onClick={() => setEditingRule(rule)}
                  className="p-2 text-slate-400 hover:text-white transition-colors"
                  title="Editar"
                >
                  <Pencil className="w-4 h-4" />
                </button>
                <button
                  onClick={() => handleDelete(rule.id)}
                  className="p-2 text-slate-400 hover:text-red-400 transition-colors"
                  title="Eliminar"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}