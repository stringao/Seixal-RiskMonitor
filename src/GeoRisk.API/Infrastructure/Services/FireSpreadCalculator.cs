using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.ExternalApis;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Calculates fire spread predictions using FARSITE-style ellipse model.
/// Based on Rate of Spread (ROS) from FWI system, wind direction, terrain, and fuel factors.
/// </summary>
public sealed class FireSpreadCalculator
{
    private readonly GeometryFactory _factory;
    private readonly SrtmTerrainService? _terrainService;

    public FireSpreadCalculator(
        GeometryFactory geometryFactory,
        SrtmTerrainService? terrainService = null)
    {
        _factory = geometryFactory;
        _terrainService = terrainService;
    }

    /// <summary>
    /// Calculate spread predictions for all horizons (1, 2, 4, 8, 12 hours)
    /// for a given fire event with current weather conditions.
    /// </summary>
    public List<FireSpreadPrediction> CalculateSpread(
        GeoEvent fireEvent,
        double latitude,
        double longitude,
        OpenMeteoWeather weather,
        FwiResult fwi,
        FireSpreadScenario scenario = FireSpreadScenario.Moderate)
    {
        var ros = CalculateROS(fwi, weather.WindSpeedKmh, weather.WindDirectionDegrees);

        // Apply terrain factors if available
        if (_terrainService != null)
        {
            var terrain = _terrainService.GetTerrainAnalysisAsync(latitude, longitude).GetAwaiter().GetResult();
            if (terrain != null)
            {
                ros = SrtmTerrainService.AdjustSpreadRateForTerrain(
                    ros,
                    terrain.SlopeDegrees,
                    terrain.AspectDegrees,
                    weather.WindDirectionDegrees);
            }
        }

        var windDirRad = weather.WindDirectionDegrees * Math.PI / 180.0;

        // Apply scenario factor
        var scenarioFactor = scenario switch
        {
            FireSpreadScenario.Optimist => 0.6,
            FireSpreadScenario.Pessimist => 1.8,
            _ => 1.0
        };

        var effectiveRos = ros * scenarioFactor;

        var predictions = new List<FireSpreadPrediction>();
        var horizons = new[] { 1, 2, 4, 8, 12 };

        foreach (var hours in horizons)
        {
            var ellipse = BuildEllipse(
                latitude,
                longitude,
                effectiveRos,
                hours,
                windDirRad);

            var areaKm2 = CalculateAreaKm2(ellipse);
            var municipalities = GetAffectedMunicipalities();
            var conclusion = GenerateConclusion(hours, areaKm2, municipalities, effectiveRos, scenario);

            predictions.Add(new FireSpreadPrediction
            {
                Id = Guid.NewGuid(),
                FireEventId = fireEvent.Id,
                HorizonHours = hours,
                Polygon = ellipse,
                RosKmh = Math.Round(effectiveRos, 2),
                AreaKm2 = Math.Round(areaKm2, 2),
                AffectedMunicipalities = municipalities,
                Scenario = scenario,
                Conclusion = conclusion,
                CalculatedAt = DateTime.UtcNow,
                FWI = Math.Round(fwi.FWI, 1),
                ISI = Math.Round(fwi.ISI, 1),
                Temperature = Math.Round(weather.TemperatureCelsius, 1),
                Humidity = Math.Round(weather.RelativeHumidityPercent, 1),
                WindSpeed = Math.Round(weather.WindSpeedKmh, 1),
                WindDirection = Math.Round(weather.WindDirectionDegrees, 0)
            });
        }

        return predictions;
    }

    /// <summary>
    /// Calculate Rate of Spread in km/h using ISI and environmental factors.
    /// Based on FARSITE ROS formula: ROS = ISI × SlopeFactor × WindFactor × FuelFactor
    /// </summary>
    public static double CalculateROS(FwiResult fwi, double windSpeedKmh, double windDirectionDegrees)
    {
        // ISI is the Initial Spread Index - base ROS without other factors
        var baseRos = fwi.ISI;

        // Wind factor: higher wind = faster spread (diminishing returns at high speeds)
        var windFactor = 1.0 + (windSpeedKmh / 30.0) * 0.20;
        windFactor = Math.Min(windFactor, 2.5);

        // ISI contribution to ROS (simplified FARSITE formula)
        // ROS (km/h) = 0.067 * ISI^1.37 * wind_factor (approximation)
        var ros = 0.067 * Math.Pow(baseRos, 1.37) * windFactor;

        return Math.Max(ros, 0.1); // minimum spread of 0.1 km/h
    }

    /// <summary>
    /// Build an ellipse polygon representing fire spread over time.
    /// Uses FARSITE-style model where:
    /// - Fire origin is at the BACK of the ellipse (leeward side)
    /// - Fire head spreads in the WIND DIRECTION (downwind)
    /// - Spread is asymmetric: faster forward (70%) than backward (30%)
    /// </summary>
    public Polygon BuildEllipse(
        double centerLat,
        double centerLon,
        double rosKmh,
        int hours,
        double windDirectionRadians)
    {
        // Calculate total ellipse dimensions
        // Semi-major axis: total distance fire travels in wind direction
        var semiMajor = rosKmh * hours;

        // Semi-minor axis: ~55-65% of major axis (typical fire shape)
        var semiMinor = semiMajor * 0.60;

        // Fire origin is at the back of the ellipse
        // Forward spread (in wind direction): 70%
        // Backward spread (against wind): 30%
        var backDistance = semiMajor * 0.30;

        // Direction vector (wind direction = fire spread direction)
        var dirX = Math.Cos(windDirectionRadians); // East component
        var dirY = Math.Sin(windDirectionRadians); // North component

        // Fire origin at center of ellipse (we'll offset the center later if needed)
        var originX = centerLon;
        var originY = centerLat;

        // Generate ellipse points
        var points = new List<Coordinate>();
        const int segments = 48;

        for (int i = 0; i <= segments; i++)
        {
            var angle = 2.0 * Math.PI * i / segments;

            // Ellipse parametric formula
            var rx = semiMajor * Math.Cos(angle);
            var ry = semiMinor * Math.Sin(angle);

            // Rotate to wind direction
            var x = rx * dirX - ry * dirY;
            var y = rx * dirY + ry * dirX;

            // Convert km to degrees (approximate: 1 degree ≈ 111 km)
            var dLon = x / 111.0;
            var dLat = y / 111.0;

            // The fire origin is at the back of the ellipse, so we shift the point
            // back by the backDistance amount to place the origin correctly
            var shiftedLon = originX + dLon - (backDistance / 111.0 * dirX);
            var shiftedLat = originY + dLat - (backDistance / 111.0 * dirY);

            points.Add(new Coordinate(shiftedLon, shiftedLat));
        }

        // Ensure closed polygon
        if (points.Count > 0 && !points[0].Equals2D(points[^1]))
        {
            points.Add(points[0]);
        }

        try
        {
            var shell = new LinearRing(points.ToArray());
            return _factory.CreatePolygon(shell);
        }
        catch
        {
            // Fallback: create a simple circle buffer around center
            var center = _factory.CreatePoint(new Coordinate(centerLon, centerLat));
            return center.Buffer(semiMajor / 111.0 * 0.5) as Polygon
                ?? _factory.CreatePolygon(new[]
                {
                    new Coordinate(centerLon - 0.01, centerLat - 0.01),
                    new Coordinate(centerLon + 0.01, centerLat - 0.01),
                    new Coordinate(centerLon + 0.01, centerLat + 0.01),
                    new Coordinate(centerLon - 0.01, centerLat + 0.01),
                    new Coordinate(centerLon - 0.01, centerLat - 0.01)
                });
        }
    }

    private static double CalculateAreaKm2(Polygon polygon)
    {
        // Convert square degrees to km²
        // 1 degree ≈ 111 km at equator, so 1 sq degree ≈ 12321 km²
        var areaDegrees = polygon.Area;
        var latMidpoint = polygon.Centroid.Y;
        var kmPerDegreeLat = 111.0;
        var kmPerDegreeLon = 111.0 * Math.Cos(latMidpoint * Math.PI / 180.0);
        return areaDegrees * kmPerDegreeLat * kmPerDegreeLon;
    }

    private static List<string> GetAffectedMunicipalities()
    {
        // This would use the OGC API to find municipalities that intersect with the ellipse
        // For now, return empty list - will be populated in the job with actual intersection
        return new List<string>();
    }

    private static string GenerateConclusion(int hours, double areaKm2, List<string> municipalities, double ros, FireSpreadScenario scenario)
    {
        var scenarioLabel = scenario switch
        {
            FireSpreadScenario.Optimist => "Cenário otimista",
            FireSpreadScenario.Pessimist => "Cenário pessimista",
            _ => "Cenário moderado"
        };

        var muniText = municipalities.Count > 0
            ? $". Impacto em: {string.Join(", ", municipalities)}"
            : "";

        return hours switch
        {
            <= 1 => $"{scenarioLabel}: fogo cobre {areaKm2:F1} km² em {hours}h (ROS {ros:F1} km/h). Propagation moderada.{muniText}",
            <= 4 => $"{scenarioLabel}: área de {areaKm2:F1} km² afetada em {hours}h. Potential for spread to nearby areas.{muniText}",
            <= 8 => $"{scenarioLabel}: fogo abrange {areaKm2:F1} km² em {hours}h. Risk elevat for extended impact.{muniText}",
            _ => $"{scenarioLabel}: scenario cr\u00edtico. {areaKm2:F1} km² em {hours}h. Evacua\u00e7\u00e3o preventiva recomendada.{muniText}"
        };
    }
}