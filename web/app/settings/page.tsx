"use client";

import { useState, useEffect } from "react";
import { Save, Loader2, User, ChevronDown, ChevronUp, Check } from "lucide-react";
import { getSettings, updateSettings, getProfile, updateProfile, PROVIDER_LABELS, PROVIDER_DESCRIPTIONS, PROVIDER_FIELDS, AppSettings as AppSettingsType, AiProviderConfig, UserProfile } from "@/lib/api/settings";

export default function SettingsPage() {
  const [settings, setSettings] = useState<AppSettingsType>({
    activeProvider: "DeepSeek",
    providers: [],
  });
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [savingProfile, setSavingProfile] = useState(false);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [profileMessage, setProfileMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [expandedProvider, setExpandedProvider] = useState<string | null>(null);

  const [email, setEmail] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  useEffect(() => {
    Promise.all([getSettings(), getProfile()])
      .then(([settingsData, profileData]) => {
        setSettings(settingsData);
        setProfile(profileData);
        setEmail(profileData.email);
        setExpandedProvider(settingsData.activeProvider);
      })
      .catch(() => {
        setSettings({
          activeProvider: "DeepSeek",
          providers: [],
        });
      })
      .finally(() => setLoading(false));
  }, []);

  const handleProviderChange = (provider: string) => {
    setSettings((prev) => ({
      ...prev,
      activeProvider: provider,
    }));
  };

  const handleProviderConfigChange = (provider: string, field: keyof AiProviderConfig, value: string | number | boolean) => {
    setSettings((prev) => ({
      ...prev,
      providers: prev.providers.map((p) =>
        p.provider === provider ? { ...p, [field]: value } : p
      ),
    }));
  };

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

  const toggleProvider = (provider: string) => {
    setExpandedProvider(expandedProvider === provider ? null : provider);
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
            Configure todos os fornecedores de IA. Selecione qual está ativo para uso na aplicação.
          </p>
        </div>

        <div className="space-y-3">
          {settings.providers.map((provider) => {
            const fields = PROVIDER_FIELDS[provider.provider] || { hasApiKey: true, hasBaseUrl: false, hasModel: true };
            const isActive = settings.activeProvider === provider.provider;
            const isExpanded = expandedProvider === provider.provider;

            return (
              <div
                key={provider.provider}
                className={`border rounded-lg transition-colors ${
                  isActive ? "border-emerald-500/50 bg-emerald-500/5" : "border-slate-700 bg-slate-900/50"
                }`}
              >
                <div
                  className="flex items-center justify-between p-4 cursor-pointer"
                  onClick={() => toggleProvider(provider.provider)}
                >
                  <div className="flex items-center gap-3">
                    <div
                      className={`w-5 h-5 rounded-full border-2 flex items-center justify-center ${
                        isActive ? "border-emerald-500 bg-emerald-500" : "border-slate-600"
                      }`}
                    >
                      {isActive && <Check className="w-3 h-3 text-white" />}
                    </div>
                    <div>
                      <p className="text-white font-medium">{PROVIDER_LABELS[provider.provider] || provider.provider}</p>
                      <p className="text-xs text-slate-400">{PROVIDER_DESCRIPTIONS[provider.provider] || ""}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {isActive && (
                      <span className="text-xs bg-emerald-500/20 text-emerald-400 px-2 py-1 rounded">Ativo</span>
                    )}
                    {isExpanded ? (
                      <ChevronUp className="w-4 h-4 text-slate-400" />
                    ) : (
                      <ChevronDown className="w-4 h-4 text-slate-400" />
                    )}
                  </div>
                </div>

                {isExpanded && (
                  <div className="px-4 pb-4 space-y-4 border-t border-slate-700 pt-4">
                    <div className="flex items-center justify-between">
                      <label className="flex items-center gap-2 cursor-pointer">
                        <input
                          type="radio"
                          name="activeProvider"
                          value={provider.provider}
                          checked={isActive}
                          onChange={() => handleProviderChange(provider.provider)}
                          className="w-4 h-4 border-slate-600 bg-slate-700 text-emerald-500 focus:ring-emerald-500 focus:ring-offset-0"
                        />
                        <span className="text-sm text-slate-300">Usar este fornecedor</span>
                      </label>
                    </div>

                    {fields.hasApiKey && (
                      <div>
                        <label className="block text-sm font-medium text-slate-300 mb-2">
                          API Key
                        </label>
                        <input
                          type="password"
                          value={provider.apiKey}
                          onChange={(e) => handleProviderConfigChange(provider.provider, "apiKey", e.target.value)}
                          placeholder={provider.provider === "Ollama" ? "Não necessário" : "sk-..."}
                          className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500 placeholder:text-slate-600"
                        />
                      </div>
                    )}

                    {fields.hasBaseUrl && (
                      <div>
                        <label className="block text-sm font-medium text-slate-300 mb-2">
                          URL Base
                        </label>
                        <input
                          type="text"
                          value={provider.baseUrl || ""}
                          onChange={(e) => handleProviderConfigChange(provider.provider, "baseUrl", e.target.value)}
                          placeholder="http://localhost:11434/v1"
                          className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500 placeholder:text-slate-600"
                        />
                      </div>
                    )}

                    {fields.hasModel && (
                      <div>
                        <label className="block text-sm font-medium text-slate-300 mb-2">
                          Modelo
                        </label>
                        <input
                          type="text"
                          value={provider.model}
                          onChange={(e) => handleProviderConfigChange(provider.provider, "model", e.target.value)}
                          className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
                        />
                      </div>
                    )}

                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-slate-300 mb-2">
                          Max Tokens
                        </label>
                        <input
                          type="number"
                          value={provider.maxTokens}
                          onChange={(e) => handleProviderConfigChange(provider.provider, "maxTokens", parseInt(e.target.value) || 1024)}
                          min={256}
                          max={8192}
                          className="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2.5 text-white text-sm focus:outline-none focus:border-emerald-500"
                        />
                      </div>
                    </div>

                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={provider.isEnabled}
                        onChange={(e) => handleProviderConfigChange(provider.provider, "isEnabled", e.target.checked)}
                        className="w-4 h-4 rounded border-slate-600 bg-slate-700 text-emerald-500 focus:ring-emerald-500 focus:ring-offset-0"
                      />
                      <span className="text-sm text-slate-300">Ativado</span>
                    </label>
                  </div>
                )}
              </div>
            );
          })}
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