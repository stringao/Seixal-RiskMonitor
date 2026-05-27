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
            .Select(a => new AlertResponse(
                a.Id,
                a.Title,
                a.Severity,
                a.Message,
                a.GeoEventId,
                a.IsRead,
                a.CreatedAt,
                a.AlertRuleId,
                a.AlertRule != null ? a.AlertRule.Name : null,
                a.FwiValue,
                a.WindSpeed,
                a.Temperature,
                a.AreaKm2,
                a.IsEscalated,
                a.EscalatedFromAlertId,
                a.ExpiresAt))
            .ToListAsync(ct);

        var result = new AlertListResponse(items, totalCount, page, pageSize);
        if (query.CacheKey is not null)
            await cache.SetAsync(query.CacheKey, result, CacheTtl, ct);
        return result;
    }
}

public sealed record GetActiveAlertsQuery(string? CacheKey) : IQuery<ActiveAlertsResponse>;

public sealed class GetActiveAlertsHandler(
    GeoRiskDbContext db,
    ICacheService cache) : IQueryHandler<GetActiveAlertsQuery, ActiveAlertsResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    public async Task<ActiveAlertsResponse> HandleAsync(GetActiveAlertsQuery query, CancellationToken ct)
    {
        if (query.CacheKey is not null)
        {
            var cached = await cache.GetAsync<ActiveAlertsResponse>(query.CacheKey, ct);
            if (cached is not null) return cached;
        }

        var now = DateTime.UtcNow;

        // Active alerts: not expired, not dismissed (IsRead = false or recently active)
        var activeAlerts = await db.Alerts
            .AsNoTracking()
            .Include(a => a.AlertRule)
            .Where(a => a.ExpiresAt == null || a.ExpiresAt > now)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlertResponse(
                a.Id,
                a.Title,
                a.Severity,
                a.Message,
                a.GeoEventId,
                a.IsRead,
                a.CreatedAt,
                a.AlertRuleId,
                a.AlertRule != null ? a.AlertRule.Name : null,
                a.FwiValue,
                a.WindSpeed,
                a.Temperature,
                a.AreaKm2,
                a.IsEscalated,
                a.EscalatedFromAlertId,
                a.ExpiresAt))
            .ToListAsync(ct);

        // Calculate summary by severity
        var summary = new AlertSummaryBySeverity(
            Info: activeAlerts.Count(a => a.Severity == AlertSeverity.Info),
            Warning: activeAlerts.Count(a => a.Severity == AlertSeverity.Warning),
            Danger: activeAlerts.Count(a => a.Severity == AlertSeverity.Danger),
            Critical: activeAlerts.Count(a => a.Severity == AlertSeverity.Critical),
            Trend: await CalculateTrendAsync(db, ct));

        var result = new ActiveAlertsResponse(activeAlerts, activeAlerts.Count, summary);
        if (query.CacheKey is not null)
            await cache.SetAsync(query.CacheKey, result, CacheTtl, ct);
        return result;
    }

    private static async Task<AlertTrend> CalculateTrendAsync(GeoRiskDbContext db, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var todayCount = await db.Alerts
            .Where(a => a.CreatedAt >= today)
            .CountAsync(ct);

        var yesterdayCount = await db.Alerts
            .Where(a => a.CreatedAt >= yesterday && a.CreatedAt < today)
            .CountAsync(ct);

        var direction = CalculateTrendDirection(todayCount, yesterdayCount);

        return new AlertTrend(yesterdayCount, todayCount, direction);
    }

    private static string CalculateTrendDirection(int todayCount, int yesterdayCount)
    {
        if (todayCount > yesterdayCount) return "increasing";
        if (todayCount < yesterdayCount) return "decreasing";
        return "stable";
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
            var ps = pageSize ?? 5;
            var cacheKey = $"alerts:{severity}:{isRead}:{p}:{ps}";
            var result = await handler.HandleAsync(new GetAlertsQuery(p, ps, severity, isRead, cacheKey), default);
            return Results.Ok(result);
        }).RequireAuthorization();

        return group;
    }
}

public static class GetActiveAlertsEndpoint
{
    public static RouteGroupBuilder MapGetActiveAlerts(this RouteGroupBuilder group)
    {
        group.MapGet("/active", async (
            IQueryHandler<GetActiveAlertsQuery, ActiveAlertsResponse> handler) =>
        {
            var result = await handler.HandleAsync(new GetActiveAlertsQuery("active_alerts"), default);
            return Results.Ok(result);
        }).RequireAuthorization();

        return group;
    }
}
