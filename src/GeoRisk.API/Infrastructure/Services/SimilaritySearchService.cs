using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Finds similar historical events based on multiple criteria:
/// - Event type
/// - Severity
/// - Location (within radius)
/// - Season (month)
/// - Weather conditions (FWI range, wind, temp)
/// - Time of day
/// </summary>
public class SimilaritySearchService
{
    private readonly GeoRiskDbContext _db;

    public SimilaritySearchService(GeoRiskDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Finds events similar to the given event.
    /// </summary>
    /// <param name="eventId">The target event ID</param>
    /// <param name="topN">Number of similar events to return</param>
    /// <param name="maxRadiusKm">Maximum radius in km for location similarity</param>
    public async Task<SimilarEvent[]> FindSimilarEventsAsync(Guid eventId, int topN = 5, double maxRadiusKm = 50, CancellationToken ct = default)
    {
        var targetEvent = await _db.GeoEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (targetEvent == null)
            return Array.Empty<SimilarEvent>();

        // Get weather data around the target event time
        var targetWeather = await _db.WeatherRiskDataPoints
            .AsNoTracking()
            .Where(w => w.Timestamp >= targetEvent.OccurredAt.AddHours(-2) &&
                        w.Timestamp <= targetEvent.OccurredAt.AddHours(2))
            .OrderBy(w => Math.Abs((w.Timestamp - targetEvent.OccurredAt).TotalMinutes))
            .FirstOrDefaultAsync(ct);

        // Get all events of the same type within a time window
        var timeWindow = TimeSpan.FromDays(365); // Look at past year
        var candidateEvents = await _db.GeoEvents
            .AsNoTracking()
            .Where(e => e.Id != eventId)
            .Where(e => e.EventType == targetEvent.EventType)
            .Where(e => e.OccurredAt >= DateTime.UtcNow.Subtract(timeWindow))
            .ToListAsync(ct);

        var similarEvents = new List<SimilarEvent>();

        foreach (var candidate in candidateEvents)
        {
            var score = CalculateSimilarity(targetEvent, candidate, targetWeather);

            if (score > 0.3) // Minimum threshold
            {
                similarEvents.Add(new SimilarEvent
                {
                    EventId = candidate.Id,
                    Score = score,
                    Title = candidate.Title,
                    OccurredAt = candidate.OccurredAt,
                    Severity = candidate.Severity
                });
            }
        }

        return similarEvents
            .OrderByDescending(e => e.Score)
            .Take(topN)
            .ToArray();
    }

    /// <summary>
    /// Calculates similarity score between two events (0-1).
    /// </summary>
    private static double CalculateSimilarity(GeoEvent target, GeoEvent candidate, WeatherRiskDataPoint? targetWeather)
    {
        double score = 0;
        double totalWeight = 0;

        score += CalculateEventTypeScore(target, candidate); totalWeight += 0.20;
        score += CalculateSeverityScore(target, candidate); totalWeight += 0.20;
        score += CalculateLocationScore(target, candidate); totalWeight += 0.25;
        score += CalculateSeasonScore(target, candidate); totalWeight += 0.15;
        score += CalculateTimeOfDayScore(target, candidate); totalWeight += 0.10;
        score += CalculateWeatherScore(targetWeather); totalWeight += 0.10;

        return totalWeight > 0 ? score / totalWeight * 100 / 100 : score;
    }

    private static double CalculateEventTypeScore(GeoEvent target, GeoEvent candidate)
    {
        return target.EventType == candidate.EventType ? 0.20 : 0;
    }

    private static double CalculateSeverityScore(GeoEvent target, GeoEvent candidate)
    {
        var severityDiff = Math.Abs((int)target.Severity - (int)candidate.Severity);
        return CalculateSeverityDiffScore(severityDiff);
    }

    private static double CalculateSeverityDiffScore(int severityDiff)
    {
        if (severityDiff == 0) return 0.20;
        if (severityDiff == 1) return 0.10;
        return 0;
    }

    private static double CalculateLocationScore(GeoEvent target, GeoEvent candidate)
    {
        var distanceKm = CalculateDistanceKm(target.Geometry.Y, target.Geometry.X,
                                             candidate.Geometry.Y, candidate.Geometry.X);
        return CalculateDistanceScore(distanceKm);
    }

    private static double CalculateDistanceScore(double distanceKm)
    {
        if (distanceKm <= 10) return 0.25;
        if (distanceKm <= 25) return 0.15;
        if (distanceKm <= 50) return 0.10;
        return 0;
    }

    private static double CalculateSeasonScore(GeoEvent target, GeoEvent candidate)
    {
        if (target.OccurredAt.Month == candidate.OccurredAt.Month)
            return 0.15;

        var targetSeason = GetSeason(target.OccurredAt.Month);
        var candidateSeason = GetSeason(candidate.OccurredAt.Month);
        return targetSeason == candidateSeason ? 0.08 : 0;
    }

    private static double CalculateTimeOfDayScore(GeoEvent target, GeoEvent candidate)
    {
        var hourDiff = Math.Abs(target.OccurredAt.Hour - candidate.OccurredAt.Hour);
        return CalculateTimeDiffScore(hourDiff);
    }

    private static double CalculateTimeDiffScore(int hourDiff)
    {
        if (hourDiff <= 2) return 0.10;
        if (hourDiff <= 4) return 0.05;
        return 0;
    }

    private static double CalculateWeatherScore(WeatherRiskDataPoint? targetWeather)
    {
        if (targetWeather == null)
            return 0;

        // Note: This is a simplified version that returns 0 when no candidateWeather is found
        // In production, this would need access to WeatherRiskDataPoints
        return 0.0;
    }

    /// <summary>
    /// Calculates distance between two coordinates using Haversine formula.
    /// </summary>
    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static int GetSeason(int month)
    {
        return month switch
        {
            >= 3 and <= 5 => 0, // Spring
            >= 6 and <= 8 => 1, // Summer
            >= 9 and <= 11 => 2, // Fall
            _ => 3 // Winter
        };
    }

    /// <summary>
    /// Finds similar events for a hypothetical scenario (without an existing event ID).
    /// </summary>
#pragma warning disable S107
    public async Task<SimilarEvent[]> FindSimilarByAttributesAsync(
        EventType eventType,
        RiskLevel severity,
        double latitude,
        double longitude,
        int month,
        double? fwi,
        int topN = 5,
        CancellationToken ct = default)
#pragma warning restore S107
    {
        var timeWindow = TimeSpan.FromDays(365);
        var candidateEvents = await _db.GeoEvents
            .AsNoTracking()
            .Where(e => e.EventType == eventType)
            .Where(e => e.OccurredAt >= DateTime.UtcNow.Subtract(timeWindow))
            .ToListAsync(ct);

        var similarEvents = new List<SimilarEvent>();

        foreach (var candidate in candidateEvents)
        {
            var score = await CalculateAttributeSimilarityScore(candidate, severity, latitude, longitude, month, fwi, ct);

            if (score > 0.3)
            {
                similarEvents.Add(new SimilarEvent
                {
                    EventId = candidate.Id,
                    Score = score,
                    Title = candidate.Title,
                    OccurredAt = candidate.OccurredAt,
                    Severity = candidate.Severity
                });
            }
        }

        return similarEvents
            .OrderByDescending(e => e.Score)
            .Take(topN)
            .ToArray();
    }

    private async Task<double> CalculateAttributeSimilarityScore(
        GeoEvent candidate,
        RiskLevel severity,
        double latitude,
        double longitude,
        int month,
        double? fwi,
        CancellationToken ct)
    {
        double score = 0;

        // Severity (weight: 25%)
        var severityDiff = Math.Abs((int)severity - (int)candidate.Severity);
        score += CalculateSeverityScoreForAttributes(severityDiff);

        // Location (weight: 30%)
        var distanceKm = CalculateDistanceKm(latitude, longitude,
                                             candidate.Geometry.Y, candidate.Geometry.X);
        score += distanceKm switch
        {
            <= 10 => 0.30,
            <= 25 => 0.20,
            <= 50 => 0.12,
            _ => 0
        };

        // Season (weight: 25%)
        score += CalculateSeasonScoreForAttributes(month, candidate.OccurredAt.Month);

        // FWI similarity if available
        if (fwi.HasValue)
        {
            score += await CalculateFwiSimilarityScoreAsync(candidate, fwi.Value, ct);
        }

        return score;
    }

    private static double CalculateSeasonScoreForAttributes(int targetMonth, int candidateMonth)
    {
        if (targetMonth == candidateMonth)
            return 0.25;
        if (GetSeason(targetMonth) == GetSeason(candidateMonth))
            return 0.12;
        return 0;
    }

    private async Task<double> CalculateFwiSimilarityScoreAsync(GeoEvent candidate, double targetFwi, CancellationToken ct)
    {
        var candidateWeather = await _db.WeatherRiskDataPoints
            .AsNoTracking()
            .Where(w => w.Timestamp >= candidate.OccurredAt.AddHours(-2) &&
                        w.Timestamp <= candidate.OccurredAt.AddHours(2))
            .OrderBy(w => Math.Abs((w.Timestamp - candidate.OccurredAt).TotalMinutes))
            .FirstOrDefaultAsync(ct);

        if (candidateWeather == null)
            return 0;

        var fwiDiff = Math.Abs(targetFwi - candidateWeather.FWI);
        return fwiDiff switch
        {
            <= 5 => 0.20,
            <= 10 => 0.12,
            <= 20 => 0.06,
            _ => 0
        };
    }

    private static double CalculateSeverityScoreForAttributes(int severityDiff)
    {
        if (severityDiff == 0) return 0.25;
        if (severityDiff == 1) return 0.12;
        return 0;
    }
}