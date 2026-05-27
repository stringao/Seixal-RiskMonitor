using System.Globalization;
using System.Text.RegularExpressions;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Alerts;

public sealed record GetRuleConditionsQuery : IQuery<RuleConditionsResponse>;

public sealed class GetRuleConditionsHandler(
    GeoRiskDbContext db) : IQueryHandler<GetRuleConditionsQuery, RuleConditionsResponse>
{
    private const string DoubleType = "double";

    public async Task<RuleConditionsResponse> HandleAsync(GetRuleConditionsQuery query, CancellationToken ct)
    {
        var conditions = new List<RuleConditionInfo>
        {
            new("EventType", "Type of geographic event (Fire, Flood, Storm, etc.)", "enum", null!, false, null, null),
            new("SeverityThreshold", "Minimum risk severity level", "enum", null!, false, null, null),
            new("Area", "Geographic polygon area (WKT format)", "polygon", null!, false, null, null),
            new("MinFwi", "Minimum FWI (Fire Weather Index) value", DoubleType, null!, true, 0, 100),
            new("MaxFwi", "Maximum FWI (Fire Weather Index) value", DoubleType, null!, true, 0, 100),
            new("MinWindSpeed", "Minimum wind speed in km/h", DoubleType, "km/h", true, 0, 200),
            new("MinTemperature", "Minimum temperature in Celsius", DoubleType, "°C", true, -20, 60),
            new("SeasonStartMonth", "Start of season (1-12)", "int", null!, false, 1, 12),
            new("SeasonEndMonth", "End of season (1-12)", "int", null!, false, 1, 12),
            new("AreaKm2Threshold", "Minimum fire area in square kilometers", DoubleType, "km²", true, 0, 10000),
            new("ConsecutiveCount", "Number of consecutive events to trigger", "int", null!, true, 1, 100),
            new("EscalationMinutes", "Minutes before escalating severity", "int", "minutes", true, 0, 1440),
            new("NotifyRoles", "Roles to notify (e.g., FireChief, CivilProtection)", "string[]", null!, false, null, null)
        };

        var currentValues = await GetCurrentConditionValuesAsync(db, ct);

        return new RuleConditionsResponse(conditions, currentValues);
    }

    private static async Task<CurrentConditionValues> GetCurrentConditionValuesAsync(GeoRiskDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var recentEvents = await db.GeoEvents
            .Where(e => e.CreatedAt >= now.AddHours(-24))
            .AsNoTracking()
            .ToListAsync(ct);

        double? currentFwi = null;
        double? currentWindSpeed = null;
        double? currentTemperature = null;
        double? currentFireAreaKm2 = null;

        // Get latest weather data
        var latestWeather = await db.WeatherRiskDataPoints
            .Where(w => w.Timestamp >= now.AddHours(-24))
            .OrderByDescending(w => w.Timestamp)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (latestWeather != null)
        {
            currentFwi = latestWeather.FWI;
            currentWindSpeed = latestWeather.WindSpeed;
            currentTemperature = latestWeather.Temperature;
        }

        // Calculate current fire area from recent fire events
        var fireEvents = recentEvents.Where(e => e.EventType == EventType.Fire).ToList();
        var areas = fireEvents
            .Where(e => !string.IsNullOrEmpty(e.Metadata))
            .Select(e => ExtractMetadataValue(e.Metadata!, "areaKm2"))
            .Where(areaStr => !string.IsNullOrEmpty(areaStr) && double.TryParse(areaStr, out var a) && a > 0)
            .Select(areaStr => double.Parse(areaStr!, CultureInfo.InvariantCulture))
            .ToList();
        currentFireAreaKm2 = areas.Count > 0 ? areas.Max() : 0;

        var currentSeason = DetermineSeason(now.Month);

        return new CurrentConditionValues(currentFwi, currentWindSpeed, currentTemperature, currentFireAreaKm2, currentSeason);
    }

    private static string DetermineSeason(int month)
    {
        return month switch
        {
            3 or 4 or 5 => "Spring",
            6 or 7 or 8 => "Summer",
            9 or 10 or 11 => "Autumn",
            _ => "Winter"
        };
    }

    private static string? ExtractMetadataValue(string metadata, string key)
    {
        var pattern = $"\"{key}\"\\s*:\\s*([\\d.]+)";
        var match = System.Text.RegularExpressions.Regex.Match(metadata, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        return match.Success ? match.Groups[1].Value : null;
    }
}

public static class GetRuleConditionsEndpoint
{
    public static RouteGroupBuilder MapGetRuleConditions(this RouteGroupBuilder group)
    {
        group.MapGet("/rules/conditions", async (
            IQueryHandler<GetRuleConditionsQuery, RuleConditionsResponse> handler) =>
        {
            var result = await handler.HandleAsync(new GetRuleConditionsQuery(), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin");

        return group;
    }
}
