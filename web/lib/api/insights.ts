import apiClient from "@/lib/api-client";

export interface ClassifyIncidentResult {
  eventId: string;
  classification: string;
  confidence: number;
  reasoning: string;
  relatedRisks: string[];
}

export async function classifyIncident(eventId: string): Promise<ClassifyIncidentResult> {
  const { data } = await apiClient.post<ClassifyIncidentResult>(
    `/insights/classify/${eventId}`,
    {}
  );
  return data;
}

export async function generateReport(from: string, to: string): Promise<string> {
  const response = await apiClient.get<string>(`/insights/report?from=${from}&to=${to}`);
  return response.data;
}

export interface DetectedPattern {
  patternType: string;
  title: string;
  description: string;
  confidence: number;
  affectedEventIds: string[];
  recommendation: string;
}

export interface DetectPatternsResult {
  patterns: DetectedPattern[];
}

export async function detectPatterns(): Promise<DetectPatternsResult> {
  const { data } = await apiClient.get<DetectPatternsResult>("/insights/patterns");
  return data;
}

// Event Chain Analysis types

export interface EventChain {
  id: string;
  analysisType: string;
  primaryEventId: string | null;
  description: string;
  confidenceScore: number;
  findings: Record<string, unknown> | null;
  contributingFactors: string[];
  recommendedAction: string | null;
  analyzedAt: string;
}

export interface ChainAnalysesResult {
  chains: EventChain[];
}

export async function getChainAnalyses(params?: {
  type?: string;
  from?: string;
  to?: string;
  minConfidence?: number;
}): Promise<ChainAnalysesResult> {
  const searchParams = new URLSearchParams();
  if (params?.type) searchParams.set("type", params.type);
  if (params?.from) searchParams.set("from", params.from);
  if (params?.to) searchParams.set("to", params.to);
  if (params?.minConfidence) searchParams.set("minConfidence", params.minConfidence.toString());

  const queryString = searchParams.toString();
  const url = queryString ? `/insights/chains?${queryString}` : "/insights/chains";

  const { data } = await apiClient.get<ChainAnalysesResult>(url);
  return data;
}

export async function getActiveChains(): Promise<ChainAnalysesResult> {
  const { data } = await apiClient.get<ChainAnalysesResult>("/insights/chains/current");
  return data;
}

export async function analyzeChains(eventIds: string[]): Promise<EventChain> {
  const { data } = await apiClient.post<EventChain>("/insights/chains/analyze", { eventIds });
  return data;
}

export interface ChainTypeInfo {
  type: string;
  description: string;
}

export async function getChainTypes(): Promise<ChainTypeInfo[]> {
  const { data } = await apiClient.get<ChainTypeInfo[]>("/insights/chains/types");
  return data;
}