using GeoRisk.API.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeoRisk.API.Features.Environment;

public static class AirQualityEndpoints
{
    private const string UnknownStr = "Unknown";

    public static void MapAirQualityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .WithTags("Environment");

        group.MapGet("/air-quality/{lat:double}/{lon:double}", GetAirQuality)
            .WithName("GetAirQuality")
            .WithDescription("Get current air quality and pollen data for a specific point");

        group.MapGet("/air-quality/region", GetRegionAirQuality)
            .WithName("GetRegionAirQuality")
            .WithDescription("Get aggregated air quality for the Setúbal region");

        group.MapGet("/pollen/{lat:double}/{lon:double}", GetPollen)
            .WithName("GetPollen")
            .WithDescription("Get pollen data for a specific point");
    }

    private static async Task<IResult> GetAirQuality(
        [FromRoute] double lat,
        [FromRoute] double lon,
        [FromServices] AirQualityService service,
        CancellationToken ct = default)
    {
        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
        {
            return Results.BadRequest(new { error = "Invalid coordinates" });
        }

        var result = await service.GetAirQualityAsync(lat, lon, ct);

        if (result.Error != null)
        {
            return Results.Problem(result.Error);
        }

        return Results.Ok(new AirQualityResponse(
            lat, lon,
            result.Current != null ? new CurrentAirQuality(
                result.Current.Timestamp,
                result.Current.Pm10,
                result.Current.Pm25,
                result.Current.NitrogenDioxide,
                result.Current.Ozone,
                result.Current.SulphurDioxide,
                result.Current.CarbonMonoxide,
                result.Current.Dust,
                result.Current.AerosolOpticalDepth) : null,
            result.TodayAverage != null ? new TodayAirQualitySummary(
                result.TodayAverage.Pm10,
                result.TodayAverage.Pm25,
                result.TodayAverage.NitrogenDioxide,
                result.TodayAverage.Ozone,
                result.TodayAverage.AqiValue ?? 1,
                result.TodayAverage.AqiCategory ?? UnknownStr,
                result.TodayAverage.PollenIndex ?? 1,
                result.TodayAverage.PollenCategory ?? UnknownStr,
                result.TodayAverage.HealthRiskLevel ?? UnknownStr) : null,
            result.Forecast.Select(f => new AirQualityForecastDay(
                f.Date,
                f.Pm10,
                f.Pm25,
                f.NitrogenDioxide,
                f.Ozone,
                f.AqiValue ?? 1,
                f.AqiCategory ?? UnknownStr,
                f.PollenIndex ?? 1,
                f.PollenCategory ?? UnknownStr)).ToList()));
    }

    private static async Task<IResult> GetRegionAirQuality(
        [FromServices] AirQualityService service,
        CancellationToken ct = default)
    {
        var result = await service.GetRegionAirQualityAsync(ct);

        if (result.Error != null)
        {
            return Results.Problem(result.Error);
        }

        return Results.Ok(new RegionAirQualityResponse(
            result.RegionAqiValue ?? 1,
            result.RegionAqiCategory ?? UnknownStr,
            result.GridPoints.Select(g => new GridPointAirQualityResponse(
                g.Longitude,
                g.Latitude,
                g.Pm10,
                g.Pm25,
                g.Ozone,
                g.NitrogenDioxide,
                g.AqiValue,
                g.AqiCategory,
                g.HealthRiskLevel)).ToList()));
    }

    private static async Task<IResult> GetPollen(
        [FromRoute] double lat,
        [FromRoute] double lon,
        [FromServices] AirQualityService service,
        CancellationToken ct = default)
    {
        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
        {
            return Results.BadRequest(new { error = "Invalid coordinates" });
        }

        var result = await service.GetPollenAsync(lat, lon, ct);

        if (result.Error != null)
        {
            return Results.Problem(result.Error);
        }

        return Results.Ok(new PollenResponse(
            result.PollenIndex ?? 1,
            result.PollenCategory ?? UnknownStr,
            result.DominantPollen ?? UnknownStr,
            result.Current != null ? new CurrentPollenResponse(
                result.Current.GrassPollen,
                result.Current.OlivePollen,
                result.Current.AlderPollen,
                result.Current.BirchPollen,
                result.Current.MugwortPollen,
                result.Current.RagweedPollen) : null,
            result.Breakdown));
    }
}

// ─── Request/Response DTOs ────────────────────────────────────────────

public sealed record AirQualityResponse(
    double Latitude,
    double Longitude,
    CurrentAirQuality? Current,
    TodayAirQualitySummary? TodayAverage,
    List<AirQualityForecastDay> Forecast);

public sealed record CurrentAirQuality(
    DateTime Timestamp,
    double? Pm10,
    double? Pm25,
    double? NitrogenDioxide,
    double? Ozone,
    double? SulphurDioxide,
    double? CarbonMonoxide,
    double? Dust,
    double? AerosolOpticalDepth);

public sealed record TodayAirQualitySummary(
    double? Pm10,
    double? Pm25,
    double? NitrogenDioxide,
    double? Ozone,
    int AqiValue,
    string AqiCategory,
    int PollenIndex,
    string PollenCategory,
    string HealthRiskLevel);

public sealed record AirQualityForecastDay(
    DateTime Date,
    double? Pm10,
    double? Pm25,
    double? NitrogenDioxide,
    double? Ozone,
    int AqiValue,
    string AqiCategory,
    int PollenIndex,
    string PollenCategory);

public sealed record RegionAirQualityResponse(
    int RegionAqiValue,
    string RegionAqiCategory,
    List<GridPointAirQualityResponse> GridPoints);

public sealed record GridPointAirQualityResponse(
    double Longitude,
    double Latitude,
    double? Pm10,
    double? Pm25,
    double? Ozone,
    double? NitrogenDioxide,
    int AqiValue,
    string AqiCategory,
    string HealthRiskLevel);

public sealed record PollenResponse(
    int PollenIndex,
    string PollenCategory,
    string DominantPollen,
    CurrentPollenResponse? Current,
    Dictionary<string, double> Breakdown);

public sealed record CurrentPollenResponse(
    double? GrassPollen,
    double? OlivePollen,
    double? AlderPollen,
    double? BirchPollen,
    double? MugwortPollen,
    double? RagweedPollen);
