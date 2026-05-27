using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Events;

public sealed record GetEventsQuery(
    int Page, int PageSize, EventType? Type, RiskLevel? Severity,
    DateTime? From, DateTime? To, string? Bbox,
    double? Lat, double? Lng, double? Radius,
    string? CacheKey, bool? InsideSeixal) : IQuery<EventListResponse>;

public sealed class GetEventsHandler(
    GeoRiskDbContext db, ICacheService cache) : IQueryHandler<GetEventsQuery, EventListResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<EventListResponse> HandleAsync(GetEventsQuery query, CancellationToken ct)
    {
        if (query.CacheKey is not null)
        {
            var cached = await cache.GetAsync<EventListResponse>(query.CacheKey, ct);
            if (cached is not null) return cached;
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = db.GeoEvents.AsNoTracking().AsQueryable();

        if (query.Type is not null) q = q.Where(e => e.EventType == query.Type);
        if (query.Severity is not null) q = q.Where(e => e.Severity == query.Severity);
        if (query.From is not null)
        {
            var fromUtc = DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc);
            q = q.Where(e => e.OccurredAt >= fromUtc);
        }
        if (query.To is not null)
        {
            var toUtc = DateTime.SpecifyKind(query.To.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            q = q.Where(e => e.OccurredAt <= toUtc);
        }

        if (query.Bbox is not null)
        {
            var coords = query.Bbox.Split(',');
            if (coords.Length == 4
                && double.TryParse(coords[0], out var x1) && double.TryParse(coords[1], out var y1)
                && double.TryParse(coords[2], out var x2) && double.TryParse(coords[3], out var y2))
            {
                var envelope = new Envelope(x1, x2, y1, y2);
                q = q.Where(e => envelope.Contains(e.Geometry.Coordinate));
            }
        }

        if (query.Lat is not null && query.Lng is not null)
        {
            var center = new Point(query.Lng.Value, query.Lat.Value) { SRID = 4326 };
            q = q.Where(e =>
                Math.Sqrt(Math.Pow(e.Geometry.X - center.X, 2) + Math.Pow(e.Geometry.Y - center.Y, 2))
                <= (query.Radius ?? 5000) * 0.00001);
        }

        var totalCount = await q.CountAsync(ct);
        var entities = await q.OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var items = entities.Select(e => new EventResponse(e.Id, Enum.GetName(e.EventType)!, e.Title, e.Description,
            e.Geometry.Y, e.Geometry.X, Enum.GetName(e.Severity)!, Enum.GetName(e.Source)!,
            e.OccurredAt, e.AIClassification, e.AIInsight, e.CreatedAt))
            .ToList();

        var result = new EventListResponse(items, totalCount, page, pageSize);
        if (query.CacheKey is not null)
            await cache.SetAsync(query.CacheKey, result, CacheTtl, ct);
        return result;
    }
}

public static class GetEventsEndpoint
{
    public static RouteGroupBuilder MapGetEvents(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetEventsHandler).RequireAuthorization();
        return group;
    }

    #pragma warning disable S107
    private static async Task<IResult> GetEventsHandler(
        int? page, int? pageSize, EventType? type, RiskLevel? severity,
        DateTime? from, DateTime? to, string? bbox, double? lat, double? lng, double? radius,
        bool? insideSeixal, IQueryHandler<GetEventsQuery, EventListResponse> handler)
    {
        var p = page ?? 1; var ps = pageSize ?? 20;
        var cacheKey = $"events:{type}:{severity}:{from}:{to}:{bbox}:{lat}:{lng}:{radius}:{insideSeixal}:{p}:{ps}";
        var result = await handler.HandleAsync(new GetEventsQuery(p, ps, type, severity, from, to, bbox, lat, lng, radius, cacheKey, insideSeixal), default);
        return Results.Ok(result);
    }
    #pragma warning restore S107
}
