export type AlertSeverity = "Info" | "Warning" | "Danger" | "Critical";

export interface Alert {
  id: string;
  title: string;
  severity: AlertSeverity;
  message: string;
  geoEventId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface AlertListResponse {
  items: Alert[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AlertFilters {
  severity?: AlertSeverity;
  isRead?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AlertRule {
  id: string;
  name: string;
  eventType: string | null;
  severityThreshold: string | null;
  areaWkt: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface AlertRuleListResponse {
  items: AlertRule[];
}

export interface CreateAlertRuleRequest {
  name: string;
  eventType?: string;
  severityThreshold?: string;
  areaWkt?: string;
  isActive: boolean;
}

export interface UpdateAlertRuleRequest {
  name?: string;
  eventType?: string;
  severityThreshold?: string;
  areaWkt?: string;
  isActive?: boolean;
}

export interface MarkAlertReadRequest {
  alertIds?: string[];
  markAllRead?: boolean;
}

export interface MarkAlertReadResponse {
  markedCount: number;
}