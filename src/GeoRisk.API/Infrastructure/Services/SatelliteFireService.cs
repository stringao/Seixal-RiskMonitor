using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

public sealed class SatelliteFireService(
    IFirmsClient firmsClient,
    IServiceScopeFactory scopeFactory,
    GeometryFactory geometryFactory,
    ILogger<SatelliteFireService> logger)
{
    // Circular buffer radius in meters (5km as specified)
    private const double LinkRadiusMeters = 5000;

    /// <summary>
    /// Fetch latest fire detections from NASA FIRMS and store them.
    /// </summary>
    public async Task FetchAndStoreAsync(CancellationToken ct = default)
    {
        var detections = await firmsClient.GetFireDetectionsAsync(ct);
        logger.LogInformation("Received {Count} fire detections from NASA FIRMS", detections.Count);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var imagesCreated = 0;
        var eventsLinked = 0;

        foreach (var detection in detections)
        {
            try
            {
                var captureTime = ConvertAcqTimeToDateTime(detection.AcqDate, detection.AcqTime);

                // Calculate confidence based on brightness temperature
                var confidence = CalculateConfidence(detection.BrightTi31);

                // Create small polygon footprint (VIIRS ~375m resolution)
                var footprint = CreateFootprint(detection.Longitude, detection.Latitude);

                var image = new SatelliteFireImage
                {
                    Id = Guid.NewGuid(),
                    CaptureTime = captureTime,
                    Source = "NASA_FIRMS",
                    ImageUrl = BuildThumbnailUrl(detection),
                    FullResolutionUrl = BuildFullResolutionUrl(detection),
                    Footprint = footprint,
                    FireRadiativePower = detection.Frp,
                    BrightnessTemperature = detection.BrightTi31,
                    IsActive = true,
                    DetectionConfidence = confidence,
                    CreatedAt = DateTime.UtcNow,
                };

                dbContext.SatelliteFireImages.Add(image);
                imagesCreated++;

                // Try to link to existing GeoEvent within 5km radius
                var linkedEventId = await TryLinkToEventAsync(
                    dbContext, detection.Latitude, detection.Longitude, captureTime, ct);

                if (linkedEventId.HasValue)
                {
                    image.LinkedEventId = linkedEventId;
                    eventsLinked++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process fire detection at ({Lat}, {Lon})",
                    detection.Latitude, detection.Longitude);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Satellite fire fetch completed: {Created} images created, {Linked} linked to events",
            imagesCreated, eventsLinked);

        // Cleanup old images (>30 days)
        await CleanupOldImagesAsync(dbContext, ct);
    }

    /// <summary>
    /// Get all recent satellite fire images within a date range and optional bounding box.
    /// </summary>
    public async Task<IReadOnlyList<SatelliteFireImage>> GetFireImagesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        double? minLat = null,
        double? maxLat = null,
        double? minLon = null,
        double? maxLon = null,
        CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var query = dbContext.SatelliteFireImages.AsNoTracking();

        if (fromDate.HasValue)
            query = query.Where(i => i.CaptureTime >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(i => i.CaptureTime <= toDate.Value);

        if (minLat.HasValue && maxLat.HasValue && minLon.HasValue && maxLon.HasValue)
        {
            var bbox = CreateBBoxFilter(minLat.Value, maxLat.Value, minLon.Value, maxLon.Value);
            query = query.Where(i => i.Footprint != null && i.Footprint.Intersects(bbox));
        }

        return await query
            .OrderByDescending(i => i.CaptureTime)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get the latest satellite fire images (last 48 hours).
    /// </summary>
    public async Task<IReadOnlyList<SatelliteFireImage>> GetLatestFireImagesAsync(
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-48);
        return await GetFireImagesAsync(fromDate: cutoff, ct: ct);
    }

    /// <summary>
    /// Get satellite fire images linked to a specific event.
    /// </summary>
    public async Task<IReadOnlyList<SatelliteFireImage>> GetFireImagesForEventAsync(
        Guid eventId,
        CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        return await dbContext.SatelliteFireImages
            .AsNoTracking()
            .Where(i => i.LinkedEventId == eventId)
            .OrderByDescending(i => i.CaptureTime)
            .ToListAsync(ct);
    }

    private async Task<Guid?> TryLinkToEventAsync(
        GeoRiskDbContext dbContext,
        double latitude,
        double longitude,
        DateTime captureTime,
        CancellationToken ct)
    {
        // Find active fire events within the radius that occurred near capture time
        var activeWindow = captureTime.AddHours(-24);

        // Create point for distance calculation
        var detectionPoint = geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        detectionPoint.SRID = 4326;

        var nearbyEvents = await dbContext.GeoEvents
            .AsNoTracking()
            .Where(e => e.EventType == EventType.Fire)
            .Where(e => e.OccurredAt >= activeWindow)
            .Where(e => e.OccurredAt <= captureTime.AddHours(24))
            .Select(e => new { e.Id, e.Geometry })
            .ToListAsync(ct);

        foreach (var geoEvent in nearbyEvents)
        {
            var distance = detectionPoint.Distance(geoEvent.Geometry);
            // Convert meters to degrees approximately (111320 meters per degree at equator)
            if (distance <= LinkRadiusMeters / 111320.0)
            {
                return geoEvent.Id;
            }
        }

        return null;
    }

    private async Task CleanupOldImagesAsync(GeoRiskDbContext dbContext, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var deleted = await dbContext.SatelliteFireImages
            .Where(i => i.CaptureTime < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
        {
            logger.LogInformation("Cleaned up {Count} old satellite fire images", deleted);
        }
    }

    private static string CalculateConfidence(double brightTi31)
    {
        // Higher brightness temperature indicates more confident fire detection
        // VIIRS active fire thresholds: low > 310K, nominal > 350K, high > 370K
        return brightTi31 switch
        {
            > 370 => "high",
            > 350 => "nominal",
            _ => "low"
        };
    }

    private static DateTime ConvertAcqTimeToDateTime(DateOnly date, int acqTime)
    {
        var hours = acqTime / 100;
        var minutes = acqTime % 100;
        return date.ToDateTime(new TimeOnly(hours, minutes));
    }

    private static Polygon CreateFootprint(double lon, double lat)
    {
        // Create a small square footprint around the detection point
        // ~375m radius (VIIRS resolution)
        const double offset = 0.003; // ~300m

        var coordinates = new[]
        {
            new Coordinate(lon - offset, lat - offset),
            new Coordinate(lon + offset, lat - offset),
            new Coordinate(lon + offset, lat + offset),
            new Coordinate(lon - offset, lat + offset),
            new Coordinate(lon - offset, lat - offset), // Close the ring
        };

        var shell = new LinearRing(coordinates);
        return new Polygon(shell);
    }

    private static Polygon CreateBBoxFilter(double minLat, double maxLat, double minLon, double maxLon)
    {
        var coordinates = new[]
        {
            new Coordinate(minLon, minLat),
            new Coordinate(maxLon, minLat),
            new Coordinate(maxLon, maxLat),
            new Coordinate(minLon, maxLat),
            new Coordinate(minLon, minLat),
        };

        var shell = new LinearRing(coordinates);
        return new Polygon(shell);
    }

    private static string BuildThumbnailUrl(FirmsFireDetection detection)
    {
        // VIIRS world map tiles - provides near-real-time tiles
        // Using NASA FIRMS hot spot mapping service
        var lat = detection.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lon = detection.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
#pragma warning disable S1075
        return $"https://firms.modaps.eosdis.nasa.gov/wms/{lat}/{lon}";
#pragma warning restore S1075
    }

    private static string BuildFullResolutionUrl(FirmsFireDetection detection)
    {
        // EO Browser link for Sentinel-2 imagery at location
        var lat = detection.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lon = detection.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
#pragma warning disable S1075
        return $"https://apps.sentinel-hub.com/eo-browser/?lat={lat}&lng={lon}&zoom=15&date={detection.AcqDate:yyyy-MM-dd}";
#pragma warning restore S1075
    }
}
