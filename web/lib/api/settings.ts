import apiClient from "@/lib/api-client";

export interface AiProviderConfig {
  id: number;
  provider: string;
  apiKey: string;
  model: string;
  baseUrl?: string;
  maxTokens: number;
  isEnabled: boolean;
}

export interface AppSettings {
  activeProvider: string;
  providers: AiProviderConfig[];
}

export interface UserProfile {
  id: string;
  email: string;
  role: string;
}

export interface UpdateProfileRequest {
  email?: string;
  currentPassword?: string;
  newPassword?: string;
}

export async function getSettings(): Promise<AppSettings> {
  const { data } = await apiClient.get<AppSettings>("/settings");
  return data;
}

export async function updateSettings(settings: AppSettings): Promise<AppSettings> {
  const { data } = await apiClient.put<AppSettings>("/settings", settings);
  return data;
}

export async function getProfile(): Promise<UserProfile> {
  const { data } = await apiClient.get<UserProfile>("/me");
  return data;
}

export async function updateProfile(request: UpdateProfileRequest): Promise<UserProfile> {
  const { data } = await apiClient.put<UserProfile>("/auth/profile", request);
  return data;
}

export const PROVIDER_LABELS: Record<string, string> = {
  DeepSeek: "DeepSeek (Chat)",
  Ollama: "Ollama (Local)",
  Qwen: "Qwen (Alibaba)",
  Anthropic: "Anthropic (Claude)",
  OpenAI: "OpenAI (GPT-4o)",
};

export const PROVIDER_DESCRIPTIONS: Record<string, string> = {
  DeepSeek: "API key em platform.deepseek.com",
  Ollama: "Modelo local - Sem API key necessária",
  Qwen: "API key em dashscope.aliyuncs.com",
  Anthropic: "API key da Anthropic",
  OpenAI: "API key da OpenAI",
};

export const PROVIDER_FIELDS: Record<string, { hasApiKey: boolean; hasBaseUrl: boolean; hasModel: boolean }> = {
  DeepSeek: { hasApiKey: true, hasBaseUrl: false, hasModel: true },
  Ollama: { hasApiKey: false, hasBaseUrl: true, hasModel: true },
  Qwen: { hasApiKey: true, hasBaseUrl: false, hasModel: true },
  Anthropic: { hasApiKey: true, hasBaseUrl: false, hasModel: true },
  OpenAI: { hasApiKey: true, hasBaseUrl: false, hasModel: true },
};