namespace GeoRisk.API.Features.Alerts.Dto;

public sealed record AlertResponse(
    Guid Id,
    string Title,
    AlertSeverity Severity,
    string Message,
    Guid? GeoEventId,
    bool IsRead,
    DateTime CreatedAt);

public sealed record AlertListResponse(
    List<AlertResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CreateAlertRuleRequest(
    string Name,
    EventType? EventType,
    RiskLevel? SeverityThreshold,
    string? AreaWkt,
    bool IsActive);

public sealed record UpdateAlertRuleRequest(
    string? Name,
    EventType? EventType,
    RiskLevel? SeverityThreshold,
    string? AreaWkt,
    bool? IsActive);

public sealed record AlertRuleResponse(
    Guid Id,
    string Name,
    EventType? EventType,
    RiskLevel? SeverityThreshold,
    string? AreaWkt,
    bool IsActive,
    DateTime CreatedAt);

public sealed record AlertRuleListResponse(
    List<AlertRuleResponse> Items);

public sealed record MarkAlertReadRequest(
    List<Guid>? AlertIds,
    bool? MarkAllRead);

public sealed record MarkAlertReadResponse(int MarkedCount);