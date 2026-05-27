using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.BackgroundJobs;

/// <summary>
/// Background job that runs every 30 minutes to:
/// 1. Update weather data points for the Setubal grid
/// 2. Calculate FWI for each point
/// 3. Generate/updated risk zone polygons
/// 4. Calculate fire spread predictions for active fires
/// </summary>
public sealed class WeatherRiskUpdateJob(
    IOpenMeteoClient openMeteo,
    FireSpreadCalculator fireSpreadCalculator,
    IServiceScopeFactory scopeFactory,
    ILogger<WeatherRiskUpdateJob> logger) : BackgroundService
{
    // Setubal area bounding box
    private const double MinLat = 38.2;
    private const double MaxLat = 39.0;
    private const double MinLon = -9.5;
    private const double MaxLon = -8.2;

    // Grid resolution in degrees (~1km)
    private const double GridStep = 0.009;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to let app start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunUpdateCycleAsync(stoppingToken);
        }
    }

    private async Task RunUpdateCycleAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting weather risk update job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

            // Step 1: Update weather data points
            await UpdateWeatherDataPointsAsync(db, ct);

            // Step 2: Update fire spread predictions
            await UpdateFireSpreadPredictionsAsync(db, ct);

            logger.LogInformation("Weather risk update job completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weather risk update job failed");
        }
    }

    private async Task UpdateWeatherDataPointsAsync(GeoRiskDbContext db, CancellationToken ct)
    {
        var pointsUpdated = 0;

        // Create grid of points
        for (var lat = MinLat; lat <= MaxLat; lat += GridStep)
        {
            for (var lon = MinLon; lon <= MaxLon; lon += GridStep)
            {
                try
                {
                    var weather = await openMeteo.GetWeatherAsync(lat, lon, ct);
                    if (weather is null) continue;

                    var fwi = FwiCalculator.Calculate(weather);
                    var riskLevel = FwiCalculator.DangerToRiskLevel(FwiCalculator.GetDangerRating(fwi.FWI));
                    var conclusion = GenerateConclusion(weather, fwi, riskLevel);

                    var dataPoint = new WeatherRiskDataPoint
                    {
                        Id = Guid.NewGuid(),
                        Location = new Point(lon, lat) { SRID = 4326 },
                        Timestamp = DateTime.UtcNow,
                        Temperature = weather.TemperatureCelsius,
                        Humidity = weather.RelativeHumidityPercent,
                        WindSpeed = weather.WindSpeedKmh,
                        WindDirection = weather.WindDirectionDegrees,
                        Precipitation = weather.PrecipitationMm,
                        FFMC = fwi.FFMC,
                        DMC = fwi.DMC,
                        DC = fwi.DC,
                        ISI = fwi.ISI,
                        BUI = fwi.BUI,
                        FWI = fwi.FWI,
                        RiskLevel = riskLevel,
                        Conclusion = conclusion,
                        GridPointId = $"{lat:F4}_{lon:F4}"
                    };

                    db.WeatherRiskDataPoints.Add(dataPoint);
                    pointsUpdated++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to process grid point ({Lat}, {Lon})", lat, lon);
                }
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated {Count} weather data points", pointsUpdated);

        // Clean up old data points (keep last 7 days)
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var deleted = await db.WeatherRiskDataPoints
            .Where(p => p.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);
        logger.LogInformation("Cleaned up {Count} old data points", deleted);
    }

    private async Task UpdateFireSpreadPredictionsAsync(GeoRiskDbContext db, CancellationToken ct)
    {
        var activeFires = await db.GeoEvents
            .AsNoTracking()
            .Where(e => e.EventType == Domain.Enums.EventType.Fire)
            .ToListAsync(ct);

        var predictionsUpdated = 0;

        foreach (var fire in activeFires)
        {
            try
            {
                var weather = await openMeteo.GetWeatherAsync(fire.Geometry.Y, fire.Geometry.X, ct);
                if (weather is null) continue;

                var fwi = FwiCalculator.Calculate(weather);

                // Calculate predictions for moderate scenario
                var predictions = fireSpreadCalculator.CalculateSpread(
                    fire,
                    fire.Geometry.Y,  // latitude
                    fire.Geometry.X,  // longitude
                    weather,
                    fwi,
                    FireSpreadScenario.Moderate);

                // Remove old predictions for this fire
                var oldPreds = await db.FireSpreadPredictions
                    .Where(p => p.FireEventId == fire.Id)
                    .ToListAsync(ct);
                db.FireSpreadPredictions.RemoveRange(oldPreds);

                // Add new predictions
                db.FireSpreadPredictions.AddRange(predictions);
                predictionsUpdated += predictions.Count;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to update fire spread for fire {FireId}", fire.Id);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated {PredictionsUpdated} fire spread predictions for {Fires} fires",
            predictionsUpdated, activeFires.Count);
    }

    private static string GenerateConclusion(OpenMeteoWeather weather, FwiResult fwi, RiskLevel level)
    {
        var parts = new List<string>();

        if (weather.TemperatureCelsius > 30) parts.Add("temperatura elevada");
        if (weather.RelativeHumidityPercent < 30) parts.Add("humidade muito baixa");
        if (weather.WindSpeedKmh > 20) parts.Add("vento forte");
        if (fwi.FWI > 30) parts.Add($"FWI {fwi.FWI:F0} ({FwiCalculator.GetDangerRating(fwi.FWI)})");

        var conditions = parts.Count > 0 ? string.Join(", ", parts) : "condições normais";
        return $"Área em atenção devido a {conditions}. Nível de risco: {level}.";
    }
}