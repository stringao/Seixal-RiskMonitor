using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Events;

public sealed record CreateEventCommand(
    EventType EventType, string Title, string? Description,
    double Latitude, double Longitude, RiskLevel Severity,
    EventSource Source, DateTime OccurredAt, string? Metadata) : ICommand<EventResponse>;

public sealed class CreateEventHandler(
    GeoRiskDbContext db, ICacheService cache) : ICommandHandler<CreateEventCommand, EventResponse>
{
    public async Task<EventResponse> HandleAsync(CreateEventCommand cmd, CancellationToken ct)
    {
        var geoEvent = new GeoEvent
        {
            Id = Guid.NewGuid(), EventType = cmd.EventType, Title = cmd.Title,
            Description = cmd.Description,
            Geometry = new Point(cmd.Longitude, cmd.Latitude) { SRID = 4326 },
            Severity = cmd.Severity, Source = cmd.Source,
            OccurredAt = cmd.OccurredAt, Metadata = cmd.Metadata
        };
        db.GeoEvents.Add(geoEvent);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPrefixAsync("events:", ct);
        return new EventResponse(geoEvent.Id, geoEvent.EventType, geoEvent.Title, geoEvent.Description,
            geoEvent.Geometry.Y, geoEvent.Geometry.X, geoEvent.Severity, geoEvent.Source,
            geoEvent.OccurredAt, geoEvent.AIClassification, geoEvent.AIInsight, geoEvent.CreatedAt);
    }
}

public static class CreateEventEndpoint
{
    public static RouteGroupBuilder MapCreateEvent(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateEventCommand cmd,
            ICommandHandler<CreateEventCommand, EventResponse> handler) =>
        {
            var result = await handler.HandleAsync(cmd, default);
            return Results.Created($"/api/events/{result.Id}", result);
        }).RequireAuthorization("AnalystOrAdmin");
        return group;
    }
}
