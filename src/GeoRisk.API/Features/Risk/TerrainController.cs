using GeoRisk.API.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeoRisk.API.Features.Risk;

public sealed record TerrainAnalysisResponse(
    Guid Id,
    double Latitude,
    double Longitude,
    double ElevationMeters,
    double SlopeDegrees,
    double AspectDegrees,
    double SolarExposureIndex,
    string TerrainComplexity,
    double NorthFacingPercent,
    double TerrainRiskScore,
    string TerrainFireRiskContribution,
    DateTime CalculatedAt
);

public sealed record TerrainHeatmapResponse(
    string Type,
    List<TerrainHeatmapFeature> Features
);

public sealed record TerrainHeatmapFeature(
    string Type,
    TerrainHeatmapGeometry Geometry,
    TerrainHeatmapProperties Properties
);

public sealed record TerrainHeatmapGeometry(
    string Type,
    double[] Coordinates
);

public sealed record TerrainHeatmapProperties(
    double Risk
);

public sealed record TerrainRiskScoreResponse(
    double Latitude,
    double Longitude,
    double TerrainRiskScore,
    string RiskLevel,
    double CombinedRiskScore,
    double? AdjustedRosKmh
);

public static class TerrainEndpoints
{
    /// <summary>
    /// Get terrain analysis for a specific point.
    /// GET /api/risk/terrain/{lat}/{lon}
    /// </summary>
    public static async Task<IResult> GetTerrainAnalysis(
        double lat,
        double lon,
        SrtmTerrainService terrainService)
    {
        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
        {
            return Results.BadRequest(new { error = "Invalid coordinates" });
        }

        var analysis = await terrainService.GetTerrainAnalysisAsync(lat, lon);
        if (analysis == null)
        {
            return Results.NotFound(new { error = "Terrain data not available for this location. Run SRTM processing first." });
        }

        var response = new TerrainAnalysisResponse(
            analysis.Id,
            analysis.Latitude,
            analysis.Longitude,
            analysis.ElevationMeters,
            analysis.SlopeDegrees,
            analysis.AspectDegrees,
            analysis.SolarExposureIndex,
            analysis.TerrainComplexity,
            analysis.NorthFacingPercent,
            analysis.TerrainRiskScore,
            analysis.TerrainFireRiskContribution,
            analysis.CalculatedAt
        );

        return Results.Ok(response);
    }

    /// <summary>
    /// Get terrain risk heatmap as GeoJSON for the region.
    /// GET /api/risk/terrain/heatmap
    /// </summary>
    public static async Task<IResult> GetTerrainHeatmap(SrtmTerrainService terrainService)
    {
        if (!terrainService.IsAvailable)
        {
            return Results.NotFound(new { error = "Terrain data not available. Run SRTM processing first." });
        }

        var geoJson = await terrainService.GetTerrainRiskHeatmapAsync();
        if (geoJson == "{}")
        {
            return Results.NotFound(new { error = "Failed to generate heatmap" });
        }

        return Results.Ok(geoJson);
    }

    /// <summary>
    /// Get terrain risk score for a point (simplified endpoint).
    /// GET /api/risk/terrain/score/{lat}/{lon}
    /// </summary>
    public static async Task<IResult> GetTerrainRiskScore(
        double lat,
        double lon,
        SrtmTerrainService terrainService)
    {
        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
        {
            return Results.BadRequest(new { error = "Invalid coordinates" });
        }

        var terrainRiskScore = await terrainService.GetTerrainRiskScoreAsync(lat, lon);
        var riskLevel = terrainRiskScore switch
        {
            >= 70 => "Extreme",
            >= 50 => "High",
            >= 30 => "Medium",
            _ => "Low"
        };

        return Results.Ok(new TerrainRiskScoreResponse(
            lat, lon, terrainRiskScore, riskLevel, terrainRiskScore, null
        ));
    }

    /// <summary>
    /// Get terrain slope visualization data as GeoJSON.
    /// Returns points with slope values and categories for map overlay.
    /// GET /api/risk/terrain/slope
    /// </summary>
    public static async Task<IResult> GetSlopeVisualization(SrtmTerrainService terrainService)
    {
        if (!terrainService.IsAvailable)
        {
            return Results.NotFound(new { error = "Terrain data not available. Run SRTM processing first." });
        }

        var geoJson = await terrainService.GetSlopeVisualizationAsync();
        if (geoJson == "{}")
        {
            return Results.NotFound(new { error = "Failed to generate slope visualization" });
        }

        return Results.Text(geoJson, "application/json");
    }

    /// <summary>
    /// Check terrain data availability.
    /// GET /api/risk/terrain/status
    /// </summary>
    public static IResult GetTerrainStatus(SrtmTerrainService terrainService)
    {
        return Results.Ok(new
        {
            available = terrainService.IsAvailable,
            message = terrainService.IsAvailable
                ? "Terrain data is available"
                : "Terrain data not available. Run scripts/process_srtm_terrain.py to generate."
        });
    }
}

public static class TerrainControllerExtensions
{
    private const string TerrainTag = "Terrain";

    public static RouteGroupBuilder MapTerrainEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/terrain/{lat:double}/{lon:double}",
            (double lat, double lon, SrtmTerrainService svc) =>
                TerrainEndpoints.GetTerrainAnalysis(lat, lon, svc))
            .WithTags(TerrainTag)
            .WithName("GetTerrainAnalysis")
            .WithDescription("Get terrain analysis for a specific point including slope, aspect, elevation, and fire risk contribution");

        group.MapGet("/terrain/heatmap",
            (SrtmTerrainService svc) => TerrainEndpoints.GetTerrainHeatmap(svc))
            .WithTags(TerrainTag)
            .WithName("GetTerrainHeatmap")
            .WithDescription("Get terrain risk heatmap as GeoJSON for overlay on map");

        group.MapGet("/terrain/score/{lat:double}/{lon:double}",
            (double lat, double lon, SrtmTerrainService svc) =>
                TerrainEndpoints.GetTerrainRiskScore(lat, lon, svc))
            .WithTags(TerrainTag)
            .WithName("GetTerrainRiskScore")
            .WithDescription("Get simplified terrain risk score for a point");

        group.MapGet("/terrain/status",
            (SrtmTerrainService svc) => TerrainEndpoints.GetTerrainStatus(svc))
            .WithTags(TerrainTag)
            .WithName("GetTerrainStatus")
            .WithDescription("Check if terrain data is available");

        group.MapGet("/terrain/slope",
            (SrtmTerrainService svc) => TerrainEndpoints.GetSlopeVisualization(svc))
            .WithTags(TerrainTag)
            .WithName("GetSlopeVisualization")
            .WithDescription("Get slope data as GeoJSON for map visualization with color-coded slope categories");

        return group;
    }
}