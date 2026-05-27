using GeoRisk.API.Infrastructure.Services;

namespace GeoRisk.API.Features.Risk;

public sealed record LandUseResponse(
    Guid Id,
    double Latitude,
    double Longitude,
    string? CosCode,
    string? CosDescription,
    string FuelCategory,
    int FuelLoad,
    double FireRiskMultiplier,
    string? DominantSpecies,
    bool IsWildlandUrbanInterface,
    DateTime Timestamp
);

public sealed record FuelRiskScoreResponse(
    double Latitude,
    double Longitude,
    int FuelLoad,
    double FireRiskMultiplier,
    string FuelCategory,
    double SpreadRateMultiplier,
    string RiskLevel
);

public static class LandUseEndpoints
{
    /// <summary>
    /// Get land use data for a specific point.
    /// GET /api/risk/fuel/{lat}/{lon}
    /// </summary>
    public static async Task<IResult> GetLandUse(
        double lat,
        double lon,
        LandUseService landUseService)
    {
        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
        {
            return Results.BadRequest(new { error = "Invalid coordinates" });
        }

        var landUse = await landUseService.GetLandUseAsync(lat, lon);
        if (landUse == null)
        {
            return Results.NotFound(new { error = "Land use data not available for this location" });
        }

        var response = new LandUseResponse(
            landUse.Id,
            landUse.Location.Y,
            landUse.Location.X,
            landUse.CosCode,
            landUse.CosDescription,
            landUse.FuelCategory ?? "Unknown",
            landUse.FuelLoad,
            landUse.FireRiskMultiplier,
            landUse.DominantSpecies,
            landUse.IsWildlandUrbanInterface,
            landUse.Timestamp
        );

        return Results.Ok(response);
    }

    /// <summary>
    /// Get fuel risk heatmap as GeoJSON.
    /// GET /api/risk/fuel/heatmap
    /// </summary>
    public static async Task<IResult> GetFuelHeatmap(LandUseService landUseService)
    {
        var geoJson = await landUseService.GetLandUseHeatmapAsync();
        if (geoJson == "{}")
        {
            return Results.NotFound(new { error = "Failed to generate fuel heatmap" });
        }

        return Results.Ok(geoJson);
    }

    /// <summary>
    /// Get simplified fuel risk score for a point.
    /// GET /api/risk/fuel/score/{lat}/{lon}
    /// </summary>
    public static async Task<IResult> GetFuelRiskScore(
        double lat,
        double lon,
        LandUseService landUseService)
    {
        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
        {
            return Results.BadRequest(new { error = "Invalid coordinates" });
        }

        var landUse = await landUseService.GetLandUseAsync(lat, lon);
        if (landUse == null)
        {
            return Results.NotFound(new { error = "Land use data not available for this location" });
        }

        var spreadMultiplier = LandUseService.GetSpreadRateMultiplier(landUse.FuelCategory ?? "Bare");
        var riskLevel = ClassifyRiskLevel(landUse.FuelLoad, landUse.FireRiskMultiplier);

        return Results.Ok(new FuelRiskScoreResponse(
            lat,
            lon,
            landUse.FuelLoad,
            landUse.FireRiskMultiplier,
            landUse.FuelCategory ?? "Unknown",
            spreadMultiplier,
            riskLevel
        ));
    }

    /// <summary>
    /// Check if land use data is available.
    /// GET /api/risk/fuel/status
    /// </summary>
    public static IResult GetFuelStatus(LandUseService landUseService)
    {
        // LandUseService is always "available" since it can fetch from API
        return Results.Ok(new
        {
            available = true,
            message = "Land use data from DGT COS is available. Data is cached for 7 days."
        });
    }

    private static string ClassifyRiskLevel(int fuelLoad, double fireRiskMultiplier)
    {
        if (fireRiskMultiplier >= 2.0) return "Extreme";
        if (fireRiskMultiplier >= 1.5 || fuelLoad >= 4) return "High";
        if (fireRiskMultiplier >= 0.5 || fuelLoad >= 2) return "Medium";
        return "Low";
    }
}

public static class LandUseControllerExtensions
{
    public static RouteGroupBuilder MapFuelEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/fuel/{lat:double}/{lon:double}",
            (double lat, double lon, LandUseService svc) =>
                LandUseEndpoints.GetLandUse(lat, lon, svc))
            .WithTags("Fuel")
            .WithName("GetLandUse")
            .WithDescription("Get land use, fuel load, and fire risk multiplier for a specific point from DGT COS");

        group.MapGet("/fuel/heatmap",
            (LandUseService svc) => LandUseEndpoints.GetFuelHeatmap(svc))
            .WithTags("Fuel")
            .WithName("GetFuelHeatmap")
            .WithDescription("Get fuel risk heatmap as GeoJSON for the region");

        group.MapGet("/fuel/score/{lat:double}/{lon:double}",
            (double lat, double lon, LandUseService svc) =>
                LandUseEndpoints.GetFuelRiskScore(lat, lon, svc))
            .WithTags("Fuel")
            .WithName("GetFuelRiskScore")
            .WithDescription("Get simplified fuel risk score for a point");

        group.MapGet("/fuel/status",
            (LandUseService svc) => LandUseEndpoints.GetFuelStatus(svc))
            .WithTags("Fuel")
            .WithName("GetFuelStatus")
            .WithDescription("Check land use data availability");

        return group;
    }
}