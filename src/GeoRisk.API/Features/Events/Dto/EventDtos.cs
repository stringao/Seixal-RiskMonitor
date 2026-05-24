namespace GeoRisk.API.Features.Events.Dto;

public sealed record CreateEventRequest(
    EventType EventType, string Title, string? Description,
    double Latitude, double Longitude, RiskLevel Severity,
    EventSource Source, DateTime OccurredAt, string? Metadata);

public sealed record EventResponse(
    Guid Id, EventType EventType, string Title, string? Description,
    double Latitude, double Longitude, RiskLevel Severity, EventSource Source,
    DateTime OccurredAt, string? AIClassification, string? AIInsight, DateTime CreatedAt);

public sealed record EventListResponse(
    List<EventResponse> Items, int TotalCount, int Page, int PageSize);

public sealed record ImportEventItem(
    string SourceId, EventType EventType, string Title,
    double Latitude, double Longitude, RiskLevel Severity,
    EventSource Source, DateTime OccurredAt, string? Description);

public sealed record ImportEventsResponse(int Imported, int Skipped);
