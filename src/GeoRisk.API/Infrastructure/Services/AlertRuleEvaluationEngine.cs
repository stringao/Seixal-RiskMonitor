using System.Text.RegularExpressions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

public class AlertRuleEvaluationEngine
{
    private readonly GeoRiskDbContext _db;
    private readonly ILogger<AlertRuleEvaluationEngine> _logger;

    public AlertRuleEvaluationEngine(GeoRiskDbContext db, ILogger<AlertRuleEvaluationEngine> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<EvaluatedAlert>> EvaluateAllRulesAsync(CancellationToken ct = default)
    {
        var activeRules = await _db.AlertRules
            .Where(r => r.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        var recentEvents = await _db.GeoEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .AsNoTracking()
            .ToListAsync(ct);

        var weatherData = await _db.WeatherRiskDataPoints
            .Where(w => w.Timestamp >= DateTime.UtcNow.AddHours(-24))
            .AsNoTracking()
            .ToListAsync(ct);

        var results = new List<EvaluatedAlert>();

        foreach (var rule in activeRules)
        {
            var triggeredAlerts = await EvaluateRuleAsync(rule, recentEvents, weatherData, ct);
            results.AddRange(triggeredAlerts);
        }

        return results;
    }

    public async Task<List<EvaluatedAlert>> EvaluateRuleAsync(
        AlertRule rule,
        List<GeoEvent> events,
        List<WeatherRiskDataPoint> weatherData,
        CancellationToken ct = default)
    {
        var results = new List<EvaluatedAlert>();

        // Check season constraint
        if (!IsInSeason(rule))
        {
            _logger.LogDebug("Rule {RuleName} skipped: outside season", rule.Name);
            return results;
        }

        var matchingEvents = GetMatchingEvents(rule, events, weatherData);

        // Check consecutive count requirement
        if (rule.ConsecutiveCount.HasValue && rule.ConsecutiveCount > 1)
        {
            var consecutiveGroups = GroupConsecutiveEvents(matchingEvents);
            foreach (var group in consecutiveGroups)
            {
                if (group.Count >= rule.ConsecutiveCount.Value)
                {
                    var eventToAlert = group[group.Count - 1];
                    results.Add(CreateEvaluatedAlert(rule, eventToAlert, weatherData));
                }
            }
        }
        else
        {
            foreach (var evt in matchingEvents)
            {
                results.Add(CreateEvaluatedAlert(rule, evt, weatherData));
            }
        }

        return results;
    }

    private static bool IsInSeason(AlertRule rule)
    {
        if (!rule.SeasonStartMonth.HasValue || !rule.SeasonEndMonth.HasValue)
            return true; // No season constraint

        var currentMonth = DateTime.UtcNow.Month;

        if (rule.SeasonStartMonth <= rule.SeasonEndMonth)
        {
            // Normal range: e.g., June (6) to September (9)
            return currentMonth >= rule.SeasonStartMonth && currentMonth <= rule.SeasonEndMonth;
        }
        else
        {
            // Crosses year boundary: e.g., November (11) to February (2)
            return currentMonth >= rule.SeasonStartMonth || currentMonth <= rule.SeasonEndMonth;
        }
    }

    private static List<GeoEvent> GetMatchingEvents(
        AlertRule rule,
        List<GeoEvent> events,
        List<WeatherRiskDataPoint> weatherData)
    {
        return events.Where(e => EvaluateEventConditions(rule, e, weatherData)).ToList();
    }

    private static bool EvaluateEventConditions(
        AlertRule rule,
        GeoEvent evt,
        List<WeatherRiskDataPoint> weatherData)
    {
        if (!MatchesEventType(rule, evt)) return false;
        if (!MeetsSeverityThreshold(rule, evt)) return false;
        if (!IsWithinArea(rule, evt)) return false;
        if (!MeetsFwiCriteria(rule, evt, weatherData)) return false;
        if (!MeetsWindSpeedCriteria(rule, evt, weatherData)) return false;
        if (!MeetsTemperatureCriteria(rule, evt, weatherData)) return false;
        if (!MeetsAreaCriteria(rule, evt)) return false;

        return true;
    }

    private static bool MatchesEventType(AlertRule rule, GeoEvent evt)
    {
        return !rule.EventType.HasValue || rule.EventType == evt.EventType;
    }

    private static bool MeetsSeverityThreshold(AlertRule rule, GeoEvent evt)
    {
        return !rule.SeverityThreshold.HasValue || evt.Severity >= rule.SeverityThreshold;
    }

    private static bool IsWithinArea(AlertRule rule, GeoEvent evt)
    {
        if (rule.Area == null || evt.Geometry == null)
            return true;

        return rule.Area.Contains(evt.Geometry) || rule.Area.Intersects(evt.Geometry);
    }

    private static bool MeetsFwiCriteria(AlertRule rule, GeoEvent evt, List<WeatherRiskDataPoint> weatherData)
    {
        if (!rule.MinFwi.HasValue && !rule.MaxFwi.HasValue)
            return true;

        var eventFwi = GetEventFwi(evt, weatherData);
        if (!eventFwi.HasValue)
            return false;

        if (rule.MinFwi.HasValue && eventFwi < rule.MinFwi)
            return false;
        if (rule.MaxFwi.HasValue && eventFwi > rule.MaxFwi)
            return false;

        return true;
    }

    private static bool MeetsWindSpeedCriteria(AlertRule rule, GeoEvent evt, List<WeatherRiskDataPoint> weatherData)
    {
        if (!rule.MinWindSpeed.HasValue)
            return true;

        var eventWind = GetEventWindSpeed(evt, weatherData);
        return eventWind.HasValue && eventWind >= rule.MinWindSpeed;
    }

    private static bool MeetsTemperatureCriteria(AlertRule rule, GeoEvent evt, List<WeatherRiskDataPoint> weatherData)
    {
        if (!rule.MinTemperature.HasValue)
            return true;

        var eventTemp = GetEventTemperature(evt, weatherData);
        return eventTemp.HasValue && eventTemp >= rule.MinTemperature;
    }

    private static bool MeetsAreaCriteria(AlertRule rule, GeoEvent evt)
    {
        if (!rule.AreaKm2Threshold.HasValue)
            return true;

        var eventArea = GetEventAreaKm2(evt);
        return eventArea.HasValue && eventArea >= rule.AreaKm2Threshold;
    }

    private static double? GetEventFwi(GeoEvent evt, List<WeatherRiskDataPoint> weatherData)
    {
        if (evt.Geometry == null) return null;

        var nearestWeather = weatherData
            .Where(w => w.Location != null)
            .OrderBy(w => CalculateDistance(evt.Geometry.Y, evt.Geometry.X, w.Location.Y, w.Location.X))
            .FirstOrDefault();

        return nearestWeather?.FWI;
    }

    private static double? GetEventWindSpeed(GeoEvent evt, List<WeatherRiskDataPoint> weatherData)
    {
        if (evt.Geometry == null) return null;

        var nearestWeather = weatherData
            .Where(w => w.Location != null)
            .OrderBy(w => CalculateDistance(evt.Geometry.Y, evt.Geometry.X, w.Location.Y, w.Location.X))
            .FirstOrDefault();

        // Parse wind speed from metadata if available
        if (!string.IsNullOrEmpty(evt.Metadata) && double.TryParse(ExtractMetadataValue(evt.Metadata, "windSpeed"), out var wind))
            return wind;

        return nearestWeather?.WindSpeed;
    }

    private static double? GetEventTemperature(GeoEvent evt, List<WeatherRiskDataPoint> weatherData)
    {
        if (evt.Geometry == null) return null;

        var nearestWeather = weatherData
            .Where(w => w.Location != null)
            .OrderBy(w => CalculateDistance(evt.Geometry.Y, evt.Geometry.X, w.Location.Y, w.Location.X))
            .FirstOrDefault();

        return nearestWeather?.Temperature;
    }

    private static double? GetEventAreaKm2(GeoEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Metadata))
            return null;

        if (double.TryParse(ExtractMetadataValue(evt.Metadata, "areaKm2"), out var area))
            return area;

        return null;
    }

    private static string? ExtractMetadataValue(string metadata, string key)
    {
        // Simple JSON parsing for metadata
        var pattern = $"\"{key}\"\\s*:\\s*([\\d.]+)";
        var match = System.Text.RegularExpressions.Regex.Match(metadata, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        return match.Success ? match.Groups[1].Value : null;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double R = 6371; // Earth radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static List<List<GeoEvent>> GroupConsecutiveEvents(List<GeoEvent> events)
    {
        if (events.Count == 0) return new List<List<GeoEvent>>();

        var sorted = events.OrderBy(e => e.OccurredAt).ToList();
        var groups = new List<List<GeoEvent>>();
        var currentGroup = new List<GeoEvent> { sorted[0] };

        for (int i = 1; i < sorted.Count; i++)
        {
            var timeDiff = (sorted[i].OccurredAt - sorted[i - 1].OccurredAt).TotalMinutes;
            if (timeDiff <= 60) // Events within 1 hour are considered consecutive
            {
                currentGroup.Add(sorted[i]);
            }
            else
            {
                groups.Add(currentGroup);
                currentGroup = new List<GeoEvent> { sorted[i] };
            }
        }
        groups.Add(currentGroup);
        return groups;
    }

    private static EvaluatedAlert CreateEvaluatedAlert(AlertRule rule, GeoEvent evt, List<WeatherRiskDataPoint> weatherData)
    {
        var fwi = GetEventFwi(evt, weatherData);
        var wind = GetEventWindSpeed(evt, weatherData);
        var temp = GetEventTemperature(evt, weatherData);
        var area = GetEventAreaKm2(evt);

        return new EvaluatedAlert
        {
            GeoEvent = evt,
            AlertRule = rule,
            FwiValue = fwi,
            WindSpeed = wind,
            Temperature = temp,
            AreaKm2 = area,
            SuggestedSeverity = DetermineAlertSeverity(rule, evt)
        };
    }

    private static AlertSeverity DetermineAlertSeverity(AlertRule rule, GeoEvent evt)
    {
        // Base severity on rule's severity threshold or event severity
        if (rule.SeverityThreshold.HasValue)
        {
            return rule.SeverityThreshold.Value switch
            {
                RiskLevel.Critical => AlertSeverity.Critical,
                RiskLevel.High => AlertSeverity.Danger,
                RiskLevel.Medium => AlertSeverity.Warning,
                RiskLevel.Low => AlertSeverity.Info,
                _ => AlertSeverity.Warning
            };
        }

        return evt.Severity switch
        {
            RiskLevel.Critical => AlertSeverity.Critical,
            RiskLevel.High => AlertSeverity.Danger,
            RiskLevel.Medium => AlertSeverity.Warning,
            RiskLevel.Low => AlertSeverity.Info,
            _ => AlertSeverity.Warning
        };
    }

    public async Task<List<EvaluatedAlert>> CheckEscalationsAsync(CancellationToken ct = default)
    {
        var escalatedAlerts = new List<EvaluatedAlert>();

        // Find recent alerts that have associated rules with escalation
        var recentAlerts = await _db.Alerts
            .Include(a => a.AlertRule)
            .Where(a => a.CreatedAt >= DateTime.UtcNow.AddHours(-24) && !a.IsEscalated && a.AlertRule != null)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var alert in recentAlerts)
        {
            var rule = alert.AlertRule!;
            if (!rule.EscalationMinutes.HasValue || rule.EscalationMinutes <= 0)
                continue;

            // Check if conditions have persisted for the escalation period
            var timeSinceAlert = (DateTime.UtcNow - alert.CreatedAt).TotalMinutes;
            if (timeSinceAlert >= rule.EscalationMinutes)
            {
                // Check if there are still matching events in the area
                var stillMatching = await HasPersistentConditionsAsync(rule, alert.GeoEventId, ct);
                if (stillMatching)
                {
                    var escalatedSeverity = EscalateSeverity(alert.Severity);
                    escalatedAlerts.Add(new EvaluatedAlert
                    {
                        GeoEvent = alert.GeoEvent,
                        AlertRule = rule,
                        IsEscalation = true,
                        EscalatedFromAlertId = alert.Id,
                        SuggestedSeverity = escalatedSeverity
                    });
                }
            }
        }

        return escalatedAlerts;
    }

    private async Task<bool> HasPersistentConditionsAsync(AlertRule rule, Guid? geoEventId, CancellationToken ct)
    {
        if (geoEventId == null) return false;

        var originalEvent = await _db.GeoEvents.FindAsync(new object[] { geoEventId.Value }, ct);
        if (originalEvent == null) return false;

        var recentEvents = await _db.GeoEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .AsNoTracking()
            .ToListAsync(ct);

        var weatherData = await _db.WeatherRiskDataPoints
            .Where(w => w.Timestamp >= DateTime.UtcNow.AddHours(-1))
            .AsNoTracking()
            .ToListAsync(ct);

        var stillMatching = GetMatchingEvents(rule, recentEvents, weatherData);
        return stillMatching.Count > 0;
    }

    private static AlertSeverity EscalateSeverity(AlertSeverity current)
    {
        return current switch
        {
            AlertSeverity.Info => AlertSeverity.Warning,
            AlertSeverity.Warning => AlertSeverity.Danger,
            AlertSeverity.Danger => AlertSeverity.Critical,
            AlertSeverity.Critical => AlertSeverity.Critical,
            _ => current
        };
    }
}

public class EvaluatedAlert
{
    public GeoEvent? GeoEvent { get; set; }
    public AlertRule? AlertRule { get; set; }
    public double? FwiValue { get; set; }
    public double? WindSpeed { get; set; }
    public double? Temperature { get; set; }
    public double? AreaKm2 { get; set; }
    public AlertSeverity SuggestedSeverity { get; set; }
    public bool IsEscalation { get; set; }
    public Guid? EscalatedFromAlertId { get; set; }
}
