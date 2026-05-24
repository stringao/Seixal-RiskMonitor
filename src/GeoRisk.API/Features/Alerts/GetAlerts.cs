using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Alerts;

public sealed record GetAlertsQuery(
    int Page,
    int PageSize,
    AlertSeverity? Severity,
    bool? IsRead,
    string? CacheKey) : IQuery<AlertListResponse>;

public sealed class GetAlertsHandler(
    GeoRiskDbContext db,
    ICacheService cache) : IQueryHandler<GetAlertsQuery, AlertListResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public async Task<AlertListResponse> HandleAsync(GetAlertsQuery query, CancellationToken ct)
    {
        if (query.CacheKey is not null)
        {
            var cached = await cache.GetAsync<AlertListResponse>(query.CacheKey, ct);
            if (cached is not null) return cached;
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = db.Alerts.AsNoTracking().AsQueryable();

        if (query.Severity is not null)
            q = q.Where(a => a.Severity == query.Severity);
        if (query.IsRead is not null)
            q = q.Where(a => a.IsRead == query.IsRead);

        var totalCount = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AlertResponse(a.Id, a.Title, a.Severity, a.Message, a.GeoEventId, a.IsRead, a.CreatedAt))
            .ToListAsync(ct);

        var result = new AlertListResponse(items, totalCount, page, pageSize);
        if (query.CacheKey is not null)
            await cache.SetAsync(query.CacheKey, result, CacheTtl, ct);
        return result;
    }
}

public static class GetAlertsEndpoint
{
    public static RouteGroupBuilder MapGetAlerts(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            int? page, int? pageSize, AlertSeverity? severity, bool? isRead,
            IQueryHandler<GetAlertsQuery, AlertListResponse> handler) =>
        {
            var p = page ?? 1;
            var ps = pageSize ?? 20;
            var cacheKey = $"alerts:{severity}:{isRead}:{p}:{ps}";
            var result = await handler.HandleAsync(new GetAlertsQuery(p, ps, severity, isRead, cacheKey), default);
            return Results.Ok(result);
        }).RequireAuthorization();
        return group;
    }
}