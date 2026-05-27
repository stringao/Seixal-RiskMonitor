using System.Text.Json;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Service for SIREN resource routing optimization.
/// Handles dispatch decisions, ETA calculations, and multi-fire resource allocation.
/// </summary>
public class ResourceRoutingService
{
    private readonly GeoRiskDbContext _db;
    private readonly HttpClient _osrmClient;
    private readonly ILogger<ResourceRoutingService> _logger;

    private const string AvailableStatus = "Available";
    private const string EnRouteStatus = "EnRoute";
    private const double DefaultRadiusKm = 50.0;
    private const int MaxDispatchOptions = 3;

    private static readonly JsonSerializerOptions CachedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ResourceRoutingService(
        GeoRiskDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<ResourceRoutingService> logger)
    {
        _db = db;
        _osrmClient = httpClientFactory.CreateClient("OSRM");
        _logger = logger;
    }

    /// <summary>
    /// Get available resources near a location.
    /// </summary>
    public async Task<List<AvailableResourceDto>> GetAvailableResourcesAsync(
        double lat, double lon, double radiusKm = DefaultRadiusKm, CancellationToken ct = default)
    {
        var point = new Point(lon, lat) { SRID = 4326 };
        var bufferDegrees = radiusKm / 111.0;

        var resources = await _db.FireResources
            .Include(r => r.FireStation)
            .Where(r => r.IsAvailable && r.Status == AvailableStatus)
            .Where(r => r.FireStation!.Geometry.Distance(point) <= bufferDegrees)
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<AvailableResourceDto>();
        foreach (var resource in resources)
        {
            var station = resource.FireStation!;
            var distanceKm = CalculateDistanceKm(lat, lon, station.Geometry.Y, station.Geometry.X);
            var route = await GetRouteFromOsrmAsync(station.Geometry.X, station.Geometry.Y, lon, lat);

            result.Add(new AvailableResourceDto(
                resource.Id,
                resource.Name,
                resource.ResourceType,
                station.Id,
                station.Name,
                station.Geometry.Y,
                station.Geometry.X,
                distanceKm,
                route?.duration / 60.0 ?? 0, // travel time in minutes
                resource.PersonnelCount,
                resource.WaterCapacityLiters,
                resource.Status));
        }

        return result.OrderBy(r => r.TravelTimeMinutes).ToList();
    }

    /// <summary>
    /// Get optimal dispatch recommendation for an event.
    /// Considers distance, travel time, resource type, and availability.
    /// </summary>
    public async Task<OptimalDispatchDto?> GetOptimalDispatchAsync(Guid eventId, CancellationToken ct = default)
    {
        var evt = await _db.GeoEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (evt == null)
            return null;

        // Determine required resource type based on event type/severity
        var requiredType = DetermineResourceType(evt.EventType, evt.Severity);

        // Find all available resources of that type within radius
        var point = new Point(evt.Geometry.X, evt.Geometry.Y) { SRID = 4326 };
        var bufferDegrees = DefaultRadiusKm / 111.0;

        var resources = await _db.FireResources
            .Include(r => r.FireStation)
            .Where(r => r.IsAvailable && r.Status == AvailableStatus)
            .Where(r => string.IsNullOrEmpty(requiredType) || r.ResourceType == requiredType)
            .Where(r => r.FireStation!.Geometry.Distance(point) <= bufferDegrees)
            .AsNoTracking()
            .ToListAsync(ct);

        var options = new List<DispatchOptionDto>();

        foreach (var resource in resources)
        {
            var station = resource.FireStation!;
            var distanceKm = CalculateDistanceKm(
                evt.Geometry.Y, evt.Geometry.X,
                station.Geometry.Y, station.Geometry.X);

            var route = await GetRouteFromOsrmAsync(
                station.Geometry.X, station.Geometry.Y,
                evt.Geometry.X, evt.Geometry.Y);

            var travelTimeMinutes = route?.duration / 60.0 ?? (distanceKm / 60.0 * 60);

            options.Add(new DispatchOptionDto(
                resource.Id,
                resource.Name,
                resource.ResourceType,
                station.Id,
                station.Name,
                station.Geometry.Y,
                station.Geometry.X,
                distanceKm,
                travelTimeMinutes,
                resource.PersonnelCount,
                resource.WaterCapacityLiters,
                CalculatePriorityScore(distanceKm, travelTimeMinutes, evt.Severity)));
        }

        var topOptions = options
            .OrderByDescending(o => o.PriorityScore)
            .Take(MaxDispatchOptions)
            .ToList();

        return new OptimalDispatchDto(
            eventId,
            evt.Title,
            evt.Geometry.Y,
            evt.Geometry.X,
            requiredType ?? "Any",
            topOptions);
    }

    /// <summary>
    /// Optimize resource allocation across multiple fires.
    /// Uses a greedy algorithm to assign resources to fires by priority.
    /// </summary>
    public async Task<MultiFireAllocationDto> GetMultiFireAllocationAsync(
        List<Guid> eventIds, CancellationToken ct = default)
    {
        var allocations = new List<FireAllocationDto>();
        var assignedResources = new HashSet<Guid>();

        // Get all events with their severities
        var events = await _db.GeoEvents
            .AsNoTracking()
            .Where(e => eventIds.Contains(e.Id))
            .ToListAsync(ct);

        // Sort events by severity (highest first) then by time (oldest first)
        var sortedEvents = events
            .OrderByDescending(e => e.Severity)
            .ThenBy(e => e.OccurredAt)
            .ToList();

        foreach (var evt in sortedEvents)
        {
            var requiredType = DetermineResourceType(evt.EventType, evt.Severity);
            var point = new Point(evt.Geometry.X, evt.Geometry.Y) { SRID = 4326 };
            var bufferDegrees = DefaultRadiusKm / 111.0;

            // Get available resources not yet assigned
            var availableResources = await _db.FireResources
                .Include(r => r.FireStation)
                .Where(r => r.IsAvailable && r.Status == AvailableStatus)
                .Where(r => !assignedResources.Contains(r.Id))
                .Where(r => string.IsNullOrEmpty(requiredType) || r.ResourceType == requiredType)
                .Where(r => r.FireStation!.Geometry.Distance(point) <= bufferDegrees)
                .AsNoTracking()
                .ToListAsync(ct);

            var fireAllocations = new List<ResourceAllocationItemDto>();

            foreach (var resource in availableResources.Take(3)) // Max 3 resources per fire
            {
                var station = resource.FireStation!;
                var distanceKm = CalculateDistanceKm(
                    evt.Geometry.Y, evt.Geometry.X,
                    station.Geometry.Y, station.Geometry.X);

                var route = await GetRouteFromOsrmAsync(
                    station.Geometry.X, station.Geometry.Y,
                    evt.Geometry.X, evt.Geometry.Y);

                var travelTimeMinutes = route?.duration / 60.0 ?? (distanceKm / 60.0 * 60);

                fireAllocations.Add(new ResourceAllocationItemDto(
                    resource.Id,
                    resource.Name,
                    resource.ResourceType,
                    station.Name,
                    distanceKm,
                    travelTimeMinutes,
                    resource.PersonnelCount));

                assignedResources.Add(resource.Id);
            }

            allocations.Add(new FireAllocationDto(
                evt.Id,
                evt.Title,
                evt.Geometry.Y,
                evt.Geometry.X,
                evt.Severity.ToString(),
                fireAllocations));
        }

        return new MultiFireAllocationDto(allocations, assignedResources.Count);
    }

    /// <summary>
    /// Calculate current ETA for a resource to an event.
    /// </summary>
    public async Task<EtaResultDto?> CalculateEtaAsync(Guid resourceId, Guid eventId, CancellationToken ct = default)
    {
        var resource = await _db.FireResources
            .Include(r => r.FireStation)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId, ct);

        var evt = await _db.GeoEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (resource == null || evt == null || resource.FireStation == null)
            return null;

        var distanceKm = CalculateDistanceKm(
            evt.Geometry.Y, evt.Geometry.X,
            resource.FireStation.Geometry.Y, resource.FireStation.Geometry.X);

        var route = await GetRouteFromOsrmAsync(
            resource.FireStation.Geometry.X, resource.FireStation.Geometry.Y,
            evt.Geometry.X, evt.Geometry.Y);

        var travelTimeMinutes = route?.duration / 60.0 ?? (distanceKm / 60.0 * 60);

        return new EtaResultDto(
            resourceId,
            resource.Name,
            eventId,
            evt.Title,
            distanceKm,
            travelTimeMinutes,
            DateTime.UtcNow.AddMinutes(travelTimeMinutes),
            route?.geometry);
    }

    /// <summary>
    /// Update resource status after dispatch or other actions.
    /// </summary>
    public async Task<bool> UpdateResourceStatusAsync(
        Guid resourceId, string status, Guid? eventId = null, CancellationToken ct = default)
    {
        var resource = await _db.FireResources
            .FirstOrDefaultAsync(r => r.Id == resourceId, ct);

        if (resource == null)
            return false;

        resource.Status = status;
        resource.IsAvailable = status == AvailableStatus;
        resource.UpdatedAt = DateTime.UtcNow;

        if (eventId.HasValue)
        {
            resource.DeployedToEventId = eventId;
            resource.DeployedAt = DateTime.UtcNow;

            // Estimate return time based on typical incident duration
            var expectedDurationMinutes = status switch
            {
                "Dispatched" => 60,
                EnRouteStatus => 45,
                "OnScene" => 180,
                _ => 120
            };
            resource.ExpectedReturnAt = DateTime.UtcNow.AddMinutes(expectedDurationMinutes);
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Dispatch a resource to an event.
    /// </summary>
    public async Task<ResourceDispatch?> DispatchResourceAsync(
        Guid resourceId, Guid eventId, CancellationToken ct = default)
    {
        var resource = await _db.FireResources
            .Include(r => r.FireStation)
            .FirstOrDefaultAsync(r => r.Id == resourceId, ct);

        var evt = await _db.GeoEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (resource == null || evt == null || resource.FireStation == null)
            return null;

        // Update resource status
        await UpdateResourceStatusAsync(resourceId, EnRouteStatus, eventId, ct);

        // Calculate route
        var distanceKm = CalculateDistanceKm(
            evt.Geometry.Y, evt.Geometry.X,
            resource.FireStation.Geometry.Y, resource.FireStation.Geometry.X);

        var route = await GetRouteFromOsrmAsync(
            resource.FireStation.Geometry.X, resource.FireStation.Geometry.Y,
            evt.Geometry.X, evt.Geometry.Y);

        var travelTimeMinutes = route?.duration / 60.0 ?? (distanceKm / 60.0 * 60);

        // Create dispatch record
        var dispatch = new ResourceDispatch
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ResourceId = resourceId,
            DispatchedAt = DateTime.UtcNow,
            DistanceKm = distanceKm,
            TravelTimeMinutes = travelTimeMinutes,
            RoutePolyline = route != null ? EncodePolyline(route.geometry) : string.Empty,
            Status = EnRouteStatus
        };

        _db.ResourceDispatches.Add(dispatch);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Dispatched resource {ResourceName} from {StationName} to event {EventTitle}. ETA: {Eta} min",
            resource.Name, resource.FireStation.Name, evt.Title, travelTimeMinutes);

        return dispatch;
    }

    /// <summary>
    /// Get all dispatches for an event.
    /// </summary>
    public async Task<List<EventDispatchDto>> GetEventDispatchesAsync(Guid eventId, CancellationToken ct = default)
    {
        return await _db.ResourceDispatches
            .Include(d => d.Resource)
            .ThenInclude(r => r!.FireStation)
            .Where(d => d.EventId == eventId)
            .AsNoTracking()
            .Select(d => new EventDispatchDto(
                d.Id,
                d.ResourceId,
                d.Resource!.Name,
                d.Resource!.ResourceType,
                d.Resource!.FireStation!.Name,
                d.DispatchedAt,
                d.ArrivedAt,
                d.DistanceKm ?? 0,
                d.TravelTimeMinutes ?? 0,
                d.Status,
                d.RoutePolyline))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get overall resource status summary.
    /// </summary>
    public async Task<ResourceStatusSummaryDto> GetResourceStatusAsync(CancellationToken ct = default)
    {
        var resources = await _db.FireResources
            .AsNoTracking()
            .ToListAsync(ct);

        var byType = resources
            .GroupBy(r => r.ResourceType)
            .ToDictionary(
                g => g.Key,
                g => new ResourceTypeStatusDto(
                    g.Key,
                    g.Count(r => r.Status == AvailableStatus),
                    g.Count(r => r.Status == "Dispatched" || r.Status == EnRouteStatus),
                    g.Count(r => r.Status == "OnScene"),
                    g.Count()));

        var byStation = await _db.FireStations
            .Include(s => s.FireResources)
            .Where(s => s.IsActive)
            .AsNoTracking()
            .Select(s => new StationResourceStatusDto(
                s.Id,
                s.Name,
                s.Geometry.Y,
                s.Geometry.X,
                s.FireResources.Count(r => r.Status == AvailableStatus),
                s.FireResources.Count(r => r.Status != AvailableStatus && r.Status != "Maintenance"),
                s.FireResources.Count))
            .ToListAsync(ct);

        return new ResourceStatusSummaryDto(
            resources.Count,
            resources.Count(r => r.Status == AvailableStatus),
            resources.Count(r => r.Status == "Dispatched" || r.Status == EnRouteStatus),
            resources.Count(r => r.Status == "OnScene"),
            byType,
            byStation);
    }

    private static string? DetermineResourceType(Domain.Enums.EventType eventType, Domain.Enums.RiskLevel severity)
    {
        // Simple heuristic for resource type selection
        // For fire events, severity determines the type of resource needed
        if (eventType == Domain.Enums.EventType.Fire)
        {
            return severity switch
            {
                Domain.Enums.RiskLevel.Critical => "Tanker",
                Domain.Enums.RiskLevel.High => "Tanker",
                Domain.Enums.RiskLevel.Medium => "Pump",
                _ => "Team"
            };
        }

        // For non-fire events (flood, landslide, etc.), use Team
        return eventType switch
        {
            Domain.Enums.EventType.Flood => "Team",
            Domain.Enums.EventType.Landslide => "Team",
            Domain.Enums.EventType.Storm => "Team",
            Domain.Enums.EventType.Industrial => "CommandUnit",
            _ => "Team"
        };
    }

    private static double CalculatePriorityScore(double distanceKm, double travelTimeMinutes, Domain.Enums.RiskLevel severity)
    {
        // Higher score = better option
        var distanceScore = Math.Max(0, 50 - distanceKm) / 50; // 0-1, closer is better
        var timeScore = Math.Max(0, 60 - travelTimeMinutes) / 60; // 0-1, faster is better
        var severityWeight = severity switch
        {
            Domain.Enums.RiskLevel.Critical => 1.5,
            Domain.Enums.RiskLevel.High => 1.2,
            Domain.Enums.RiskLevel.Medium => 1.0,
            _ => 0.8
        };

        return (distanceScore * 0.3 + timeScore * 0.7) * severityWeight;
    }

    private async Task<OsrmRouteResult?> GetRouteFromOsrmAsync(double fromLng, double fromLat, double toLng, double toLat)
    {
        try
        {
            var url = $"http://localhost:5001/route/v1/driving/{fromLng},{fromLat};{toLng},{toLat}?overview=full&geometries=geojson";
            var response = await _osrmClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<OsrmResponse>(json, CachedJsonOptions);

            if (data?.routes?.FirstOrDefault() is { } route)
            {
                return new OsrmRouteResult(
                    route.distance,
                    route.duration,
                    route.geometry.coordinates.Select(c => (c[1], c[0])).ToList());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get route from OSRM");
        }

        return null;
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    private static string EncodePolyline(List<(double lat, double lng)> coordinates)
    {
        // Simple encoding for polyline - in production use proper polyline encoding
        return JsonSerializer.Serialize(coordinates);
    }
}

// DTOs
public record AvailableResourceDto(
    Guid ResourceId,
    string Name,
    string ResourceType,
    Guid StationId,
    string StationName,
    double Latitude,
    double Longitude,
    double DistanceKm,
    double TravelTimeMinutes,
    int PersonnelCount,
    double WaterCapacityLiters,
    string Status);

public record DispatchOptionDto(
    Guid ResourceId,
    string Name,
    string ResourceType,
    Guid StationId,
    string StationName,
    double Latitude,
    double Longitude,
    double DistanceKm,
    double TravelTimeMinutes,
    int PersonnelCount,
    double WaterCapacityLiters,
    double PriorityScore);

public record OptimalDispatchDto(
    Guid EventId,
    string EventTitle,
    double EventLatitude,
    double EventLongitude,
    string RequiredResourceType,
    List<DispatchOptionDto> Options);

public record ResourceAllocationItemDto(
    Guid ResourceId,
    string Name,
    string ResourceType,
    string StationName,
    double DistanceKm,
    double TravelTimeMinutes,
    int PersonnelCount);

public record FireAllocationDto(
    Guid EventId,
    string EventTitle,
    double Latitude,
    double Longitude,
    string Severity,
    List<ResourceAllocationItemDto> AssignedResources);

public record MultiFireAllocationDto(
    List<FireAllocationDto> FireAllocations,
    int TotalResourcesAssigned);

public record EtaResultDto(
    Guid ResourceId,
    string ResourceName,
    Guid EventId,
    string EventTitle,
    double DistanceKm,
    double EtaMinutes,
    DateTime EstimatedArrival,
    List<(double lat, double lng)>? RouteGeometry);

public record EventDispatchDto(
    Guid DispatchId,
    Guid ResourceId,
    string ResourceName,
    string ResourceType,
    string StationName,
    DateTime DispatchedAt,
    DateTime? ArrivedAt,
    double DistanceKm,
    double TravelTimeMinutes,
    string Status,
    string RoutePolyline);

public record ResourceTypeStatusDto(
    string ResourceType,
    int Available,
    int EnRoute,
    int OnScene,
    int Total);

public record StationResourceStatusDto(
    Guid StationId,
    string StationName,
    double Latitude,
    double Longitude,
    int AvailableCount,
    int DeployedCount,
    int TotalResources);

public record ResourceStatusSummaryDto(
    int TotalResources,
    int Available,
    int EnRoute,
    int OnScene,
    Dictionary<string, ResourceTypeStatusDto> ByType,
    List<StationResourceStatusDto> ByStation);

// OSRM response types
internal record OsrmResponse(List<OsrmRoute> routes);
internal record OsrmRoute(double distance, double duration, OsrmGeometry geometry);
internal record OsrmGeometry(List<List<double>> coordinates);

internal record OsrmRouteResult(double distance, double duration, List<(double lat, double lng)> geometry);