using System.Globalization;
using System.Text.Json;
using GeoRisk.API.Features.Ai.Dashboard;
using GeoRisk.API.Infrastructure.Services;
using GeoRisk.API.Infrastructure.Persistence;

namespace GeoRisk.API.Features.Ai;

public static class DashboardEndpoints
{
    private const string AiDashboardTag = "AI Dashboard";
    private const string StableTrend = "stable";

    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/dashboard/today", GetTodayDashboard)
            .WithTags(AiDashboardTag);

        group.MapGet("/dashboard/weekly", GetWeeklyDashboard)
            .WithTags(AiDashboardTag);

        group.MapGet("/dashboard/compare", GetCompareDashboard)
            .WithTags(AiDashboardTag);

        group.MapGet("/dashboard/situation", GetSituationDashboard)
            .WithTags(AiDashboardTag);

        return group;
    }

    private static async Task<IResult> GetTodayDashboard(
        IDashboardGeneratorService dashboardService)
    {
        const string cacheKey = "dashboard_today";
        const string provider = "deepseek";
        var cacheDuration = TimeSpan.FromMinutes(30);

        var cached = await TryGetCachedAsync<DashboardResponse>(dashboardService, cacheKey);
        if (cached != null) return Results.Ok(cached);

        var result = await dashboardService.GenerateRiskSummaryAsync();
        if (result == null)
            return Results.Json(new { error = "AI not configured or generation failed" }, statusCode: 503);

        await CacheResultAsync(dashboardService, cacheKey, result, provider, cacheDuration);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetWeeklyDashboard(
        IDashboardGeneratorService dashboardService)
    {
        var weekKey = $"weekly_report_w{ISOWeek.GetWeekOfYear(DateTime.UtcNow)}";
        const string provider = "deepseek";
        var cacheDuration = TimeSpan.FromHours(6);

        var cached = await TryGetCachedAsync<WeeklyReportResponse>(dashboardService, weekKey);
        if (cached != null) return Results.Ok(cached);

        var result = await dashboardService.GenerateWeeklyReportAsync();
        if (result == null)
            return Results.Json(new { error = "AI not configured or generation failed" }, statusCode: 503);

        await CacheResultAsync(dashboardService, weekKey, result, provider, cacheDuration);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCompareDashboard(
        IDashboardGeneratorService dashboardService,
        GeoRiskDbContext db)
    {
        const string cacheKey = "dashboard_compare";
        const string provider = "system";
        var cacheDuration = TimeSpan.FromMinutes(30);

        var cached = await dashboardService.GetCachedDashboardAsync(cacheKey);
        if (cached != null)
            return Results.Ok(JsonSerializer.Deserialize<object>(cached));

        var comparison = await BuildComparisonDataAsync(db);
        await CacheResultAsync(dashboardService, cacheKey, comparison, provider, cacheDuration);
        return Results.Ok(comparison);
    }

    private static async Task<IResult> GetSituationDashboard(
        IDashboardGeneratorService dashboardService)
    {
        const string cacheKey = "situation_report";
        const string provider = "deepseek";
        var cacheDuration = TimeSpan.FromMinutes(5);

        var cached = await TryGetCachedAsync<SituationReportResponse>(dashboardService, cacheKey);
        if (cached != null) return Results.Ok(cached);

        var result = await dashboardService.GenerateSituationReportAsync();
        if (result == null)
            return Results.Json(new { error = "AI not configured or generation failed" }, statusCode: 503);

        await CacheResultAsync(dashboardService, cacheKey, result, provider, cacheDuration);
        return Results.Ok(result);
    }

    private static async Task<T?> TryGetCachedAsync<T>(IDashboardGeneratorService dashboardService, string cacheKey)
    {
        var cached = await dashboardService.GetCachedDashboardAsync(cacheKey);
        return cached == null ? default : JsonSerializer.Deserialize<T>(cached);
    }

    private static async Task CacheResultAsync(
        IDashboardGeneratorService dashboardService,
        string cacheKey,
        object result,
        string provider,
        TimeSpan duration)
    {
        var json = JsonSerializer.Serialize(result);
        await dashboardService.SetCachedDashboardAsync(cacheKey, json, provider, duration);
    }

    private static async Task<object> BuildComparisonDataAsync(GeoRiskDbContext db)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var lastWeekStart = weekStart.AddDays(-7);
        var lastMonthStart = now.AddDays(-30);

        var currentWeekEvents = await db.GeoEvents
            .Where(e => e.OccurredAt >= weekStart)
            .AsNoTracking()
            .CountAsync();

        var prevWeekEvents = await db.GeoEvents
            .Where(e => e.OccurredAt >= lastWeekStart && e.OccurredAt < weekStart)
            .AsNoTracking()
            .CountAsync();

        var currentFwi = await GetWeatherDataAsync(db, todayStart, null);
        var historicalFwi = await GetWeatherDataAsync(db, lastMonthStart, todayStart);

        var currentAvgFwi = currentFwi.Count > 0 ? currentFwi.Average(w => w.FWI) : 0;
        var historicalAvgFwi = historicalFwi.Count > 0 ? historicalFwi.Average(w => w.FWI) : 0;

        return new
        {
            currentWeekEvents,
            previousWeekEvents = prevWeekEvents,
            weekOverWeekChange = prevWeekEvents > 0
                ? Math.Round((double)(currentWeekEvents - prevWeekEvents) / prevWeekEvents * 100, 1)
                : 0,
            currentFwiAverage = Math.Round(currentAvgFwi, 1),
            historicalFwiAverage = Math.Round(historicalAvgFwi, 1),
            fwiTrend = DetermineFwiTrend(currentAvgFwi, historicalAvgFwi),
            generatedAt = DateTime.UtcNow
        };
    }

    private static string DetermineFwiTrend(double currentFwi, double historicalFwi)
    {
        if (currentFwi > historicalFwi) return "increasing";
        if (currentFwi < historicalFwi) return "decreasing";
        return StableTrend;
    }

    private static async Task<List<WeatherRiskDataPoint>> GetWeatherDataAsync(
        GeoRiskDbContext db,
        DateTime from,
        DateTime? to)
    {
        var query = db.WeatherRiskDataPoints
            .Where(w => w.Timestamp >= from);

        if (to.HasValue)
            query = query.Where(w => w.Timestamp < to.Value);

        return await query.AsNoTracking().ToListAsync();
    }
}
