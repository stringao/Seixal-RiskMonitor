using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeoRisk.API.Features.Risk.Forecast;

public static class RiskForecastEndpoints
{
    private const string StableTrend = "stable";

    public static void MapRiskForecast(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/risk/forecast", GetForecast)
            .WithTags("Risk")
            .WithName("GetFwiForecast")
            .WithDescription("Get 7-day FWI forecast for a bounding box or point");

        app.MapGet("/api/risk/forecast/summary", GetForecastSummary)
            .WithTags("Risk")
            .WithName("GetFwiForecastSummary")
            .WithDescription("Get overall risk summary for the region for next 7 days");
    }

    private static async Task<IResult> GetForecast(
        [FromServices] FwiForecastService service,
        [FromQuery] double? minLat,
        [FromQuery] double? maxLat,
        [FromQuery] double? minLon,
        [FromQuery] double? maxLon,
        [FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        if (days < 1 || days > 7)
        {
            return Results.BadRequest(new { error = "Days must be between 1 and 7" });
        }

        var forecasts = await service.GetForecastsAsync(minLat, maxLat, minLon, maxLon, days, ct);

        // Group by forecast date and calculate statistics
        var grouped = forecasts
            .GroupBy(f => f.ForecastDate)
            .Select(g => new DayForecastResponse(
                g.Key,
                g.Select(f => new FwiForecastPointResponse(
                    f.GridPointId,
                    f.Location.X,
                    f.Location.Y,
                    f.HorizonDays,
                    f.FFMC,
                    f.DMC,
                    f.DC,
                    f.ISI,
                    f.BUI,
                    f.FWI,
                    f.RiskLevel)).ToList(),
                new FwiStatistics(
                    g.Min(f => f.FWI),
                    g.Max(f => f.FWI),
                    g.Average(f => f.FWI)),
                DetermineTrend(g)));

        return Results.Ok(new FwiForecastResponse(grouped.ToList()));
    }

    private static async Task<IResult> GetForecastSummary(
        [FromServices] FwiForecastService service,
        CancellationToken ct = default)
    {
        var summaries = await service.GetDailySummariesAsync(ct);

        if (summaries.Count == 0)
        {
            return Results.Ok(new FwiForecastSummaryResponse(
                null,
                null,
                "unknown",
                new List<DaySummaryResponse>()));
        }

        // Find peak fire risk day (highest average FWI)
        var peakDay = summaries.MaxBy(s => s.AvgFwi);

        // Determine overall trend
        var trend = DetermineTrendFromSummaries(summaries);

        // Find dominant risk level - build summary directly
        var daySummaries = summaries.Select(s => new DaySummaryResponse(
            s.Date,
            s.MinFwi,
            s.MaxFwi,
            s.AvgFwi,
            FwiCalculator.GetDangerRating(s.AvgFwi),
            s.MinRiskLevel,
            s.MaxRiskLevel)).ToList();

        return Results.Ok(new FwiForecastSummaryResponse(
            peakDay?.Date,
            peakDay?.AvgFwi,
            trend,
            daySummaries));
    }

    private static string DetermineTrend(IGrouping<DateTime, Domain.Entities.FwiForecast> group)
    {
        // Simple trend based on FWI progression
        var ordered = group.OrderBy(f => f.HorizonDays).ToList();
        if (ordered.Count < 2) return StableTrend;

        var firstFwi = ordered[0].FWI;
        var lastFwi = ordered[ordered.Count - 1].FWI;
        var diff = lastFwi - firstFwi;

        return diff switch
        {
            > 2 => "increasing",
            < -2 => "decreasing",
            _ => StableTrend
        };
    }

    private static string DetermineTrendFromSummaries(List<DailyFwiSummary> summaries)
    {
        if (summaries.Count < 2) return StableTrend;

        var first = summaries[0].AvgFwi;
        var last = summaries[summaries.Count - 1].AvgFwi;
        var diff = last - first;

        return diff switch
        {
            > 2 => "increasing",
            < -2 => "decreasing",
            _ => StableTrend
        };
    }
}

public sealed record FwiForecastResponse(List<DayForecastResponse> Days);

public sealed record DayForecastResponse(
    DateTime Date,
    List<FwiForecastPointResponse> Points,
    FwiStatistics Statistics,
    string Trend);

public sealed record FwiForecastPointResponse(
    string GridPointId,
    double Longitude,
    double Latitude,
    int HorizonDays,
    double FFMC,
    double DMC,
    double DC,
    double ISI,
    double BUI,
    double FWI,
    RiskLevel RiskLevel);

public sealed record FwiStatistics(
    double MinFwi,
    double MaxFwi,
    double AvgFwi);

public sealed record FwiForecastSummaryResponse(
    DateTime? PeakFireRiskDay,
    double? PeakFwi,
    string Trend,
    List<DaySummaryResponse> Days);

public sealed record DaySummaryResponse(
    DateTime Date,
    double MinFwi,
    double MaxFwi,
    double AvgFwi,
    string DangerRating,
    RiskLevel MinRiskLevel,
    RiskLevel MaxRiskLevel);