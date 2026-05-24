import apiClient from "@/lib/api-client";
import type {
  Alert,
  AlertFilters,
  AlertListResponse,
  AlertRule,
  AlertRuleListResponse,
  CreateAlertRuleRequest,
  UpdateAlertRuleRequest,
  MarkAlertReadRequest,
  MarkAlertReadResponse,
} from "@/lib/types/alert";

export async function fetchAlerts(
  filters: AlertFilters = {}
): Promise<AlertListResponse> {
  const params = new URLSearchParams();
  if (filters.severity) params.set("severity", filters.severity);
  if (filters.isRead !== undefined) params.set("isRead", String(filters.isRead));
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));

  const { data } = await apiClient.get<AlertListResponse>(
    `/alerts?${params.toString()}`
  );
  return data;
}

export async function fetchAlertRules(): Promise<AlertRuleListResponse> {
  const { data } = await apiClient.get<AlertRuleListResponse>("/alerts/rules");
  return data;
}

export async function createAlertRule(
  request: CreateAlertRuleRequest
): Promise<AlertRule> {
  const { data } = await apiClient.post<AlertRule>("/alerts", {
    Action: "Create",
    RuleId: null,
    CreateRequest: request,
    UpdateRequest: null,
  });
  return data;
}

export async function updateAlertRule(
  ruleId: string,
  request: UpdateAlertRuleRequest
): Promise<AlertRule> {
  const { data } = await apiClient.post<AlertRule>("/alerts", {
    Action: "Update",
    RuleId: ruleId,
    CreateRequest: null,
    UpdateRequest: request,
  });
  return data;
}

export async function deleteAlertRule(ruleId: string): Promise<void> {
  await apiClient.delete(`/alerts/${ruleId}`);
}

export async function toggleAlertRule(ruleId: string): Promise<AlertRule> {
  const { data } = await apiClient.patch<AlertRule>(`/alerts/${ruleId}/toggle`);
  return data;
}

export async function markAlertsRead(
  request: MarkAlertReadRequest
): Promise<MarkAlertReadResponse> {
  const { data } = await apiClient.post<MarkAlertReadResponse>(
    "/alerts/read",
    request
  );
  return data;
}