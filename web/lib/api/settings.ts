import apiClient from "@/lib/api-client";

export interface AppSettings {
  llmProvider: string;
  apiKey: string;
  modelName: string;
  maxTokens: number;
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
