"use client";

import { useState, useEffect } from "react";
import { Save, Loader2, User } from "lucide-react";
import { getSettings, updateSettings, getProfile, updateProfile, AppSettings, UserProfile } from "@/lib/api/settings";

export default function SettingsPage() {
  const [settings, setSettings] = useState<AppSettings>({
    llmProvider: "OpenAI",
    apiKey: "",
    modelName: "gpt-4o",
    maxTokens: 1024,
  });
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [savingProfile, setSavingProfile] = useState(false);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [profileMessage, setProfileMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const [email, setEmail] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  useEffect(() => {
    Promise.all([getSettings(), getProfile()])
      .then(([settingsData, profileData]) => {
        setSettings(settingsData);
        setProfile(profileData);
        setEmail(profileData.email);
      })
      .catch(() => {
        setSettings({
          llmProvider: "OpenAI",
          apiKey: "",
          modelName: "gpt-4o",
          maxTokens: 1024,
        });
      })
      .finally(() => setLoading(false));
  }, []);

  const handleSettingsSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMessage(null);

    try {
      await updateSettings(settings);
      setMessage({ type: "success", text: "Definições guardadas com sucesso!" });
    } catch {
      setMessage({ type: "error", text: "Erro ao guardar definições" });
    } finally {
      setSaving(false);
    }
  };

  const handleProfileSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavingProfile(true);
    setProfileMessage(null);

    try {
      const result = await updateProfile({
        email: email !== profile?.email ? email : undefined,
        currentPassword: newPassword ? currentPassword : undefined,
        newPassword: newPassword || undefined,
      });
      setProfile(result);
      setProfileMessage({ type: "success", text: "Perfil atualizado com sucesso!" });
      setCurrentPassword("");
      setNewPassword("");
    } catch {
      setProfileMessage({ type: "error", text: "Erro ao atualizar perfil" });
    } finally {
      setSavingProfile(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-emerald-400" />
      </div>
    );
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Definições</h1>
        <p className="text-sm text-slate-400 mt-1">Configurar integrações e preferências da aplicação</p>
      </div>

      {message && (
        <div
          className={`p-4 rounded-lg text-sm ${
            message.type === "success"
              ? "bg-emerald-500/20 text-emerald-400 border border-emerald-500/30"
              : "bg-red-500/20 text-red-400 border border-red-500/30"
          }`}
        >
          {message.text}
        </div>
      )}

      <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 space-y-4">
        <div className="flex items-center gap-3">
          <User className="w-5 h-5 text-emerald-400" />
          <h2 className="text-lg font-semibold text-white">Perfil</h2>
        </div>

        <form onSubmit={handleProfileSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              Email
            </label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
            />
          </div>

          <div className="pt-2 border-t border-slate-700">
            <p className="text-sm text-slate-400 mb-3">Alterar Palavra-passe</p>
            <div className="space-y-3">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Palavra-passe Atual
                </label>
                <input
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Nova Palavra-passe
                </label>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
                />
              </div>
            </div>
          </div>

          {profileMessage && (
            <div
              className={`p-3 rounded-lg text-sm ${
                profileMessage.type === "success"
                  ? "bg-emerald-500/20 text-emerald-400 border border-emerald-500/30"
                  : "bg-red-500/20 text-red-400 border border-red-500/30"
              }`}
            >
              {profileMessage.text}
            </div>
          )}

          <button
            type="submit"
            disabled={savingProfile}
            className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors"
          >
            {savingProfile ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Save className="w-4 h-4" />
            )}
            {savingProfile ? "A guardar..." : "Guardar Perfil"}
          </button>
        </form>
      </div>

      <form onSubmit={handleSettingsSubmit} className="bg-slate-800/60 border border-slate-700 rounded-xl p-6 space-y-6">
        <div>
          <h2 className="text-lg font-semibold text-white mb-4">Configuração da API de IA</h2>
          <p className="text-sm text-slate-400 mb-6">
            Para ativar as funcionalidades de IA (deteção de padrões, classificação automática e geração de relatórios),
            configure a API key do seu fornecedor preferido.
          </p>
        </div>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              Fornecedor de IA
            </label>
            <select
              value={settings.llmProvider}
              onChange={(e) => setSettings({ ...settings, llmProvider: e.target.value })}
              className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
            >
              <option value="OpenAI">OpenAI (GPT-4o)</option>
              <option value="Anthropic">Anthropic (Claude)</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              API Key
            </label>
            <input
              type="password"
              value={settings.apiKey}
              onChange={(e) => setSettings({ ...settings, apiKey: e.target.value })}
              placeholder="sk-..."
              className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500 placeholder:text-slate-600"
            />
            <p className="text-xs text-slate-500 mt-1.5">
              A chave será guardada de forma segura no servidor
            </p>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">
                Modelo
              </label>
              <input
                type="text"
                value={settings.modelName}
                onChange={(e) => setSettings({ ...settings, modelName: e.target.value })}
                placeholder="gpt-4o"
                className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500 placeholder:text-slate-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">
                Max Tokens
              </label>
              <input
                type="number"
                value={settings.maxTokens}
                onChange={(e) => setSettings({ ...settings, maxTokens: parseInt(e.target.value) || 1024 })}
                min={256}
                max={8192}
                className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
              />
            </div>
          </div>
        </div>

        <div className="pt-4 border-t border-slate-700">
          <button
            type="submit"
            disabled={saving}
            className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors"
          >
            {saving ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Save className="w-4 h-4" />
            )}
            {saving ? "A guardar..." : "Guardar Definições"}
          </button>
        </div>
      </form>

      <div className="bg-slate-800/40 border border-slate-700/50 rounded-xl p-4">
        <h3 className="text-sm font-medium text-slate-300 mb-2">Funcionalidades dependentes de IA</h3>
        <ul className="space-y-1.5 text-sm text-slate-400">
          <li>• Deteção de padrões em eventos</li>
          <li>• Classificação automática de incidentes</li>
          <li>• Geração de relatórios inteligentes</li>
          <li>• Análise de riscos com IA</li>
        </ul>
        <p className="text-xs text-slate-500 mt-3">
          Sem API key configurada, estas funcionalidades mostrarão uma mensagem informativa.
        </p>
      </div>
    </div>
  );
}
