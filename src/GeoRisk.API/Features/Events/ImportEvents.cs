using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Events;

public sealed record ImportEventsCommand(List<ImportEventItem> Items) : ICommand<ImportEventsResponse>;

public sealed class ImportEventsHandler(
    GeoRiskDbContext db, ICacheService cache) : ICommandHandler<ImportEventsCommand, ImportEventsResponse>
{
    public async Task<ImportEventsResponse> HandleAsync(ImportEventsCommand cmd, CancellationToken ct)
    {
        var existingSourceIds = await db.GeoEvents
            .Where(e => e.SourceId != null).Select(e => e.SourceId!).ToHashSetAsync(ct);
        var imported = 0; var skipped = 0;
        foreach (var item in cmd.Items)
        {
            if (existingSourceIds.Contains(item.SourceId)) { skipped++; continue; }
            db.GeoEvents.Add(new GeoEvent
            {
                Id = Guid.NewGuid(), EventType = item.EventType, Title = item.Title,
                Description = item.Description,
                Geometry = new Point(item.Longitude, item.Latitude) { SRID = 4326 },
                Severity = item.Severity, Source = item.Source,
                SourceId = item.SourceId, OccurredAt = item.OccurredAt
            });
            existingSourceIds.Add(item.SourceId); imported++;
        }
        if (imported > 0) { await db.SaveChangesAsync(ct); await cache.RemoveByPrefixAsync("events:", ct); }
        return new ImportEventsResponse(imported, skipped);
    }
}

public static class ImportEventsEndpoint
{
    public static RouteGroupBuilder MapImportEvents(this RouteGroupBuilder group)
    {
        group.MapPost("/import", async (List<ImportEventItem> items,
            ICommandHandler<ImportEventsCommand, ImportEventsResponse> handler) =>
        {
            var result = await handler.HandleAsync(new ImportEventsCommand(items), default);
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");
        return group;
    }
}
