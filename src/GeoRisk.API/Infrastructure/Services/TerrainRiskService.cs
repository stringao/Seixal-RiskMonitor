using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.ExternalApis;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Combines terrain (SRTM) and weather (FWI) data for comprehensive fire risk assessment.
/// Terrain factors modify the base FWI risk:
/// - Steep slopes = faster fire spread, harder to fight
/// - South-facing = drier microclimate, higher risk
/// - North-facing = moisture retention, lower risk
/// - High elevation = wind exposure, lower humidity
/// </summary>
public sealed class TerrainRiskService
{
    private readonly SrtmTerrainService _terrainService;

    public TerrainRiskService(SrtmTerrainService terrainService)
    {
        _terrainService = terrainService;
    }

    /// <summary>
    /// Calculate comprehensive risk score combining terrain and weather.
    /// </summary>
    public async Task<TerrainWeatherRiskResult> CalculateCombinedRiskAsync(
        double latitude,
        double longitude,
        OpenMeteoWeather weather)
    {
        // Get terrain analysis
        var terrain = await _terrainService.GetTerrainAnalysisAsync(latitude, longitude);

        // Calculate FWI
        var fwi = FwiCalculator.Calculate(weather);

        if (terrain == null)
        {
            // No terrain data - fall back to FWI only
            return new TerrainWeatherRiskResult
            {
                Latitude = latitude,
                Longitude = longitude,
                FWI = fwi.FWI,
                TerrainRiskScore = 50, // Default moderate
                CombinedRiskScore = fwi.FWI,
                RiskLevel = FwiCalculator.GetDangerRating(fwi.FWI),
                TerrainAnalysis = null,
                CalculatedAt = DateTime.UtcNow
            };
        }

        // Calculate combined risk
        var terrainRiskScore = terrain.TerrainRiskScore;
        var combinedRisk = SrtmTerrainService.CalculateCombinedRisk(terrainRiskScore, fwi.FWI);

        // Determine overall risk level
        var riskLevel = FwiCalculator.GetDangerRating(combinedRisk);

        return new TerrainWeatherRiskResult
        {
            Latitude = latitude,
            Longitude = longitude,
            Elevation = terrain.ElevationMeters,
            Slope = terrain.SlopeDegrees,
            Aspect = terrain.AspectDegrees,
            FWI = fwi.FWI,
            ISI = fwi.ISI,
            BUI = fwi.BUI,
            TerrainRiskScore = terrainRiskScore,
            CombinedRiskScore = combinedRisk,
            AdjustedRosKmh = 0,
            RiskLevel = riskLevel,
            RiskContributingFactors = GetContributingFactors(terrain, weather, fwi),
            TerrainAnalysis = terrain,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private static List<string> GetContributingFactors(
        TerrainAnalysis terrain,
        OpenMeteoWeather weather,
        FwiResult fwi)
    {
        var factors = new List<string>();

        // Terrain factors
        if (terrain.SlopeDegrees >= 30)
            factors.Add("Steep terrain (>30°) significantly increases fire behavior");
        else if (terrain.SlopeDegrees >= 20)
            factors.Add("Moderate slope increases uphill fire spread");

        if (terrain.AspectDegrees >= 135 && terrain.AspectDegrees < 225)
            factors.Add("South-facing aspect: maximum solar exposure, drier conditions");

        if (terrain.AspectDegrees >= 315 || terrain.AspectDegrees < 45)
            factors.Add("North-facing: higher moisture retention, lower fire risk");

        if (terrain.ElevationMeters > 500)
            factors.Add("High elevation increases wind exposure");

        if (terrain.TerrainComplexity == "High")
            factors.Add("Complex terrain may hinder firefighting operations");

        // Weather factors
        if (fwi.FWI >= 30)
            factors.Add($"Very high FWI ({fwi.FWI:F0}) indicates extreme fire conditions");

        if (weather.TemperatureCelsius >= 35)
            factors.Add("High temperature accelerates fuel drying");

        if (weather.RelativeHumidityPercent <= 20)
            factors.Add("Very low humidity increases fire intensity");

        if (weather.WindSpeedKmh >= 40)
            factors.Add("Strong winds promote rapid fire spread");

        return factors;
    }
}

/// <summary>
/// Result of combined terrain + weather risk calculation.
/// </summary>
public sealed record TerrainWeatherRiskResult
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double Elevation { get; init; }
    public double Slope { get; init; }
    public double Aspect { get; init; }
    public double FWI { get; init; }
    public double ISI { get; init; }
    public double BUI { get; init; }
    public double TerrainRiskScore { get; init; }
    public double CombinedRiskScore { get; init; }
    public double AdjustedRosKmh { get; init; }
    public string RiskLevel { get; init; } = "Low";
    public List<string> RiskContributingFactors { get; init; } = new();
    public TerrainAnalysis? TerrainAnalysis { get; init; }
    public DateTime CalculatedAt { get; init; }
}
