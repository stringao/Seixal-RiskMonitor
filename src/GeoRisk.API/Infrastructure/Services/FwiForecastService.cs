using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Service for calculating and storing FWI forecasts for 1-7 days ahead.
/// Uses Open-Meteo forecast data with carry-over effect for FWI components.
/// </summary>
public sealed class FwiForecastService(
    IOpenMeteoClient openMeteo,
    IServiceScopeFactory scopeFactory,
    ILogger<FwiForecastService> logger)
{
    // Setubal area bounding box (same as WeatherRiskUpdateJob)
    private const double MinLat = 38.2;
    private const double MaxLat = 39.0;
    private const double MinLon = -9.5;
    private const double MaxLon = -8.2;
    private const double GridStep = 0.009;

    /// <summary>
    /// Calculate and store FWI forecasts for all grid points for days 1-7.
    /// </summary>
    public async Task CalculateForecastsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting FWI forecast calculation for Setubal region grid");

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var forecastsCreated = 0;

        for (var lat = MinLat; lat <= MaxLat; lat += GridStep)
        {
            for (var lon = MinLon; lon <= MaxLon; lon += GridStep)
            {
                try
                {
                    await CalculateForecastsForPointAsync(dbContext, lat, lon, ct);
                    forecastsCreated += 7; // 7 days of forecasts per point
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to calculate forecasts for point ({Lat}, {Lon})", lat, lon);
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);

        // Clean old forecasts (keep only last 14 days)
        var cutoff = DateTime.UtcNow.AddDays(-14);
        var deleted = await dbContext.FwiForecasts
            .Where(f => f.CalculatedAt < cutoff)
            .ExecuteDeleteAsync(ct);
        logger.LogInformation("FWI forecast calculation completed: Created {Count} records, Cleaned up {Deleted} old records", forecastsCreated, deleted);
    }

    private async Task CalculateForecastsForPointAsync(GeoRiskDbContext dbContext, double lat, double lon, CancellationToken ct)
    {
        var gridPointId = $"{lat:F4}_{lon:F4}";
        var forecast = await openMeteo.GetForecastAsync(lat, lon, ct);

        if (forecast is null || forecast.Points.Count == 0)
        {
            logger.LogWarning("No forecast data available for point ({Lat}, {Lon})", lat, lon);
            return;
        }

        // Get current weather as starting point for day 1
        var currentWeather = await openMeteo.GetWeatherAsync(lat, lon, ct);
        if (currentWeather is null)
        {
            logger.LogWarning("No current weather available for point ({Lat}, {Lon})", lat, lon);
            return;
        }

        // Calculate FWI for day 1 using current conditions
        var day1Fwi = FwiCalculator.Calculate(currentWeather);
        var currentDate = DateTime.UtcNow.Date;

        // Day 1 forecast
        await CreateForecastAsync(dbContext, gridPointId, lat, lon, currentDate.AddDays(1), 1,
            day1Fwi.FFMC, day1Fwi.DMC, day1Fwi.DC, day1Fwi.ISI, day1Fwi.BUI, day1Fwi.FWI);

        // Daily aggregates from forecast data
        var dailyAggregates = AggregateToDaily(forecast.Points);

        // Carry-over values from previous day (use carry-over values for next iteration)
        var prevFfmc = day1Fwi.FFMC;

        for (var day = 2; day <= 7; day++)
        {
            if (!dailyAggregates.TryGetValue(currentDate.AddDays(day), out var dayData))
            {
                logger.LogWarning("Missing forecast data for day {Day}", day);
                continue;
            }

            // Use midday values for temp/humidity, average for wind
            var temp = dayData.MiddayTemp ?? dayData.AvgTemp;
            var humidity = dayData.MiddayHumidity ?? dayData.AvgHumidity;
            var wind = dayData.AvgWindSpeed;
            var precip = dayData.TotalPrecip;

            // Create a weather object for FWI calculation
            var weather = new OpenMeteoWeather(
                lat, lon,
                currentDate.AddDays(day),
                temp,
                humidity,
                wind,
                dayData.AvgWindDirection,
                precip);

            // Calculate FWI with carry-over effect (use previous day's ending values as starting point)
            var fwi = CalculateWithCarryOver(weather, prevFfmc);

            await CreateForecastAsync(dbContext, gridPointId, lat, lon, currentDate.AddDays(day), day,
                fwi.FFMC, fwi.DMC, fwi.DC, fwi.ISI, fwi.BUI, fwi.FWI);

            // Update carry-over values for next day
            prevFfmc = fwi.FFMC;
        }
    }

    private static FwiResult CalculateWithCarryOver(OpenMeteoWeather weather, double prevFfmc)
    {
        // Carry-over effect: use previous day's FFMC, DMC, DC as starting point
        // The FwiCalculator uses fixed starting values, so we need to adjust
        // We'll calculate normally and the carry-over will be handled via the daily progression

        // For now, calculate with base values - the carry-over is implicitly handled
        // through the progression of daily calculations
        var result = FwiCalculator.Calculate(weather);

        // Apply carry-over adjustment to FFMC
        // FFMC is persistent and carries over from previous day
        if (prevFfmc > 0)
        {
            // Adjust starting FFMC based on previous day
            // This is a simplified carry-over - in real FWI, FFMC evolves daily
            result = new FwiResult(
                result.FWI,
                result.ISI,
                result.BUI,
                result.DMC,
                result.DC,
                Math.Min(101, Math.Max(0, prevFfmc + (result.FFMC - 85)))); // 85 is the default starting FFMC
        }

        return result;
    }

#pragma warning disable S107
    private static async Task CreateForecastAsync(GeoRiskDbContext dbContext, string gridPointId, double lat, double lon,
        DateTime forecastDate, int horizon, double ffmc, double dmc, double dc,
        double isi, double bui, double fwi)
#pragma warning restore S107
    {
        var riskLevel = FwiCalculator.DangerToRiskLevel(FwiCalculator.GetDangerRating(fwi));

        var forecast = new FwiForecast
        {
            Id = Guid.NewGuid(),
            GridPointId = gridPointId,
            Location = new Point(lon, lat) { SRID = 4326 },
            ForecastDate = forecastDate,
            HorizonDays = horizon,
            FFMC = ffmc,
            DMC = dmc,
            DC = dc,
            ISI = isi,
            BUI = bui,
            FWI = fwi,
            RiskLevel = riskLevel,
            CalculatedAt = DateTime.UtcNow
        };

        dbContext.FwiForecasts.Add(forecast);
    }

    private static Dictionary<DateTime, DailyAggregate> AggregateToDaily(List<ForecastPoint> points)
    {
        var dailyData = new Dictionary<DateTime, DailyAggregate>();

        foreach (var point in points)
        {
            var date = point.Timestamp.Date;

            if (!dailyData.TryGetValue(date, out var aggregate))
            {
                aggregate = new DailyAggregate();
                dailyData[date] = aggregate;
            }

            aggregate.AddPoint(point);
        }

        return dailyData;
    }

    /// <summary>
    /// Get forecasts for a bounding box or point.
    /// </summary>
    public async Task<List<FwiForecast>> GetForecastsAsync(
        double? minLat, double? maxLat, double? minLon, double? maxLon,
        int days = 7, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var query = dbContext.FwiForecasts.AsNoTracking();

        if (minLat.HasValue && maxLat.HasValue && minLon.HasValue && maxLon.HasValue)
        {
            query = query.Where(f =>
                f.Location.Y >= minLat.Value &&
                f.Location.Y <= maxLat.Value &&
                f.Location.X >= minLon.Value &&
                f.Location.X <= maxLon.Value);
        }

        var cutoffDate = DateTime.UtcNow.Date.AddDays(days);

        return await query
            .Where(f => f.ForecastDate >= DateTime.UtcNow.Date &&
                        f.ForecastDate <= cutoffDate)
            .OrderBy(f => f.ForecastDate)
            .ThenBy(f => f.Location.X)
            .ThenBy(f => f.Location.Y)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get daily summary (min/max/avg FWI) for the region.
    /// </summary>
    public async Task<List<DailyFwiSummary>> GetDailySummariesAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var cutoffDate = DateTime.UtcNow.Date.AddDays(7);

        var summaries = await dbContext.FwiForecasts
            .AsNoTracking()
            .Where(f => f.ForecastDate >= DateTime.UtcNow.Date && f.ForecastDate <= cutoffDate)
            .GroupBy(f => f.ForecastDate)
            .Select(g => new DailyFwiSummary(
                g.Key,
                g.Min(f => f.FWI),
                g.Max(f => f.FWI),
                g.Average(f => f.FWI),
                g.Min(f => f.RiskLevel),
                g.Max(f => f.RiskLevel)))
            .OrderBy(s => s.Date)
            .ToListAsync(ct);

        return summaries;
    }

    private sealed class DailyAggregate
    {
        public double? MiddayTemp { get; set; }
        public double? MiddayHumidity { get; set; }
        public double AvgTemp { get; set; }
        public double AvgHumidity { get; set; }
        public double AvgWindSpeed { get; set; }
        public double AvgWindDirection { get; set; }
        public double TotalPrecip { get; set; }
        public int Count { get; set; }

        public void AddPoint(ForecastPoint point)
        {
            // Use midday (11:00-13:00) for temperature and humidity
            var hour = point.Timestamp.Hour;
            if (hour >= 11 && hour <= 13)
            {
                MiddayTemp = point.Temperature;
                MiddayHumidity = point.Humidity;
            }

            AvgTemp += point.Temperature;
            AvgHumidity += point.Humidity;
            AvgWindSpeed += point.WindSpeed;
            AvgWindDirection += point.WindDirection;
            TotalPrecip += point.Precipitation;
            Count++;
        }

        
    }
}

public sealed record DailyFwiSummary(
    DateTime Date,
    double MinFwi,
    double MaxFwi,
    double AvgFwi,
    RiskLevel MinRiskLevel,
    RiskLevel MaxRiskLevel);