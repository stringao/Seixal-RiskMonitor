using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using System.Globalization;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Service for analyzing historical fire data to identify spatial-temporal hotspots.
/// Uses grid-based clustering (1km x 1km cells) to identify recurrent fire zones.
/// </summary>
public class HotspotAnalysisService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private const double CellSizeDegrees = 1.0 / 111.0; // ~1km at equator
    private const int MinFiresForHotspot = 3;
    private static readonly string[] WindDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private static readonly char[] SouthWestChars = { 'S', 'W' };

    public HotspotAnalysisService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Identifies all hotspots from historical fire events within the specified time window.
    /// </summary>
    /// <param name="years">Number of years of historical data to analyze (default 2).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of identified FireHotspot entities.</returns>
    public async Task<List<FireHotspot>> IdentifyHotspotsAsync(int years = 2, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-years);
        var hotspots = new List<FireHotspot>();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var fireEvents = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= cutoffDate)
            .AsNoTracking()
            .ToListAsync(ct);

        if (fireEvents.Count == 0)
            return hotspots;

        // Group fires by grid cell
        var cellGroups = fireEvents
            .GroupBy(e => GetGridCellId(e.Geometry.Y, e.Geometry.X))
            .Where(g => g.Count() >= MinFiresForHotspot);

        foreach (var group in cellGroups)
        {
            var cellId = group.Key;
            var cellEvents = group.ToList();

            // Calculate cell centroid
            var avgLat = cellEvents.Average(e => e.Geometry.Y);
            var avgLon = cellEvents.Average(e => e.Geometry.X);

            // Parse cell coordinates for polygon creation
            var (lat, lon) = ParseGridCellId(cellId);

            // Calculate temporal patterns
            var monthCounts = cellEvents
                .GroupBy(e => e.OccurredAt.Month)
                .ToDictionary(g => g.Key, g => g.Count());
            var peakMonth = monthCounts.Count > 0
                ? monthCounts.MaxBy(kv => kv.Value).Key
                : 1;

            var hourCounts = cellEvents
                .GroupBy(e => e.OccurredAt.Hour)
                .ToDictionary(g => g.Key, g => g.Count());
            var peakHour = hourCounts.Count > 0
                ? hourCounts.MaxBy(kv => kv.Value).Key
                : 12;

            // Calculate average severity
            var severityCounts = cellEvents
                .GroupBy(e => e.Severity)
                .ToDictionary(g => g.Key, g => g.Count());
            var avgSeverity = severityCounts.Count > 0
                ? severityCounts.MaxBy(kv => kv.Value).Key
                : RiskLevel.Medium;

            // Calculate total area burned (from metadata if available, estimate from severity otherwise)
            var totalArea = CalculateTotalAreaBurned(cellEvents);

            // Calculate average FWI from correlated weather data
            var averageFwi = await CalculateAverageFwiAsync(cellEvents, ct);

            // Determine common wind direction
            var commonWindDir = await DetermineCommonWindDirectionAsync(cellEvents, ct);

            // Calculate risk level based on frequency + area + severity
            var riskLevel = CalculateHotspotRiskLevel(cellEvents.Count, totalArea, avgSeverity);

            // Generate hotspot name based on location
            var name = GenerateHotspotName(avgLat, avgLon);

            var hotspot = new FireHotspot
            {
                Id = Guid.NewGuid(),
                Name = name,
                Location = new Point(avgLon, avgLat) { SRID = 4326 },
                GridCellId = cellId,
                FireCount = cellEvents.Count,
                TotalAreaBurned = totalArea,
                AverageSeverity = avgSeverity,
                PeakMonth = peakMonth,
                PeakHour = peakHour,
                CommonWindDirection = commonWindDir,
                AverageFwi = averageFwi,
                RiskLevel = riskLevel,
                CellGeometry = CreateCellPolygon(lat, lon),
                LastUpdated = DateTime.UtcNow
            };

            hotspots.Add(hotspot);
        }

        return hotspots;
    }

    /// <summary>
    /// Gets all hotspots, optionally filtered by risk level.
    /// </summary>
    public async Task<List<FireHotspot>> GetHotspotsAsync(RiskLevel? riskLevel = null, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var query = db.FireHotspots.AsNoTracking();

        if (riskLevel.HasValue)
            query = query.Where(h => h.RiskLevel == riskLevel.Value);

        return await query
            .OrderByDescending(h => h.RiskLevel)
            .ThenByDescending(h => h.FireCount)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Gets currently active hotspots based on recent fire activity.
    /// </summary>
    public async Task<List<FireHotspot>> GetActiveHotspotsAsync(CancellationToken ct = default)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recentFireCellIds = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= sevenDaysAgo)
            .AsNoTracking()
            .Select(e => GetGridCellId(e.Geometry.Y, e.Geometry.X))
            .Distinct()
            .ToListAsync(ct);

        return await db.FireHotspots
            .Where(h => recentFireCellIds.Contains(h.GridCellId))
            .AsNoTracking()
            .OrderByDescending(h => h.RiskLevel)
            .ThenByDescending(h => h.FireCount)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Gets a specific hotspot by ID with its associated alerts and recent events.
    /// </summary>
    public async Task<FireHotspot?> GetHotspotByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        return await db.FireHotspots
            .Include(h => h.Alerts.OrderByDescending(a => a.CreatedAt))
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    /// <summary>
    /// Finds hotspots near a given point.
    /// </summary>
    /// <param name="lat">Latitude.</param>
    /// <param name="lon">Longitude.</param>
    /// <param name="radiusKm">Search radius in kilometers (default 10).</param>
    public async Task<List<(FireHotspot Hotspot, double DistanceKm)>> FindHotspotsNearAsync(
        double lat, double lon, double radiusKm = 10, CancellationToken ct = default)
    {
        var point = new Point(lon, lat) { SRID = 4326 };
        var bufferDegrees = radiusKm / 111.0;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var nearbyHotspots = await db.FireHotspots
            .Where(h => h.Location.Distance(point) <= bufferDegrees)
            .AsNoTracking()
            .ToListAsync(ct);

        var result = nearbyHotspots
            .Select(h => (Hotspot: h, DistanceKm: CalculateDistanceKm(lat, lon, h.Location.Y, h.Location.X)))
            .OrderBy(r => r.DistanceKm)
            .ToList();

        return result;
    }

    /// <summary>
    /// Gets historical fire events within a specific hotspot's grid cell.
    /// </summary>
    public async Task<List<GeoEvent>> GetHotspotEventsAsync(string gridCellId, int years = 2, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-years);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        return await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= cutoffDate)
            .Where(e => GetGridCellId(e.Geometry.Y, e.Geometry.X) == gridCellId)
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Gets seasonal pattern data for a hotspot (fires by month).
    /// </summary>
    public async Task<Dictionary<int, int>> GetSeasonalPatternAsync(string gridCellId, CancellationToken ct = default)
    {
        var twoYearsAgo = DateTime.UtcNow.AddYears(-2);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var events = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= twoYearsAgo)
            .Where(e => GetGridCellId(e.Geometry.Y, e.Geometry.X) == gridCellId)
            .Select(e => e.OccurredAt.Month)
            .ToListAsync(ct);

        var pattern = Enumerable.Range(1, 12)
            .ToDictionary(m => m, m => events.Count(e => e == m));

        return pattern;
    }

    /// <summary>
    /// Checks for unusual activity in hotspot zones and creates alerts if needed.
    /// </summary>
    public async Task<List<HotspotAlert>> CheckForAlertsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var todayStart = today.AddDays(-1); // Last 24 hours

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recentFires = await GetRecentFiresAsync(db, todayStart, ct);
        if (recentFires.Count == 0)
            return new List<HotspotAlert>();

        var recentCellCounts = GetRecentCellCounts(recentFires);
        var hotspots = await GetHotspotsWithAlertsAsync(db, ct);

        var alerts = new List<HotspotAlert>();
        foreach (var hotspot in hotspots)
        {
            if (recentCellCounts.TryGetValue(hotspot.GridCellId, out var recentCount))
            {
                AddRecurrenceAlertIfNeeded(hotspot, recentCount, recentFires, today, alerts);
                AddNewHotspotAlertIfNeeded(hotspot, recentCount, recentFires, today, alerts);
            }
        }

        return alerts;
    }

    private static async Task<List<GeoEvent>> GetRecentFiresAsync(GeoRiskDbContext db, DateTime todayStart, CancellationToken ct)
    {
        return await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire && e.OccurredAt >= todayStart)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    private static Dictionary<string, int> GetRecentCellCounts(List<GeoEvent> recentFires)
    {
        return recentFires
            .GroupBy(e => GetGridCellId(e.Geometry.Y, e.Geometry.X))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static async Task<List<FireHotspot>> GetHotspotsWithAlertsAsync(GeoRiskDbContext db, CancellationToken ct)
    {
        return await db.FireHotspots
            .Include(h => h.Alerts)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    private static void AddRecurrenceAlertIfNeeded(
        FireHotspot hotspot,
        int recentCount,
        List<GeoEvent> recentFires,
        DateTime today,
        List<HotspotAlert> alerts)
    {
        // 50% of historical count in 24h
        if (recentCount < hotspot.FireCount * 0.5)
            return;

        var existingAlertToday = hotspot.Alerts
            .Any(a => a.CreatedAt >= today && a.AlertType == "RecurrenceAboveThreshold");

        if (existingAlertToday)
            return;

        var firstFireInCell = recentFires.First(e =>
            GetGridCellId(e.Geometry.Y, e.Geometry.X) == hotspot.GridCellId);

        alerts.Add(new HotspotAlert
        {
            Id = Guid.NewGuid(),
            FireHotspotId = hotspot.Id,
            GeoEventId = firstFireInCell.Id,
            AlertType = "RecurrenceAboveThreshold",
            Message = $"Hotspot {hotspot.Name} showing unusual activity: {recentCount} fires in 24h (historical avg: {hotspot.FireCount} fires)",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });
    }

    private static void AddNewHotspotAlertIfNeeded(
        FireHotspot hotspot,
        int recentCount,
        List<GeoEvent> recentFires,
        DateTime today,
        List<HotspotAlert> alerts)
    {
        if (hotspot.FireCount != recentCount)
            return;

        var existingNewHotspotAlert = hotspot.Alerts
            .Any(a => a.CreatedAt >= today && a.AlertType == "NewHotspot");

        if (existingNewHotspotAlert)
            return;

        var firstFireInCell = recentFires.First(e =>
            GetGridCellId(e.Geometry.Y, e.Geometry.X) == hotspot.GridCellId);

        alerts.Add(new HotspotAlert
        {
            Id = Guid.NewGuid(),
            FireHotspotId = hotspot.Id,
            GeoEventId = firstFireInCell.Id,
            AlertType = "NewHotspot",
            Message = $"New hotspot triggered at {hotspot.Name}: {recentCount} fire(s) detected.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });
    }

    /// <summary>
    /// Saves or updates hotspots in the database.
    /// </summary>
    public async Task SyncHotspotsAsync(List<FireHotspot> hotspots, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var existingHotspots = await db.FireHotspots.AsNoTracking().ToListAsync(ct);
        var existingByCell = existingHotspots.ToDictionary(h => h.GridCellId, h => h.Id);

        foreach (var hotspot in hotspots)
        {
            if (existingByCell.TryGetValue(hotspot.GridCellId, out var existingId))
            {
                // Update existing
                var existing = await db.FireHotspots.FindAsync(new object[] { existingId }, ct);
                if (existing != null)
                {
                    existing.Name = hotspot.Name;
                    existing.Location = hotspot.Location;
                    existing.FireCount = hotspot.FireCount;
                    existing.TotalAreaBurned = hotspot.TotalAreaBurned;
                    existing.AverageSeverity = hotspot.AverageSeverity;
                    existing.PeakMonth = hotspot.PeakMonth;
                    existing.PeakHour = hotspot.PeakHour;
                    existing.CommonWindDirection = hotspot.CommonWindDirection;
                    existing.AverageFwi = hotspot.AverageFwi;
                    existing.RiskLevel = hotspot.RiskLevel;
                    existing.CellGeometry = hotspot.CellGeometry;
                    existing.LastUpdated = DateTime.UtcNow;
                }
            }
            else
            {
                // Add new
                db.FireHotspots.Add(hotspot);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Creates alerts in the database.
    /// </summary>
    public async Task CreateAlertsAsync(List<HotspotAlert> alerts, CancellationToken ct = default)
    {
        if (alerts.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        db.HotspotAlerts.AddRange(alerts);
        await db.SaveChangesAsync(ct);
    }

    // Grid cell ID helpers

    private static string GetGridCellId(double lat, double lon)
    {
        var latDir = lat >= 0 ? "N" : "S";
        var lonDir = lon >= 0 ? "E" : "W";
        var latInt = (int)Math.Abs(Math.Floor(lat));
        var lonInt = (int)Math.Abs(Math.Floor(lon));

        return $"{latDir}{latInt}{lonDir}{lonInt:D3}";
    }

    private static (double lat, double lon) ParseGridCellId(string cellId)
    {
        // Parse "N38W009" format
        var latDir = cellId[0];
        var latEnd = cellId.IndexOfAny(SouthWestChars);
        var lonDir = cellId[latEnd];
        var lat = int.Parse(cellId.Substring(1, latEnd - 1));
        var lon = int.Parse(cellId.Substring(latEnd + 1));

        if (latDir == 'S') lat = -lat;
        if (lonDir == 'W') lon = -lon;

        return (lat + 0.5, lon + 0.5); // Return center of cell
    }

    private static Polygon CreateCellPolygon(double lat, double lon)
    {
        var halfCell = CellSizeDegrees / 2;
        var coords = new[]
        {
            new Coordinate(lon - halfCell, lat - halfCell),
            new Coordinate(lon + halfCell, lat - halfCell),
            new Coordinate(lon + halfCell, lat + halfCell),
            new Coordinate(lon - halfCell, lat + halfCell),
            new Coordinate(lon - halfCell, lat - halfCell) // Close ring
        };

        return new Polygon(new LinearRing(coords)) { SRID = 4326 };
    }

    // Calculation helpers

    private static double CalculateTotalAreaBurned(List<GeoEvent> events)
    {
        double total = 0;
        foreach (var evt in events)
        {
            // Try to extract area from metadata
            if (!string.IsNullOrEmpty(evt.Metadata))
            {
                // Simple heuristic: look for area burned estimation
                // In production, parse the JSON metadata
            }
            // Estimate based on severity
            total += evt.Severity switch
            {
                RiskLevel.Critical => 50,
                RiskLevel.High => 20,
                RiskLevel.Medium => 5,
                RiskLevel.Low => 1,
                _ => 1
            };
        }
        return total;
    }

    private async Task<double> CalculateAverageFwiAsync(List<GeoEvent> events, CancellationToken ct)
    {
        var eventIds = events.Select(e => e.Id).ToList();
        var eventTimestamps = events.ToDictionary(e => e.Id, e => e.OccurredAt);

        // Get weather data points within 1 hour of each fire event
        var fwiValues = new List<double>();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        foreach (var eventId in eventIds)
        {
            var eventTime = eventTimestamps[eventId];
            var oneHourBefore = eventTime.AddHours(-1);
            var oneHourAfter = eventTime.AddHours(1);

            var weatherPoint = await db.WeatherRiskDataPoints
                .Where(w => w.Timestamp >= oneHourBefore && w.Timestamp <= oneHourAfter)
                .OrderBy(w => Math.Abs(w.Timestamp.Ticks - eventTime.Ticks))
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            if (weatherPoint != null)
                fwiValues.Add(weatherPoint.FWI);
        }

        return fwiValues.Count > 0 ? fwiValues.Average() : 0;
    }

    private async Task<string?> DetermineCommonWindDirectionAsync(List<GeoEvent> events, CancellationToken ct)
    {
        var eventIds = events.Select(e => e.Id).ToList();
        var eventTimestamps = events.ToDictionary(e => e.Id, e => e.OccurredAt);

        var windDirections = new List<double>();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        foreach (var eventId in eventIds)
        {
            var eventTime = eventTimestamps[eventId];
            var oneHourBefore = eventTime.AddHours(-1);
            var oneHourAfter = eventTime.AddHours(1);

            var weatherPoint = await db.WeatherRiskDataPoints
                .Where(w => w.Timestamp >= oneHourBefore && w.Timestamp <= oneHourAfter)
                .OrderBy(w => Math.Abs(w.Timestamp.Ticks - eventTime.Ticks))
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            if (weatherPoint != null)
                windDirections.Add(weatherPoint.WindDirection);
        }

        if (windDirections.Count == 0)
            return null;

        // Group by octant (45-degree segments)
        var octantCounts = windDirections
            .GroupBy(wd => (int)(wd / 45) % 8)
            .ToDictionary(g => g.Key, g => g.Count());

        var dominantOctant = octantCounts.MaxBy(kv => kv.Value).Key;
        return WindDirections[dominantOctant];
    }

    private static RiskLevel CalculateHotspotRiskLevel(int fireCount, double totalArea, RiskLevel avgSeverity)
    {
        // Simple risk calculation: frequency * area * severity weight
        var score = fireCount * 2 + totalArea * 0.1 + (int)avgSeverity * 5;

        return score switch
        {
            >= 100 => RiskLevel.Critical,
            >= 50 => RiskLevel.High,
            >= 20 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }

    private static string GenerateHotspotName(double lat, double lon)
    {
        var latDir = lat >= 0 ? "N" : "S";
        var lonDir = lon >= 0 ? "E" : "W";

        return $"Zone {latDir}{Math.Abs(lat):F1}{lonDir}{Math.Abs(lon):F1}";
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in km
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}