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

export interface GenerateReportResult {
  report: string;
}

export async function generateReport(from: string, to: string): Promise<string> {
  const { data } = await apiClient.get<string>(`/insights/report?from=${from}&to=${to}`);
  return data;
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