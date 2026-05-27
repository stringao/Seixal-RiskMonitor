using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using System.Globalization;
using System.Text.Json;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Service for detecting and analyzing chains of related fire events.
/// Identifies simultaneous fires, sequential correlations, resource contention,
/// and ember cast events where one fire drives another via downwind ember spread.
/// </summary>
public class EventChainAnalysisService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private const int SimultaneousWindowHours = 6;
    private const double FwiTolerance = 5.0;
    private const double EmberCastMinFrp = 500.0; // MW
    private const double EmberCastMinRangeKm = 10.0;
    private const double EmberCastMaxRangeKm = 20.0;
    private const int EmberCastTimeWindowHours = 2;

    private static readonly string[] WindDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

    public EventChainAnalysisService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Detects fires happening at the same time under similar conditions.
    /// Groups fires within a 6-hour window and checks for correlated conditions.
    /// </summary>
    public async Task<List<EventChainAnalysis>> DetectSimultaneousFiresAsync(CancellationToken ct = default)
    {
        var chains = new List<EventChainAnalysis>();
        var cutoffDate = DateTime.UtcNow.AddDays(-7);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recentFires = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= cutoffDate)
            .AsNoTracking()
            .ToListAsync(ct);

        if (recentFires.Count < 2)
            return chains;

        // Group by 6-hour windows
        var groupedByWindow = recentFires
            .GroupBy(f => new DateTime(
                f.OccurredAt.Year, f.OccurredAt.Month, f.OccurredAt.Day,
                (f.OccurredAt.Hour / SimultaneousWindowHours) * SimultaneousWindowHours, 0, 0,
                DateTimeKind.Utc))
            .Where(g => g.Count() >= 2);

        foreach (var windowGroup in groupedByWindow)
        {
            var fires = windowGroup.ToList();

            // Get weather data for each fire
            var fireWeatherData = await GetWeatherDataForEventsAsync(fires, db, ct);
            if (fireWeatherData.Count == 0)
                continue;

            // Check if all fires share similar conditions
            var avgWindDir = fireWeatherData.Average(w => w.WindDirection);
            var avgFwi = fireWeatherData.Average(w => w.FWI);
            var windDirVariance = fireWeatherData.Average(w => Math.Abs(w.WindDirection - avgWindDir));

            // All within same wind direction band (within 45 degrees) and FWI within tolerance
            var sameConditions = windDirVariance < 45 && fireWeatherData.All(w => Math.Abs(w.FWI - avgFwi) < FwiTolerance);

            if (sameConditions)
            {
                var primaryFire = fires.OrderByDescending(f => f.Severity).First();
                var findings = new
                {
                    windowStart = windowGroup.Key,
                    windowEnd = windowGroup.Key.AddHours(SimultaneousWindowHours),
                    fireCount = fires.Count,
                    averageFwi = avgFwi,
                    averageWindDirection = avgWindDir,
                    sameWindDirection = windDirVariance < 45,
                    sameFwiRange = fireWeatherData.All(w => Math.Abs(w.FWI - avgFwi) < FwiTolerance),
                    eventIds = fires.Select(f => f.Id).ToList()
                };

                chains.Add(new EventChainAnalysis
                {
                    Id = Guid.NewGuid(),
                    AnalysisType = "Simultaneous",
                    PrimaryEventId = primaryFire.Id,
                    Description = $"{fires.Count} fogos detectados na mesma janela temporal sob condições semelhantes",
                    ConfidenceScore = CalculateSimultaneousConfidence(fires.Count, windDirVariance, fireWeatherData),
                    Findings = JsonSerializer.Serialize(findings),
                    ContributingFactors = DetermineContributingFactors(fireWeatherData),
                    RecommendedAction = fires.Count >= 3 ? "Considere desplegar unidades adicionais e coordenar esfuerzos de extinção" : null,
                    AnalyzedAt = DateTime.UtcNow
                });
            }
        }

        return chains;
    }

    /// <summary>
    /// Detects fires where one ends and another starts nearby - potential sequential correlation.
    /// </summary>
    public async Task<List<EventChainAnalysis>> DetectSequentialCorrelationAsync(CancellationToken ct = default)
    {
        var chains = new List<EventChainAnalysis>();
        var cutoffDate = DateTime.UtcNow.AddDays(-14);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recentFires = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= cutoffDate)
            .AsNoTracking()
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(ct);

        if (recentFires.Count < 2)
            return chains;

        var fireStations = await db.FireStations.AsNoTracking().ToListAsync(ct);

        for (int i = 0; i < recentFires.Count - 1; i++)
        {
            var fire1 = recentFires[i];
            var fire2 = recentFires[i + 1];

            // Check if fire2 started within 24 hours of fire1 ending
            // For simplicity, we estimate fire1 ended at fire2's start (worst case)
            var timeBetween = (fire2.OccurredAt - fire1.OccurredAt).TotalHours;
            if (!IsWithinSequentialTimeWindow(timeBetween))
                continue;

            // Check if within reasonable distance (50km - typical fire perimeter spread)
            var distanceKm = CalculateDistanceKm(
                fire1.Geometry.Y, fire1.Geometry.X,
                fire2.Geometry.Y, fire2.Geometry.X);

            if (distanceKm > 50)
                continue;

            // Check if same or nearby fire stations might respond to both
            var sharedStation = FindSharedFireStation(fire1, fire2, fireStations);

            if (sharedStation != null || distanceKm < 20)
            {
                var findings = new
                {
                    firstEventId = fire1.Id,
                    secondEventId = fire2.Id,
                    timeBetweenHours = timeBetween,
                    distanceKm = distanceKm,
                    sharedFireStation = sharedStation?.Name,
                    possibleCauses = new[] { "Recurso compartilhado", "Propagação por terreno", "Ignição deliberada" }
                };

                chains.Add(new EventChainAnalysis
                {
                    Id = Guid.NewGuid(),
                    AnalysisType = "Sequential",
                    PrimaryEventId = fire1.Id,
                    Description = $"Incêndio sekuncário {fire2.Id.ToString()[..8]} iniciado a {distanceKm:F1}km do primeiro em {timeBetween:F1}h",
                    ConfidenceScore = CalculateSequentialConfidence(timeBetween, distanceKm, sharedStation != null),
                    Findings = JsonSerializer.Serialize(findings),
                    ContributingFactors = new List<string>
                    {
                        $"Tempo entre incêndios: {timeBetween:F1}h",
                        $"Distância: {distanceKm:F1}km",
                        sharedStation != null ? $"Estação partilhada: {sharedStation.Name}" : "Sem estação partilhada"
                    },
                    RecommendedAction = "Verificar se há recursos partilhados entre os incêndios",
                    AnalyzedAt = DateTime.UtcNow
                });
            }
        }

        return chains;
    }

    /// <summary>
    /// Detects cases where multiple fires are competing for the same firefighting units.
    /// </summary>
    public async Task<List<EventChainAnalysis>> DetectResourceContentionAsync(CancellationToken ct = default)
    {
        var chains = new List<EventChainAnalysis>();
        var cutoffDate = DateTime.UtcNow.AddHours(-48);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recentFires = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= cutoffDate)
            .AsNoTracking()
            .ToListAsync(ct);

        if (recentFires.Count < 2)
            return chains;

        var fireStations = await db.FireStations.AsNoTracking().ToListAsync(ct);

        // Group fires by nearby fire station
        var firesByStation = new Dictionary<Guid, List<(GeoEvent Fire, double Distance)>>();

        foreach (var fire in recentFires)
        {
            var nearestStations = FindNearestFireStations(fire, fireStations, 30.0);
            if (nearestStations.Count == 0)
                continue;

            var primaryStation = nearestStations[0];
            if (!firesByStation.ContainsKey(primaryStation.Station.Id))
                firesByStation[primaryStation.Station.Id] = new List<(GeoEvent, double)>();
            firesByStation[primaryStation.Station.Id].Add((fire, primaryStation.DistanceKm));
        }

        // Find stations with multiple nearby fires
        foreach (var kvp in firesByStation.Where(kv => kv.Value.Count >= 2))
        {
            var station = fireStations.First(s => s.Id == kvp.Key);
            var fires = kvp.Value;

            // Check if fires are far enough apart that single station can't handle all
            var maxSeparation = CalculateMaxSeparation(fires);

            if (maxSeparation > 20) // More than 20km apart
            {
                var findings = new
                {
                    fireStationId = station.Id,
                    fireStationName = station.Name,
                    fireCount = fires.Count,
                    fires = fires.Select(f => new
                    {
                        eventId = f.Fire.Id,
                        distanceFromStation = f.Distance
                    }).ToList(),
                    maxSeparationKm = maxSeparation,
                    personnelCount = station.PersonnelCount,
                    vehicleCount = station.VehicleCount
                };

                chains.Add(new EventChainAnalysis
                {
                    Id = Guid.NewGuid(),
                    AnalysisType = "ResourceContention",
                    PrimaryEventId = fires.OrderByDescending(f => f.Fire.Severity).First().Fire.Id,
                    Description = $"{fires.Count} incêndios a competir pela estação {station.Name} (distância máx: {maxSeparation:F1}km)",
                    ConfidenceScore = CalculateResourceContentionConfidence(fires.Count, maxSeparation, station.PersonnelCount),
                    Findings = JsonSerializer.Serialize(findings),
                    ContributingFactors = new List<string>
                    {
                        $"Estação: {station.Name}",
                        $"Incêndios: {fires.Count}",
                        $"Separção máx: {maxSeparation:F1}km",
                        $"Pessoal: {station.PersonnelCount}",
                        $"Veículos: {station.VehicleCount}"
                    },
                    RecommendedAction = "Solicitar apoio de estações vizinhas ou solicitar recursos adicionais",
                    AnalyzedAt = DateTime.UtcNow
                });
            }
        }

        return chains;
    }

    /// <summary>
    /// Identifies likely ember-cast secondary ignitions where a large fire (FRP > 500 MW)
    /// causes new fires within 10-20km downwind.
    /// </summary>
    public async Task<List<EventChainAnalysis>> DetectEmberCastEventsAsync(CancellationToken ct = default)
    {
        var chains = new List<EventChainAnalysis>();
        var cutoffDate = DateTime.UtcNow.AddDays(-7);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recentFires = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= cutoffDate)
            .AsNoTracking()
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(ct);

        if (recentFires.Count < 2)
            return chains;

        var fireWeatherData = await GetWeatherDataForEventsAsync(recentFires, db, ct);
        var weatherByEventId = fireWeatherData.ToDictionary(w => w.EventId, w => w);

        for (int i = 0; i < recentFires.Count; i++)
        {
            var potentialSourceFire = recentFires[i];

            // Check if this fire has high FRP (from metadata or severity-based estimate)
            var frp = EstimateFrpFromSeverity(potentialSourceFire.Severity);
            if (frp < EmberCastMinFrp)
                continue;

            // Get wind direction at time of fire
            if (!weatherByEventId.TryGetValue(potentialSourceFire.Id, out var sourceWeather))
                continue;

            for (int j = i + 1; j < recentFires.Count; j++)
            {
                var potentialTargetFire = recentFires[j];
                ProcessEmberCastTarget(potentialSourceFire, potentialTargetFire, sourceWeather, frp, chains);
            }
        }

        return chains;
    }

    private static void ProcessEmberCastTarget(
        GeoEvent sourceFire,
        GeoEvent targetFire,
        EventWeatherData sourceWeather,
        double sourceFrp,
        List<EventChainAnalysis> chains)
    {
        // Check timing and distance
        if (!IsValidEmberCastTarget(sourceFire, targetFire, out var distanceKm, out var timeBetween))
            return;

        // Check if target is downwind of source
        var bearingToTarget = CalculateBearing(
            sourceFire.Geometry.Y, sourceFire.Geometry.X,
            targetFire.Geometry.Y, targetFire.Geometry.X);

        var windBearing = sourceWeather.WindDirection;
        var angleDiff = Math.Abs(NormalizeAngle(bearingToTarget - windBearing));

        // Target is downwind if within 45 degrees of wind direction
        if (angleDiff > 45 && angleDiff < 315)
            return;

        var findings = new
        {
            sourceEventId = sourceFire.Id,
            targetEventId = targetFire.Id,
            estimatedFrpMw = sourceFrp,
            distanceKm = distanceKm,
            timeBetweenHours = timeBetween,
            sourceWindDirection = sourceWeather.WindDirection,
            bearingToTarget = bearingToTarget,
            angleFromDownwind = angleDiff,
            emberCastRangeKm = $"{EmberCastMinRangeKm}-{EmberCastMaxRangeKm}"
        };

        chains.Add(new EventChainAnalysis
        {
            Id = Guid.NewGuid(),
            AnalysisType = "EmberCast",
            PrimaryEventId = sourceFire.Id,
            Description = $"Probável ignição por brasas: {targetFire.Id.ToString()[..8]} iniciou {distanceKm:F1}km jusante {timeBetween:F1}h após {sourceFire.Id.ToString()[..8]}",
            ConfidenceScore = CalculateEmberCastConfidence(distanceKm, timeBetween, angleDiff),
            Findings = JsonSerializer.Serialize(findings),
            ContributingFactors = new List<string>
            {
                $"FRP estimado: {sourceFrp:F0} MW",
                $"Direção do vento: {GetWindDirectionName(sourceWeather.WindDirection)}",
                $"Distância: {distanceKm:F1}km",
                $"Intervalo temporal: {timeBetween:F1}h"
            },
            RecommendedAction = "Verificar padrão de ignição e considerar investigação de causas",
            AnalyzedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Generates a comprehensive chain analysis report for a set of events.
    /// </summary>

    // Helper methods for S3776 complexity reduction

    private static bool IsWithinSequentialTimeWindow(double timeBetween)
    {
        return timeBetween > 0 && timeBetween <= 24;
    }

    private static double CalculateMaxSeparation(List<(GeoEvent Fire, double Distance)> fires)
    {
        var maxSeparation = 0.0;
        for (int i = 0; i < fires.Count; i++)
        {
            for (int j = i + 1; j < fires.Count; j++)
            {
                var sep = CalculateDistanceKm(
                    fires[i].Fire.Geometry.Y, fires[i].Fire.Geometry.X,
                    fires[j].Fire.Geometry.Y, fires[j].Fire.Geometry.X);
                maxSeparation = Math.Max(maxSeparation, sep);
            }
        }
        return maxSeparation;
    }

    private static bool IsValidEmberCastTarget(
        GeoEvent sourceFire,
        GeoEvent targetFire,
        out double distanceKm,
        out double timeBetween)
    {
        distanceKm = 0;
        timeBetween = 0;

        // Check timing: target started within 2 hours of source
        timeBetween = (targetFire.OccurredAt - sourceFire.OccurredAt).TotalHours;
        if (timeBetween <= 0 || timeBetween > EmberCastTimeWindowHours)
            return false;

        // Check distance: within ember cast range
        distanceKm = CalculateDistanceKm(
            sourceFire.Geometry.Y, sourceFire.Geometry.X,
            targetFire.Geometry.Y, targetFire.Geometry.X);

        return distanceKm >= EmberCastMinRangeKm && distanceKm <= EmberCastMaxRangeKm;
    }

    public async Task<EventChainAnalysis> GenerateChainReportAsync(List<Guid> eventIds, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var events = await db.GeoEvents
            .Where(e => eventIds.Contains(e.Id))
            .AsNoTracking()
            .ToListAsync(ct);

        if (events.Count == 0)
        {
            return new EventChainAnalysis
            {
                AnalysisType = "Unknown",
                Description = "No events found for the provided IDs",
                ConfidenceScore = 0,
                AnalyzedAt = DateTime.UtcNow
            };
        }

        var fireEvents = events.Where(e => e.EventType == EventType.Fire).ToList();
        if (fireEvents.Count < 2)
        {
            return new EventChainAnalysis
            {
                AnalysisType = "Single",
                Description = "Analysis requires at least 2 fire events",
                ConfidenceScore = 0,
                ContributingFactors = new List<string> { $"Only {fireEvents.Count} fire event(s) provided" },
                AnalyzedAt = DateTime.UtcNow
            };
        }

        var weatherData = await GetWeatherDataForEventsAsync(fireEvents, db, ct);

        // Determine chain type based on characteristics
        var timeSpan = (fireEvents.Max(e => e.OccurredAt) - fireEvents.Min(e => e.OccurredAt)).TotalHours;
        var avgFwi = weatherData.Count > 0 ? weatherData.Average(w => w.FWI) : 0;
        var avgWindDir = weatherData.Count > 0 ? weatherData.Average(w => w.WindDirection) : 0;

        var analysisType = DetermineAnalysisType(timeSpan);

        var findings = new
        {
            eventCount = fireEvents.Count,
            timeSpanHours = timeSpan,
            averageFwi = avgFwi,
            averageWindDirection = avgWindDir,
            eventIds = eventIds,
            severities = fireEvents.Select(e => e.Severity.ToString()).ToList(),
            timeRange = new
            {
                earliest = fireEvents.Min(e => e.OccurredAt),
                latest = fireEvents.Max(e => e.OccurredAt)
            }
        };

        return new EventChainAnalysis
        {
            Id = Guid.NewGuid(),
            AnalysisType = analysisType,
            PrimaryEventId = fireEvents.OrderByDescending(e => e.Severity).First().Id,
            Description = GenerateChainDescription(fireEvents, timeSpan),
            ConfidenceScore = CalculateOverallConfidence(fireEvents.Count, timeSpan),
            Findings = JsonSerializer.Serialize(findings),
            ContributingFactors = DetermineContributingFactors(weatherData),
            RecommendedAction = GenerateRecommendation(analysisType),
            AnalyzedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets all stored chain analyses.
    /// </summary>
    public async Task<List<EventChainAnalysis>> GetChainAnalysesAsync(
        string? analysisType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        double? minConfidence = null,
        CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var query = db.EventChainAnalyses.AsNoTracking();

        if (!string.IsNullOrEmpty(analysisType))
            query = query.Where(c => c.AnalysisType == analysisType);

        if (fromDate.HasValue)
            query = query.Where(c => c.AnalyzedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(c => c.AnalyzedAt <= toDate.Value);

        if (minConfidence.HasValue)
            query = query.Where(c => c.ConfidenceScore >= minConfidence.Value);

        return await query.OrderByDescending(c => c.AnalyzedAt).ToListAsync(ct);
    }

    /// <summary>
    /// Gets currently active chains - chains involving events from the last 24 hours.
    /// </summary>
    public async Task<List<EventChainAnalysis>> GetActiveChainsAsync(CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddHours(-24);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        // Get recent fire events
        var recentEventIds = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= cutoffDate)
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (recentEventIds.Count == 0)
            return new List<EventChainAnalysis>();

        // Get chains where primary event is in recent events
        return await db.EventChainAnalyses
            .Where(c => c.PrimaryEventId.HasValue && recentEventIds.Contains(c.PrimaryEventId.Value))
            .Where(c => c.AnalyzedAt >= cutoffDate)
            .AsNoTracking()
            .OrderByDescending(c => c.ConfidenceScore)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Saves or updates chain analyses.
    /// </summary>
    public async Task SaveChainAnalysesAsync(List<EventChainAnalysis> chains, CancellationToken ct = default)
    {
        if (chains.Count == 0)
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        // For now, just add new chains (could add update logic later)
        db.EventChainAnalyses.AddRange(chains);
        await db.SaveChangesAsync(ct);
    }

    // Helper classes and methods

    private static string DetermineAnalysisType(double timeSpan)
    {
        if (timeSpan <= SimultaneousWindowHours)
            return "Simultaneous";
        if (timeSpan <= 24)
            return "Sequential";
        return "ComplexChain";
    }

    private sealed class EventWeatherData
    {
        public Guid EventId { get; set; }
        public double WindDirection { get; set; }
        public double FWI { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
    }

    private static async Task<List<EventWeatherData>> GetWeatherDataForEventsAsync(
        List<GeoEvent> events,
        GeoRiskDbContext db,
        CancellationToken ct)
    {
        var result = new List<EventWeatherData>();

        foreach (var evt in events)
        {
            var oneHourBefore = evt.OccurredAt.AddHours(-1);
            var oneHourAfter = evt.OccurredAt.AddHours(1);

            var weatherPoint = await db.WeatherRiskDataPoints
                .Where(w => w.Timestamp >= oneHourBefore && w.Timestamp <= oneHourAfter)
                .OrderBy(w => Math.Abs(w.Timestamp.Ticks - evt.OccurredAt.Ticks))
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            if (weatherPoint != null)
            {
                result.Add(new EventWeatherData
                {
                    EventId = evt.Id,
                    WindDirection = weatherPoint.WindDirection,
                    FWI = weatherPoint.FWI,
                    Temperature = weatherPoint.Temperature,
                    Humidity = weatherPoint.Humidity
                });
            }
        }

        return result;
    }

    private static string GetWindDirectionName(double degrees)
    {
        var index = (int)Math.Round(degrees / 45) % 8;
        return WindDirections[index];
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = ToRad(lon2 - lon1);
        var lat1Rad = ToRad(lat1);
        var lat2Rad = ToRad(lat2);

        var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
        var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

        var bearing = Math.Atan2(y, x) * 180 / Math.PI;
        return (bearing + 360) % 360;
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    private static double EstimateFrpFromSeverity(RiskLevel severity)
    {
        return severity switch
        {
            RiskLevel.Critical => 800,
            RiskLevel.High => 500,
            RiskLevel.Medium => 200,
            RiskLevel.Low => 50,
            _ => 100
        };
    }

    private static FireStation? FindSharedFireStation(GeoEvent fire1, GeoEvent fire2, List<FireStation> stations)
    {
        var stations1 = FindNearestFireStations(fire1, stations, 30.0);
        var stations2 = FindNearestFireStations(fire2, stations, 30.0);

        return stations1
            .Select(s => s.Station.Id)
            .Intersect(stations2.Select(s => s.Station.Id))
            .Select(id => stations.First(s => s.Id == id))
            .FirstOrDefault();
    }

    private static List<(FireStation Station, double DistanceKm)> FindNearestFireStations(
        GeoEvent fire, List<FireStation> stations, double maxDistanceKm)
    {
        return stations
            .Select(s => (Station: s, DistanceKm: CalculateDistanceKm(
                fire.Geometry.Y, fire.Geometry.X,
                s.Geometry.Y, s.Geometry.X)))
            .Where(x => x.DistanceKm <= maxDistanceKm)
            .OrderBy(x => x.DistanceKm)
            .ToList();
    }

    private static double CalculateSimultaneousConfidence(int fireCount, double windVariance, List<EventWeatherData> weatherData)
    {
        var baseConfidence = Math.Min(0.9, fireCount * 0.15);
        var windBonus = windVariance < 30 ? 0.1 : 0;
        var weatherBonus = weatherData.All(w => w.FWI > 20) ? 0.05 : 0;
        return Math.Min(1.0, baseConfidence + windBonus + weatherBonus);
    }

    private static double CalculateSequentialConfidence(double timeBetween, double distanceKm, bool hasSharedStation)
    {
        var timeBonus = GetSequentialTimeBonus(timeBetween);
        var distanceBonus = GetSequentialDistanceBonus(distanceKm);
        var stationBonus = hasSharedStation ? 0.2 : 0;
        return Math.Min(0.95, 0.3 + timeBonus + distanceBonus + stationBonus);
    }

    private static double GetSequentialTimeBonus(double timeBetween)
    {
        if (timeBetween < 6)
            return 0.2;
        if (timeBetween < 12)
            return 0.1;
        return 0;
    }

    private static double GetSequentialDistanceBonus(double distanceKm)
    {
        if (distanceKm < 10)
            return 0.2;
        if (distanceKm < 30)
            return 0.1;
        return 0;
    }

    private static double CalculateResourceContentionConfidence(int fireCount, double maxSeparation, int personnelCount)
    {
        var countFactor = Math.Min(0.4, fireCount * 0.1);
        var separationFactor = GetSeparationFactor(maxSeparation);
        var resourceFactor = GetResourceFactor(personnelCount);
        return Math.Min(0.95, countFactor + separationFactor + resourceFactor);
    }

    private static double GetSeparationFactor(double maxSeparation)
    {
        if (maxSeparation > 30)
            return 0.3;
        if (maxSeparation > 20)
            return 0.2;
        return 0.1;
    }

    private static double GetResourceFactor(int personnelCount)
    {
        if (personnelCount < 20)
            return 0.2;
        if (personnelCount < 50)
            return 0.1;
        return 0;
    }

    private static double CalculateEmberCastConfidence(double distanceKm, double timeBetween, double angleDiff)
    {
        var distanceOptimal = distanceKm >= 10 && distanceKm <= 15 ? 0.3 : 0.2;
        var timeOptimal = GetEmberCastTimeBonus(timeBetween);
        var angleOptimal = GetEmberCastAngleBonus(angleDiff);
        return Math.Min(0.95, 0.2 + distanceOptimal + timeOptimal + angleOptimal);
    }

    private static double GetEmberCastTimeBonus(double timeBetween)
    {
        if (timeBetween <= 1)
            return 0.3;
        if (timeBetween <= 2)
            return 0.2;
        return 0.1;
    }

    private static double GetEmberCastAngleBonus(double angleDiff)
    {
        if (angleDiff < 20)
            return 0.2;
        if (angleDiff < 45)
            return 0.1;
        return 0;
    }

    private static double CalculateOverallConfidence(int eventCount, double timeSpanHours)
    {
        var baseConfidence = Math.Min(0.8, eventCount * 0.1);
        var timeBonus = GetOverallTimeBonus(timeSpanHours);
        return Math.Min(0.95, baseConfidence + timeBonus);
    }

    private static double GetOverallTimeBonus(double timeSpanHours)
    {
        if (timeSpanHours <= 6)
            return 0.15;
        if (timeSpanHours <= 24)
            return 0.1;
        return 0;
    }

    private static List<string> DetermineContributingFactors(List<EventWeatherData> weatherData)
    {
        var factors = new List<string>();

        if (weatherData.Count > 0)
        {
            var avgFwi = weatherData.Average(w => w.FWI);
            if (avgFwi > 30) factors.Add($"FWI elevado: {avgFwi:F1}");
            if (avgFwi > 20) factors.Add("Condições de risco de incêndio");

            var avgWind = weatherData.Average(w => w.WindDirection);
            factors.Add($"Direção do vento: {GetWindDirectionName(avgWind)}");
        }

        return factors;
    }

    private static string GenerateChainDescription(List<GeoEvent> events, double timeSpanHours)
    {
        var fireCount = events.Count(e => e.EventType == EventType.Fire);
        var typeLabel = GetChainTypeLabel(timeSpanHours);
        return $"{fireCount} incêndios {typeLabel} detectados num período de {timeSpanHours:F1} horas";
    }

    private static string GetChainTypeLabel(double timeSpanHours)
    {
        if (timeSpanHours <= 6)
            return "simultâneos";
        if (timeSpanHours <= 24)
            return "em cadeia";
        return "relacionados";
    }

    private static string GenerateRecommendation(string analysisType)
    {
        return analysisType switch
        {
            "Simultaneous" => "Coordenar esforços de extinção entre os incêndios e verificar condições meteorológicas partilhadas",
            "Sequential" => "Investigar possível propagação ou causa comum",
            "ResourceContention" => "Solicitar recursos adicionais de estações vizinhas",
            "EmberCast" => "Verificar padrão de ignição e considerar investigação de causas",
            _ => "Analisar relação entre os incêndios"
        };
    }
}
