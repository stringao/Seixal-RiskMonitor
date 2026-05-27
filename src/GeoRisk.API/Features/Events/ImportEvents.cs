using System.Text.Json;
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
    private static readonly JsonSerializerOptions CachedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static RouteGroupBuilder MapImportEvents(this RouteGroupBuilder group)
    {
        group.MapPost("/import", async (HttpContext http,
            ICommandHandler<ImportEventsCommand, ImportEventsResponse> handler) =>
        {
            var items = await JsonSerializer.DeserializeAsync<List<ImportEventItem>>(http.Request.Body, CachedJsonOptions);
            if (items is null) return Results.BadRequest(new { error = "Request body is required" });
            var result = await handler.HandleAsync(new ImportEventsCommand(items), default);
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");
        return group;
    }
}
