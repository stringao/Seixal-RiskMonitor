using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Service for fetching, processing, and storing air quality and pollen data.
/// Uses Open-Meteo Air Quality API with European AQI standards.
/// </summary>
public sealed class AirQualityService(
    IOpenMeteoClient openMeteo,
    IServiceScopeFactory scopeFactory,
    ILogger<AirQualityService> logger)
{
    private const string UnknownValue = "Unknown";
    // Setubal area bounding box (same as FwiForecastService)
    private const double MinLat = 38.2;
    private const double MaxLat = 39.0;
    private const double MinLon = -9.5;
    private const double MaxLon = -8.2;
    private const double GridStep = 0.009;

    // European AQI thresholds for pollutants (µg/m³)
    // Based on CAQI (Common Air Quality Index) standards
    private const double Pm10VeryGood = 20, Pm10Good = 35, Pm10Medium = 50, Pm10Poor = 100;
    private const double Pm25VeryGood = 10, Pm25Good = 20, Pm25Medium = 25, Pm25Poor = 50;
    private const double No2VeryGood = 40, No2Good = 90, No2Medium = 120, No2Poor = 230;
    private const double O3VeryGood = 50, O3Good = 100, O3Medium = 130, O3Poor = 240;
    private const double So2VeryGood = 50, So2Good = 100, So2Medium = 200, So2Poor = 350;
    private const double CoVeryGood = 200, CoGood = 400, CoMedium = 800, CoPoor = 1000;

    /// <summary>
    /// Fetch and store air quality data for all grid points in the region.
    /// </summary>
    public async Task FetchAndStoreAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting air quality data fetch for region");

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recordsCreated = 0;

        for (var lat = MinLat; lat <= MaxLat; lat += GridStep)
        {
            for (var lon = MinLon; lon <= MaxLon; lon += GridStep)
            {
                try
                {
                    await FetchAndStoreForPointAsync(dbContext, lat, lon, ct);
                    recordsCreated++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to fetch air quality for point ({Lat}, {Lon})", lat, lon);
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);

        // Clean old data (keep only last 7 days)
        var cutoff = DateTime.UtcNow.Date.AddDays(-7);
        var deleted = await dbContext.AirQualityDataPoints
            .Where(a => a.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);
        logger.LogInformation("Air quality fetch completed: Created {NewCount} records, Cleaned up {DeletedCount} old records", recordsCreated, deleted);
    }

    /// <summary>
    /// Fetch air quality for a specific point and return current data with today's average and forecast.
    /// </summary>
    public async Task<AirQualityResult> GetAirQualityAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var airQuality = await openMeteo.GetAirQualityAsync(latitude, longitude, ct);

        if (airQuality is null)
        {
            return new AirQualityResult(null, null, new List<DailyAirQuality>(), "Unable to fetch data");
        }

        // Calculate today's daily aggregate
        var today = DateTime.UtcNow.Date;
        var todayHours = airQuality.HourlyForecast
            .Where(h => h.Timestamp.Date == today)
            .ToList();

        var todayRaw = AggregateHourly(todayHours);
        var todayAggregate = todayRaw is null ? null : ToCurrentDayAggregate(todayRaw);

        // Group hourly data into daily forecasts
        var dailyForecasts = airQuality.HourlyForecast
            .GroupBy(h => h.Timestamp.Date)
            .Where(g => g.Key >= today)
            .OrderBy(g => g.Key)
            .Take(5)
            .Select(g => ToDailyAirQuality(g.Key, AggregateHourly(g.ToList())))
            .Where(d => d is not null)
            .Cast<DailyAirQuality>()
            .ToList();

        return new AirQualityResult(
            airQuality.Current,
            todayAggregate,
            dailyForecasts,
            null);
    }

    /// <summary>
    /// Get aggregated air quality for the entire Setubal region.
    /// </summary>
    public async Task<RegionAirQualityResult> GetRegionAirQualityAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var today = DateTime.UtcNow.Date;
        var todayData = await dbContext.AirQualityDataPoints
            .AsNoTracking()
            .Where(a => a.Timestamp.Date == today)
            .ToListAsync(ct);

        if (todayData.Count == 0)
        {
            return new RegionAirQualityResult(null, null, new List<GridPointAirQuality>(), "No data available");
        }

        // Calculate region averages
        var avgPm10 = todayData.Average(a => a.Pm10 ?? 0);
        var avgPm25 = todayData.Average(a => a.Pm25 ?? 0);
        var avgO3 = todayData.Average(a => a.Ozone ?? 0);
        var avgNo2 = todayData.Average(a => a.NitrogenDioxide ?? 0);

        var regionAqi = CalculateAqiFromValues(avgPm10, avgPm25, avgNo2, avgO3, null, null);

        var gridPoints = todayData.Select(a => new GridPointAirQuality(
            a.Location.X,
            a.Location.Y,
            a.Pm10,
            a.Pm25,
            a.Ozone,
            a.NitrogenDioxide,
            a.AqiValue ?? 1,
            a.AqiCategory ?? UnknownValue,
            a.HealthRiskLevel ?? UnknownValue)).ToList();

        return new RegionAirQualityResult(
            regionAqi.aqiValue,
            regionAqi.category,
            gridPoints,
            null);
    }

    /// <summary>
    /// Get pollen-specific data for a location.
    /// </summary>
    public async Task<PollenResult> GetPollenAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var airQuality = await openMeteo.GetAirQualityAsync(latitude, longitude, ct);

        if (airQuality is null)
        {
            return new PollenResult(null, null, null, null, new Dictionary<string, double>(), "Unable to fetch data");
        }

        // Calculate today's pollen average
        var today = DateTime.UtcNow.Date;
        var todayPollen = airQuality.HourlyForecast
            .Where(h => h.Timestamp.Date == today)
            .ToList();

        var pollenValues = new Dictionary<string, double?>();
        var pollenBreakdown = new Dictionary<string, double>();

        if (todayPollen.Count > 0)
        {
            pollenValues["grass"] = AverageOrNull(todayPollen, p => p.GrassPollen);
            pollenValues["olive"] = AverageOrNull(todayPollen, p => p.OlivePollen);
            pollenValues["alder"] = AverageOrNull(todayPollen, p => p.AlderPollen);
            pollenValues["birch"] = AverageOrNull(todayPollen, p => p.BirchPollen);
            pollenValues["mugwort"] = AverageOrNull(todayPollen, p => p.MugwortPollen);
            pollenValues["ragweed"] = AverageOrNull(todayPollen, p => p.RagweedPollen);

            foreach (var kvp in pollenValues)
            {
                pollenBreakdown[kvp.Key] = kvp.Value ?? 0;
            }
        }

        // Find dominant pollen
        var dominantPollen = pollenValues.MaxBy(kvp => kvp.Value ?? 0).Key;

        // Calculate pollen index
        var avgPollen = pollenValues.Values.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty(0).Average();
        var pollenIndex = CalculatePollenIndex(avgPollen);

        // Current values
        var currentPollen = new CurrentPollenData(
            airQuality.Current.GrassPollen,
            airQuality.Current.OlivePollen,
            airQuality.Current.AlderPollen,
            airQuality.Current.BirchPollen,
            airQuality.Current.MugwortPollen,
            airQuality.Current.RagweedPollen);

        return new PollenResult(
            pollenIndex.index,
            pollenIndex.category,
            dominantPollen,
            currentPollen,
            pollenBreakdown,
            null);
    }

    private async Task FetchAndStoreForPointAsync(GeoRiskDbContext dbContext, double lat, double lon, CancellationToken ct)
    {
        var gridPointId = $"{lat:F4}_{lon:F4}";
        var airQuality = await openMeteo.GetAirQualityAsync(lat, lon, ct);

        if (airQuality is null || airQuality.HourlyForecast.Count == 0)
        {
            logger.LogWarning("No air quality data available for point ({Lat}, {Lon})", lat, lon);
            return;
        }

        // Group hourly data into daily aggregates
        var dailyGroups = airQuality.HourlyForecast
            .GroupBy(h => h.Timestamp.Date)
            .OrderBy(g => g.Key)
            .Take(5);

        foreach (var dayGroup in dailyGroups)
        {
            var dailyData = AggregateHourly(dayGroup.ToList());
            if (dailyData == null) continue;

            // Calculate AQI based on "worst pollutant" principle
            var aqiResult = CalculateAqiFromValues(
                dailyData.Pm10,
                dailyData.Pm25,
                dailyData.NitrogenDioxide,
                dailyData.Ozone,
                dailyData.SulphurDioxide,
                dailyData.CarbonMonoxide);

            // Calculate pollen index
            var pollenIndex = CalculatePollenIndex(dailyData.AvgPollen);

            // Calculate combined health risk
            var healthRisk = CalculateHealthRisk(aqiResult.aqiValue, pollenIndex.index);

            var dataPoint = new AirQualityDataPoint
            {
                Id = Guid.NewGuid(),
                GridPointId = gridPointId,
                Location = new Point(lon, lat) { SRID = 4326 },
                Timestamp = dayGroup.Key,

                // Pollutants (use daily average)
                Pm10 = dailyData.Pm10,
                Pm25 = dailyData.Pm25,
                NitrogenDioxide = dailyData.NitrogenDioxide,
                Ozone = dailyData.Ozone,
                SulphurDioxide = dailyData.SulphurDioxide,
                CarbonMonoxide = dailyData.CarbonMonoxide,
                Dust = dailyData.Dust,
                AerosolOpticalDepth = dailyData.AerosolOpticalDepth,

                // AQI
                AqiValue = aqiResult.aqiValue,
                AqiCategory = aqiResult.category,
                DominantPollutant = aqiResult.dominantPollutant,

                // Pollen
                GrassPollen = dailyData.GrassPollen,
                OlivePollen = dailyData.OlivePollen,
                AlderPollen = dailyData.AlderPollen,
                BirchPollen = dailyData.BirchPollen,
                MugwortPollen = dailyData.MugwortPollen,
                RagweedPollen = dailyData.RagweedPollen,
                PollenIndex = pollenIndex.index,
                PollenCategory = pollenIndex.category,
                DominantPollen = pollenIndex.dominantPollen,

                // Health risk
                HealthRiskLevel = healthRisk,

                CreatedAt = DateTime.UtcNow
            };

            dbContext.AirQualityDataPoints.Add(dataPoint);
        }
    }

    private static DailyAggregateResult? AggregateHourly(List<AirQualityHourly> hours)
    {
        if (hours.Count == 0) return null;

        return new DailyAggregateResult(
            AverageOrNull(hours, h => h.Pm10),
            AverageOrNull(hours, h => h.Pm25),
            AverageOrNull(hours, h => h.NitrogenDioxide),
            AverageOrNull(hours, h => h.Ozone),
            AverageOrNull(hours, h => h.SulphurDioxide),
            AverageOrNull(hours, h => h.CarbonMonoxide),
            AverageOrNull(hours, h => h.Dust),
            AverageOrNull(hours, h => h.AerosolOpticalDepth),
            AverageOrNull(hours, h => h.GrassPollen),
            AverageOrNull(hours, h => h.OlivePollen),
            AverageOrNull(hours, h => h.AlderPollen),
            AverageOrNull(hours, h => h.BirchPollen),
            AverageOrNull(hours, h => h.MugwortPollen),
            AverageOrNull(hours, h => h.RagweedPollen));
    }

    private static double? AverageOrNull(List<AirQualityHourly> hours, Func<AirQualityHourly, double?> selector)
    {
        var values = hours.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return values.Count > 0 ? values.Average() : null;
    }

    private static (int aqiValue, string category, string? dominantPollutant) CalculateAqiFromValues(
        double? pm10, double? pm25, double? no2, double? o3, double? so2, double? co)
    {
        var pollutants = new List<(string name, double? value, double veryGood, double good, double medium, double poor)>
        {
            ("PM10", pm10, Pm10VeryGood, Pm10Good, Pm10Medium, Pm10Poor),
            ("PM2.5", pm25, Pm25VeryGood, Pm25Good, Pm25Medium, Pm25Poor),
            ("NO2", no2, No2VeryGood, No2Good, No2Medium, No2Poor),
            ("O3", o3, O3VeryGood, O3Good, O3Medium, O3Poor),
            ("SO2", so2, So2VeryGood, So2Good, So2Medium, So2Poor),
            ("CO", co, CoVeryGood, CoGood, CoMedium, CoPoor)
        };

        // Calculate individual index for each pollutant
        var indices = new List<(string name, int index)>();

        foreach (var p in pollutants.Where(p => p.value.HasValue))
        {
            var index = CalculateSingleAqi(p.value!.Value, p.veryGood, p.good, p.medium, p.poor);
            if (index > 0)
            {
                indices.Add((p.name, index));
            }
        }

        // Use "worst pollutant" - highest index determines overall AQI
        if (indices.Count == 0)
        {
            return (1, "Very Good", null);
        }

        var worst = indices.MaxBy(i => i.index);
        return (worst.index, IndexToCategory(worst.index), worst.name);
    }

    private static int CalculateSingleAqi(double value, double veryGood, double good, double medium, double poor)
    {
        if (value <= veryGood) return 1;
        if (value <= good) return 2;
        if (value <= medium) return 3;
        if (value <= poor) return 4;
        return 5;
    }

    private static string IndexToCategory(int index) => index switch
    {
        1 => "Very Good",
        2 => "Good",
        3 => "Medium",
        4 => "Poor",
        5 => "Bad",
        _ => UnknownValue
    };

    private static (int index, string category, string? dominantPollen) CalculatePollenIndex(double avgPollen)
    {
        var index = avgPollen switch
        {
            0 => 1,                    // None
            <= 10 => 2,                // Low
            <= 30 => 3,                // Moderate
            <= 60 => 4,                // High
            _ => 5                     // Very High
        };

        var category = index switch
        {
            1 => "None",
            2 => "Low",
            3 => "Moderate",
            4 => "High",
            5 => "Very High",
            _ => UnknownValue
        };

        return (index, category, null);
    }

    private static string CalculateHealthRisk(int aqiValue, int pollenIndex)
    {
        // Combined risk based on worst of AQI and pollen
        var maxIndex = Math.Max(aqiValue, pollenIndex);

        return maxIndex switch
        {
            1 => "Low",
            2 => "Low",
            3 => "Moderate",
            4 => "High",
            5 => "Very High",
            _ => "Moderate"
        };
    }

    private static CurrentDayAggregate ToCurrentDayAggregate(DailyAggregateResult agg)
    {
        var aqi = CalculateAqiFromValues(agg.Pm10, agg.Pm25, agg.NitrogenDioxide, agg.Ozone, agg.SulphurDioxide, agg.CarbonMonoxide);
        var pollen = CalculatePollenIndex(agg.AvgPollen);
        var healthRisk = CalculateHealthRisk(aqi.aqiValue, pollen.index);
        return new CurrentDayAggregate(
            agg.Pm10, agg.Pm25, agg.NitrogenDioxide, agg.Ozone,
            agg.SulphurDioxide, agg.CarbonMonoxide,
            aqi.aqiValue, aqi.category, pollen.index, pollen.category, healthRisk);
    }

    private static DailyAirQuality? ToDailyAirQuality(DateTime date, DailyAggregateResult? agg)
    {
        if (agg is null) return null;
        var aqi = CalculateAqiFromValues(agg.Pm10, agg.Pm25, agg.NitrogenDioxide, agg.Ozone, agg.SulphurDioxide, agg.CarbonMonoxide);
        var pollen = CalculatePollenIndex(agg.AvgPollen);
        return new DailyAirQuality(date, agg.Pm10, agg.Pm25, agg.NitrogenDioxide, agg.Ozone,
            aqi.aqiValue, aqi.category, pollen.index, pollen.category);
    }

    private sealed record DailyAggregateResult(
        double? Pm10,
        double? Pm25,
        double? NitrogenDioxide,
        double? Ozone,
        double? SulphurDioxide,
        double? CarbonMonoxide,
        double? Dust,
        double? AerosolOpticalDepth,
        double? GrassPollen,
        double? OlivePollen,
        double? AlderPollen,
        double? BirchPollen,
        double? MugwortPollen,
        double? RagweedPollen)
    {
        public double AvgPollen => new[] { GrassPollen, OlivePollen, AlderPollen, BirchPollen, MugwortPollen, RagweedPollen }
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Average();
    }
}

// ─── Result Records ────────────────────────────────────────────────────

public sealed record AirQualityResult(
    AirQualityCurrent? Current,
    CurrentDayAggregate? TodayAverage,
    List<DailyAirQuality> Forecast,
    string? Error);

public sealed record CurrentDayAggregate(
    double? Pm10,
    double? Pm25,
    double? NitrogenDioxide,
    double? Ozone,
    double? SulphurDioxide,
    double? CarbonMonoxide,
    int? AqiValue,
    string? AqiCategory,
    int? PollenIndex,
    string? PollenCategory,
    string? HealthRiskLevel);

public sealed record DailyAirQuality(
    DateTime Date,
    double? Pm10,
    double? Pm25,
    double? NitrogenDioxide,
    double? Ozone,
    int? AqiValue,
    string? AqiCategory,
    int? PollenIndex,
    string? PollenCategory);

public sealed record RegionAirQualityResult(
    int? RegionAqiValue,
    string? RegionAqiCategory,
    List<GridPointAirQuality> GridPoints,
    string? Error);

public sealed record GridPointAirQuality(
    double Longitude,
    double Latitude,
    double? Pm10,
    double? Pm25,
    double? Ozone,
    double? NitrogenDioxide,
    int AqiValue,
    string AqiCategory,
    string HealthRiskLevel);

public sealed record PollenResult(
    int? PollenIndex,
    string? PollenCategory,
    string? DominantPollen,
    CurrentPollenData? Current,
    Dictionary<string, double> Breakdown,
    string? Error);

public sealed record CurrentPollenData(
    double? GrassPollen,
    double? OlivePollen,
    double? AlderPollen,
    double? BirchPollen,
    double? MugwortPollen,
    double? RagweedPollen);