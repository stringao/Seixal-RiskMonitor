using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;

namespace GeoRisk.API.Features.Events;

public sealed record GetEventByIdQuery(Guid Id) : IQuery<EventResponse?>;

public sealed class GetEventByIdHandler(GeoRiskDbContext db) : IQueryHandler<GetEventByIdQuery, EventResponse?>
{
    public async Task<EventResponse?> HandleAsync(GetEventByIdQuery query, CancellationToken ct)
    {
        var e = await db.GeoEvents.AsNoTracking().FirstOrDefaultAsync(ev => ev.Id == query.Id, ct);
        if (e is null) return null;
        return new EventResponse(e.Id, e.EventType, e.Title, e.Description,
            e.Geometry.Y, e.Geometry.X, e.Severity, e.Source,
            e.OccurredAt, e.AIClassification, e.AIInsight, e.CreatedAt);
    }
}

public static class GetEventByIdEndpoint
{
    public static RouteGroupBuilder MapGetEventById(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id,
            IQueryHandler<GetEventByIdQuery, EventResponse?> handler) =>
        {
            var result = await handler.HandleAsync(new GetEventByIdQuery(id), default);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();
        return group;
    }
}
