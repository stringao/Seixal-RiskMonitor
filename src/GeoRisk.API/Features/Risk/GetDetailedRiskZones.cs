using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Risk;

/// <summary>
/// Returns detailed weather risk data for the Setubal area.
/// Used by the frontend heatmap and risk zone visualization.
/// </summary>
public sealed record GetDetailedRiskZonesQuery(string? MinRiskLevel) : IQuery<DetailedRiskZonesResponse>;

public sealed record DetailedRiskZonesResponse(
    List<RiskZoneDetail> Zones,
    DateTime LastUpdated,
    int TotalPoints);

public sealed record RiskZoneDetail(
    Guid Id,
    string RiskLevelName,
    int RiskIndex,
    double Temperature,
    double Humidity,
    double WindSpeed,
    string WindDirection,
    string Wkt,
    string Conclusion,
    DateTime CalculatedAt,
    List<DataPointDto> DataPoints);

public sealed record DataPointDto(
    double Latitude,
    double Longitude,
    double FWI,
    double Temperature,
    double Humidity,
    DateTime Timestamp);

public sealed class GetDetailedRiskZonesHandler(GeoRiskDbContext db)
    : IQueryHandler<GetDetailedRiskZonesQuery, DetailedRiskZonesResponse>
{
    public async Task<DetailedRiskZonesResponse> HandleAsync(GetDetailedRiskZonesQuery query, CancellationToken ct)
    {
        var minLevel = ParseRiskLevel(query.MinRiskLevel);

        // Get latest data points (within last hour)
        var cutoff = DateTime.UtcNow.AddHours(-1);

        var points = await db.WeatherRiskDataPoints
            .AsNoTracking()
            .Where(p => p.Timestamp >= cutoff)
            .OrderByDescending(p => p.Timestamp)
            .ToListAsync(ct);

        // If no data, generate demo zones for visualization
        if (points.Count == 0)
        {
            var demoZones = GenerateDemoZones();
            var filteredZones = minLevel.HasValue
                ? demoZones.Where(z => Enum.TryParse<RiskLevel>(z.RiskLevelName, out var l) && l >= minLevel).ToList()
                : demoZones;
            return new DetailedRiskZonesResponse(filteredZones, DateTime.UtcNow, demoZones.Count);
        }

        // Group by approximate risk zone (grid cell clustering)
        // In a production system, this would use actual polygon intersection
        // For now, we create risk zone groups based on FWI ranges
        var zones = GroupIntoRiskZones(points, minLevel);

        var lastUpdated = points.Count > 0 ? points.Max(p => p.Timestamp) : DateTime.UtcNow;

        return new DetailedRiskZonesResponse(zones, lastUpdated, points.Count);
    }

    private static List<RiskZoneDetail> GenerateDemoZones()
    {
        var zones = new List<RiskZoneDetail>();
        var now = DateTime.UtcNow;

        // Demo zone 1 - Critical area (Seixal)
        zones.Add(new RiskZoneDetail(
            Guid.NewGuid(),
            "Critical",
            42,
            35.5, 25.0, 18.0, "NE (45°)",
            "POLYGON((-9.15 38.65, -9.05 38.65, -9.05 38.75, -9.15 38.75, -9.15 38.65))",
            "Nível crítico de risco de incêndio. Ação imediata recomendada.",
            now,
            new List<DataPointDto>
            {
                new(38.70, -9.10, 42.5, 35.0, 25, now),
                new(38.72, -9.08, 41.0, 36.0, 23, now),
                new(38.68, -9.12, 40.0, 34.5, 27, now)
            }));

        // Demo zone 2 - High risk (Sesimbra)
        zones.Add(new RiskZoneDetail(
            Guid.NewGuid(),
            "High",
            28,
            32.0, 35.0, 22.0, "E (90°)",
            "POLYGON((-9.25 38.50, -9.15 38.50, -9.15 38.60, -9.25 38.60, -9.25 38.50))",
            "Risco elevado de incêndio. Vigilância máxima.",
            now,
            new List<DataPointDto>
            {
                new(38.55, -9.20, 28.0, 32.0, 35, now),
                new(38.57, -9.18, 27.0, 31.5, 37, now)
            }));

        // Demo zone 3 - Medium risk (Almada)
        zones.Add(new RiskZoneDetail(
            Guid.NewGuid(),
            "Medium",
            15,
            28.0, 55.0, 12.0, "W (270°)",
            "POLYGON((-9.20 38.65, -9.10 38.65, -9.10 38.72, -9.20 38.72, -9.20 38.65))",
            "Risco moderado. Manter atenção.",
            now,
            new List<DataPointDto>
            {
                new(38.68, -9.15, 15.0, 28.0, 55, now),
                new(38.70, -9.13, 14.5, 27.5, 57, now)
            }));

        // Demo zone 4 - Low risk (Barreiro)
        zones.Add(new RiskZoneDetail(
            Guid.NewGuid(),
            "Low",
            6,
            24.0, 70.0, 8.0, "NW (315°)",
            "POLYGON((-9.05 38.70, -8.95 38.70, -8.95 38.78, -9.05 38.78, -9.05 38.70))",
            "Risco baixo. Condições estáveis.",
            now,
            new List<DataPointDto>
            {
                new(38.74, -9.00, 6.0, 24.0, 70, now),
                new(38.76, -8.98, 5.5, 23.5, 72, now)
            }));

        return zones;
    }

    private static List<RiskZoneDetail> GroupIntoRiskZones(
        List<Domain.Entities.WeatherRiskDataPoint> points,
        RiskLevel? minLevel)
    {
        if (points.Count == 0) return new List<RiskZoneDetail>();

        // Group by risk level and geographic clustering
        var groups = points
            .Where(p => !minLevel.HasValue || p.RiskLevel >= minLevel)
            .GroupBy(p => new { p.RiskLevel, GridLat = Math.Round(p.Location.Y, 2), GridLon = Math.Round(p.Location.X, 2) })
            .Select(g =>
            {
                var avgFWI = g.Average(p => p.FWI);
                var avgTemp = g.Average(p => p.Temperature);
                var avgHum = g.Average(p => p.Humidity);
                var avgWind = g.Average(p => p.WindSpeed);
                var avgWindDir = g.Average(p => p.WindDirection);

                var riskLevelName = g.Key.RiskLevel.ToString();
                var conclusion = g.First().Conclusion ?? GenerateDefaultConclusion(g.Key.RiskLevel, avgFWI);

                // Build polygon from surrounding points
                var minLat = g.Min(p => p.Location.Y);
                var maxLat = g.Max(p => p.Location.Y);
                var minLon = g.Min(p => p.Location.X);
                var maxLon = g.Max(p => p.Location.X);

                var polygonWkt = $"POLYGON(({minLon} {minLat}, {maxLon} {minLat}, {maxLon} {maxLat}, {minLon} {maxLat}, {minLon} {minLat}))";

                return new RiskZoneDetail(
                    Guid.NewGuid(),
                    riskLevelName,
                    (int)Math.Round(avgFWI),
                    Math.Round(avgTemp, 1),
                    Math.Round(avgHum, 1),
                    Math.Round(avgWind, 1),
                    $"{CardinalDirection(avgWindDir)} ({(int)avgWindDir}°)",
                    polygonWkt,
                    conclusion,
                    g.Max(p => p.Timestamp),
                    g.Select(p => new DataPointDto(
                        p.Location.Y, p.Location.X,
                        Math.Round(p.FWI, 1),
                        Math.Round(p.Temperature, 1),
                        Math.Round(p.Humidity, 1),
                        p.Timestamp)).ToList());
            })
            .OrderByDescending(z => z.RiskIndex)
            .ToList();

        return groups;
    }

    private static string GenerateDefaultConclusion(RiskLevel level, double fwi)
    {
        return level switch
        {
            RiskLevel.Critical => $"Nível crítico de risco de incêndio (FWI {fwi:F0}). Ação imediata recomendada.",
            RiskLevel.High => $"Risco elevado de incêndio (FWI {fwi:F0}). Vigilância máxima.",
            RiskLevel.Medium => $"Risco moderado (FWI {fwi:F0}). Manter atenção.",
            _ => $"Risco baixo (FWI {fwi:F0}). Condições estáveis."
        };
    }

    private static string CardinalDirection(double degrees)
    {
        var dirs = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        var index = (int)Math.Round(degrees / 45.0) % 8;
        return dirs[index];
    }

    private static RiskLevel? ParseRiskLevel(string? level)
    {
        if (string.IsNullOrEmpty(level))
            return null;
        return Enum.TryParse<RiskLevel>(level, ignoreCase: true, out var result) ? result : null;
    }
}

public static class GetDetailedRiskZonesEndpoint
{
    public static RouteGroupBuilder MapGetDetailedRiskZones(this RouteGroupBuilder group)
    {
        group.MapGet("/zones/detailed", async (
            string? minRiskLevel,
            IQueryHandler<GetDetailedRiskZonesQuery, DetailedRiskZonesResponse> h) =>
        {
            var result = await h.HandleAsync(new GetDetailedRiskZonesQuery(minRiskLevel), default);
            return Results.Ok(result);
        })
        .WithDescription("Get detailed weather risk zones for the Setubal area with FWI calculations")
        .WithTags("Risk");

        return group;
    }
}