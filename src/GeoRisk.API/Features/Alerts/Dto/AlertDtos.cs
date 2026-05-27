namespace GeoRisk.API.Features.Alerts.Dto;

public sealed record AlertResponse(
    Guid Id,
    string Title,
    AlertSeverity Severity,
    string Message,
    Guid? GeoEventId,
    bool IsRead,
    DateTime CreatedAt,
    Guid? AlertRuleId,
    string? AlertRuleName,
    double? FwiValue,
    double? WindSpeed,
    double? Temperature,
    double? AreaKm2,
    bool IsEscalated,
    Guid? EscalatedFromAlertId,
    DateTime? ExpiresAt);

public sealed record AlertListResponse(
    List<AlertResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ActiveAlertsResponse(
    List<AlertResponse> Items,
    int TotalCount,
    AlertSummaryBySeverity Summary);

public sealed record AlertSummaryBySeverity(
    int Info,
    int Warning,
    int Danger,
    int Critical,
    AlertTrend Trend);

public sealed record AlertTrend(
    int YesterdayTotal,
    int TodayTotal,
    string Direction);

public sealed record CreateAlertRuleRequest(
    string Name,
    string? EventType,
    string? SeverityThreshold,
    string? AreaWkt,
    bool IsActive,
    double? MinFwi,
    double? MaxFwi,
    double? MinWindSpeed,
    double? MinTemperature,
    int? SeasonStartMonth,
    int? SeasonEndMonth,
    double? AreaKm2Threshold,
    int? ConsecutiveCount,
    int? EscalationMinutes,
    List<string>? NotifyRoles);

public sealed record UpdateAlertRuleRequest(
    string? Name,
    string? EventType,
    string? SeverityThreshold,
    string? AreaWkt,
    bool? IsActive,
    double? MinFwi,
    double? MaxFwi,
    double? MinWindSpeed,
    double? MinTemperature,
    int? SeasonStartMonth,
    int? SeasonEndMonth,
    double? AreaKm2Threshold,
    int? ConsecutiveCount,
    int? EscalationMinutes,
    List<string>? NotifyRoles);

public sealed record AlertRuleResponse(
    Guid Id,
    string Name,
    string? EventType,
    string? SeverityThreshold,
    string? AreaWkt,
    bool IsActive,
    DateTime CreatedAt,
    double? MinFwi,
    double? MaxFwi,
    double? MinWindSpeed,
    double? MinTemperature,
    int? SeasonStartMonth,
    int? SeasonEndMonth,
    double? AreaKm2Threshold,
    int? ConsecutiveCount,
    int? EscalationMinutes,
    List<string>? NotifyRoles);

public sealed record AlertRuleListResponse(
    List<AlertRuleResponse> Items);

public sealed record RuleConditionsResponse(
    List<RuleConditionInfo> Conditions,
    CurrentConditionValues CurrentValues);

public sealed record RuleConditionInfo(
    string Name,
    string Description,
    string Type,
    string Unit,
    bool IsNumeric,
    double? Min,
    double? Max);

public sealed record CurrentConditionValues(
    double? CurrentFwi,
    double? CurrentWindSpeed,
    double? CurrentTemperature,
    double? CurrentFireAreaKm2,
    string CurrentSeason);

public sealed record TestRuleResponse(
    Guid RuleId,
    bool WouldTrigger,
    int MatchingEventsCount,
    List<TestRuleMatch> Matches);

public sealed record TestRuleMatch(
    Guid EventId,
    string EventTitle,
    string EventType,
    DateTime OccurredAt,
    GeoLocation Location,
    double? Fwi,
    double? WindSpeed,
    double? Temperature,
    double? AreaKm2);

public sealed record GeoLocation(
    double Latitude,
    double Longitude);

public sealed record MarkAlertReadRequest(
    List<Guid>? AlertIds,
    bool? MarkAllRead);

public sealed record MarkAlertReadResponse(int MarkedCount);
