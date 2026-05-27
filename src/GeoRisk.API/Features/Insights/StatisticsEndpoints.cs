using System.Globalization;
using System.Text.Json;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Insights;

/// <summary>
/// API endpoints for insights and statistics.
/// </summary>
public static class StatisticsEndpoints
{
    public static RouteGroupBuilder MapStatistics(this RouteGroupBuilder group)
    {
        group.MapGet("/statistics/seasonal", GetSeasonalStatistics)
            .WithTags("Insights")
            .WithName("GetSeasonalStatistics")
            .WithSummary("Get seasonal fire statistics with year/month/region filters")
            .RequireAuthorization();

        group.MapGet("/statistics/trends", GetTrendAnalysis)
            .WithTags("Insights")
            .WithName("GetTrendAnalysis")
            .WithSummary("5-year trend analysis - fires per year, area per year, trend direction")
            .RequireAuthorization();

        group.MapGet("/compare", CompareSeasons)
            .WithTags("Insights")
            .WithName("CompareSeasons")
            .WithSummary("Compare current season with historical average")
            .RequireAuthorization();

        return group;
    }

    /// <summary>
    /// Get seasonal statistics with year, month, and region filters.
    /// </summary>
    private static async Task<IResult> GetSeasonalStatistics(
        int? year,
        int? month,
        string? region,
        GeoRiskDbContext db,
        CancellationToken ct)
    {
        var query = db.SeasonalStatistics.AsNoTracking();

        if (year.HasValue)
            query = query.Where(s => s.Year == year.Value);

        if (month.HasValue)
            query = query.Where(s => s.Month == month.Value);

        if (!string.IsNullOrEmpty(region))
            query = query.Where(s => s.Region == region);

        var stats = await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToListAsync(ct);

        var response = stats.Select(s => new SeasonalStatisticsResponse(
            Id: s.Id,
            Year: s.Year,
            Month: s.Month,
            MonthLabel: s.Month == 0 ? "Annual" : new DateTime(s.Year, s.Month, 1, 0, 0, 0, DateTimeKind.Utc).ToString("MMMM", CultureInfo.InvariantCulture),
            Region: s.Region,
            TotalFires: s.TotalFires,
            TotalAreaHa: s.TotalAreaHa,
            LargestFireHa: s.LargestFireHa,
            AverageFwi: s.AverageFwi,
            AverageTemperature: s.AverageTemperature,
            TotalPrecipitationMm: s.TotalPrecipitationMm,
            PeakFireDay: s.PeakFireDay,
            FireCauseBreakdown: ParseJson<Dictionary<string, int>>(s.FireCauseBreakdown),
            DailyFireCounts: ParseJson<List<int>>(s.DailyFireCounts),
            CalculatedAt: s.CalculatedAt
        )).ToList();

        return Results.Ok(response);
    }

    /// <summary>
    /// Get 5-year trend analysis.
    /// </summary>
    private static async Task<IResult> GetTrendAnalysis(
        string? region,
        PostIncidentAnalysisService analysisService,
        CancellationToken ct)
    {
        var result = await analysisService.GetTrendAnalysisAsync(region ?? "Portugal", ct);

        var response = new TrendAnalysisResponse(
            Years: result.Years,
            FiresPerYear: result.FiresPerYear,
            AreaPerYear: result.AreaPerYear,
            TrendDirection: result.TrendDirection,
            TrendDescription: result.TrendDirection switch
            {
                "increasing" => "O número de incêndios está aumentando nos últimos 5 anos.",
                "decreasing" => "O número de incêndios está diminuindo nos últimos 5 anos.",
                _ => "O número de incêndios permanece estável nos últimos 5 anos."
            },
            AverageFiresPerYear: result.FiresPerYear.Count > 0 ? result.FiresPerYear.Average() : 0,
            AverageAreaPerYear: result.AreaPerYear.Count > 0 ? result.AreaPerYear.Average() : 0
        );

        return Results.Ok(response);
    }

    /// <summary>
    /// Compare current season with historical average.
    /// </summary>
    private static async Task<IResult> CompareSeasons(
        int? year,
        int? month,
        string? region,
        PostIncidentAnalysisService analysisService,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var targetYear = year ?? now.Year;
        var targetMonth = month ?? now.Month;
        var targetRegion = region ?? "Portugal";

        var result = await analysisService.CompareWithHistoricalAsync(targetYear, targetMonth, targetRegion, ct);

        var response = new SeasonComparisonResponse(
            CurrentFires: result.CurrentFires,
            HistoricalAvgFires: result.HistoricalAvgFires,
            CurrentAreaHa: result.CurrentAreaHa,
            HistoricalAvgAreaHa: result.HistoricalAvgAreaHa,
            Comparison: result.Comparison,
            ComparisonDescription: result.Comparison switch
            {
                "above_average" => $"Esta estação tem {Math.Abs(result.FireChange ?? 0):P0} mais incêndios que a média histórica.",
                "below_average" => $"Esta estação tem {Math.Abs(result.FireChange ?? 0):P0} menos incêndios que a média histórica.",
                "normal" => "Esta estação está dentro da média histórica.",
                _ => "Dados históricos insuficientes para comparação."
            },
            FireChange: result.FireChange,
            AreaChange: result.AreaChange,
            FwiChange: result.FwiChange,
            Year: targetYear,
            Month: targetMonth,
            Region: targetRegion
        );

        return Results.Ok(response);
    }

    private static T? ParseJson<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }
}

public record SeasonalStatisticsResponse(
    Guid Id,
    int Year,
    int Month,
    string MonthLabel,
    string Region,
    int TotalFires,
    double TotalAreaHa,
    double LargestFireHa,
    double AverageFwi,
    double AverageTemperature,
    double TotalPrecipitationMm,
    DateTime? PeakFireDay,
    Dictionary<string, int>? FireCauseBreakdown,
    List<int>? DailyFireCounts,
    DateTime CalculatedAt);

public record TrendAnalysisResponse(
    List<int> Years,
    List<int> FiresPerYear,
    List<double> AreaPerYear,
    string TrendDirection,
    string TrendDescription,
    double AverageFiresPerYear,
    double AverageAreaPerYear);

public record SeasonComparisonResponse(
    int? CurrentFires,
    double? HistoricalAvgFires,
    double? CurrentAreaHa,
    double? HistoricalAvgAreaHa,
    string Comparison,
    string ComparisonDescription,
    double? FireChange,
    double? AreaChange,
    double? FwiChange,
    int Year,
    int Month,
    string Region);
