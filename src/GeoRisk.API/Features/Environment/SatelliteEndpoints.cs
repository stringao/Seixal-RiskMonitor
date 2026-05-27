using GeoRisk.API.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeoRisk.API.Features.Environment;

public static class SatelliteEndpoints
{
    public static void MapSatelliteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environment")
            .WithTags("Environment");

        group.MapGet("/satellite/fires", GetSatelliteFires)
            .WithName("GetSatelliteFires")
            .WithDescription("Get recent satellite fire detections with optional filtering");

        group.MapGet("/satellite/fires/latest", GetLatestSatelliteFires)
            .WithName("GetLatestSatelliteFires")
            .WithDescription("Get most recent satellite fire detections (last 48 hours)");

        group.MapGet("/satellite/fires/{eventId:guid}", GetSatelliteFiresByEvent)
            .WithName("GetSatelliteFiresByEvent")
            .WithDescription("Get satellite fire images linked to a specific fire event");
    }

#pragma warning disable S107
    private static async Task<IResult> GetSatelliteFires(
        [FromServices] SatelliteFireService service,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] double? minLat,
        [FromQuery] double? maxLat,
        [FromQuery] double? minLon,
        [FromQuery] double? maxLon,
        CancellationToken ct = default)
#pragma warning restore S107
    {
        var images = await service.GetFireImagesAsync(fromDate, toDate, minLat, maxLat, minLon, maxLon, ct);

        var response = images.Select(i => new SatelliteFireImageResponse(
            i.Id,
            i.LinkedEventId,
            i.CaptureTime,
            i.Source,
            i.ImageUrl,
            i.FullResolutionUrl,
            i.Footprint != null ? new FootprintResponse(
                i.Footprint.Coordinates[0].Y,
                i.Footprint.Coordinates[0].X,
                i.Footprint.Coordinates[2].Y,
                i.Footprint.Coordinates[2].X) : null,
            i.FireRadiativePower,
            i.BrightnessTemperature,
            i.IsActive,
            i.DetectionConfidence)).ToList();

        return Results.Ok(new SatelliteFireListResponse(response));
    }

    private static async Task<IResult> GetLatestSatelliteFires(
        [FromServices] SatelliteFireService service,
        CancellationToken ct = default)
    {
        var images = await service.GetLatestFireImagesAsync(ct);

        var response = images.Select(i => new SatelliteFireImageResponse(
            i.Id,
            i.LinkedEventId,
            i.CaptureTime,
            i.Source,
            i.ImageUrl,
            i.FullResolutionUrl,
            i.Footprint != null ? new FootprintResponse(
                i.Footprint.Coordinates[0].Y,
                i.Footprint.Coordinates[0].X,
                i.Footprint.Coordinates[2].Y,
                i.Footprint.Coordinates[2].X) : null,
            i.FireRadiativePower,
            i.BrightnessTemperature,
            i.IsActive,
            i.DetectionConfidence)).ToList();

        return Results.Ok(new SatelliteFireListResponse(response));
    }

    private static async Task<IResult> GetSatelliteFiresByEvent(
        [FromRoute] Guid eventId,
        [FromServices] SatelliteFireService service,
        CancellationToken ct = default)
    {
        var images = await service.GetFireImagesForEventAsync(eventId, ct);

        var response = images.Select(i => new SatelliteFireImageResponse(
            i.Id,
            i.LinkedEventId,
            i.CaptureTime,
            i.Source,
            i.ImageUrl,
            i.FullResolutionUrl,
            i.Footprint != null ? new FootprintResponse(
                i.Footprint.Coordinates[0].Y,
                i.Footprint.Coordinates[0].X,
                i.Footprint.Coordinates[2].Y,
                i.Footprint.Coordinates[2].X) : null,
            i.FireRadiativePower,
            i.BrightnessTemperature,
            i.IsActive,
            i.DetectionConfidence)).ToList();

        return Results.Ok(new SatelliteFireListResponse(response));
    }
}

// ─── Request/Response DTOs ────────────────────────────────────────────

public sealed record SatelliteFireListResponse(
    List<SatelliteFireImageResponse> Images);

public sealed record SatelliteFireImageResponse(
    Guid Id,
    Guid? LinkedEventId,
    DateTime CaptureTime,
    string Source,
    string ImageUrl,
    string FullResolutionUrl,
    FootprintResponse? Footprint,
    double? FireRadiativePower,
    double? BrightnessTemperature,
    bool IsActive,
    string? DetectionConfidence);

public sealed record FootprintResponse(
    double MinLatitude,
    double MinLongitude,
    double MaxLatitude,
    double MaxLongitude);
