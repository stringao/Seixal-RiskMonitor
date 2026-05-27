using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Resources;

public static class ResourceEndpoints
{
    private const string ResourcesTag = "Resources";

    public static RouteGroupBuilder MapResources(this RouteGroupBuilder group)
    {
        group.MapGetStationsWithResources();
        group.MapGetAvailableResources();
        group.MapGetOptimalDispatch();
        group.MapDispatchResource();
        group.MapGetEventDispatches();
        group.MapGetResourceStatus();

        return group;
    }

    private static void MapGetStationsWithResources(this RouteGroupBuilder group)
    {
        group.MapGet("/stations", async (GeoRiskDbContext db) =>
        {
            var stations = await db.FireStations
                .Include(s => s.FireResources)
                .Where(s => s.IsActive)
                .AsNoTracking()
                .Select(s => new StationWithResourcesDto(
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Type,
                    s.Geometry.Y,
                    s.Geometry.X,
                    s.FireResources.Count(r => r.Status == "Available"),
                    s.FireResources.Count,
                    s.FireResources.Select(r => new ResourceSummaryDto(
                        r.Id,
                        r.Name,
                        r.ResourceType,
                        r.Status,
                        r.IsAvailable)).ToList()))
                .ToListAsync();

            return Results.Ok(new StationListResponse(stations));
        })
        .WithName("GetStationsWithResources")
        .WithTags(ResourcesTag)
        .WithDescription("List all fire stations with their available resources")
        .RequireAuthorization();
    }

    private static void MapGetAvailableResources(this RouteGroupBuilder group)
    {
        group.MapGet("/available", async (double lat, double lon, double radius = 50, ResourceRoutingService? service = null) =>
        {
            if (service == null)
                return Results.BadRequest(new { message = "Service not available" });

            var resources = await service.GetAvailableResourcesAsync(lat, lon, radius);
            return Results.Ok(new AvailableResourcesResponse(resources));
        })
        .WithName("GetAvailableResources")
        .WithTags(ResourcesTag)
        .WithDescription("Find available resources near a location")
        .RequireAuthorization();
    }

    private static void MapGetOptimalDispatch(this RouteGroupBuilder group)
    {
        group.MapGet("/optimal/{eventId:guid}", async (Guid eventId, ResourceRoutingService? service = null) =>
        {
            if (service == null)
                return Results.BadRequest(new { message = "Event not found" });

            var dispatch = await service.GetOptimalDispatchAsync(eventId);
            if (dispatch == null)
                return Results.NotFound(new { message = "Event not found" });

            return Results.Ok(dispatch);
        })
        .WithName("GetOptimalDispatch")
        .WithTags(ResourcesTag)
        .WithDescription("Get optimal dispatch recommendation for an event")
        .RequireAuthorization();
    }

    private static void MapDispatchResource(this RouteGroupBuilder group)
    {
        group.MapPost("/dispatch", async (DispatchRequest request, ResourceRoutingService service) =>
        {
            var dispatch = await service.DispatchResourceAsync(request.ResourceId, request.EventId);
            if (dispatch == null)
                return Results.BadRequest(new { message = "Resource or event not found" });

            return Results.Created($"/api/resources/dispatch/{dispatch.Id}", dispatch);
        })
        .WithName("DispatchResource")
        .WithTags(ResourcesTag)
        .WithDescription("Dispatch a resource to an event")
        .RequireAuthorization();
    }

    private static void MapGetEventDispatches(this RouteGroupBuilder group)
    {
        group.MapGet("/dispatch/{eventId:guid}", async (Guid eventId, ResourceRoutingService service) =>
        {
            var dispatches = await service.GetEventDispatchesAsync(eventId);
            return Results.Ok(new EventDispatchesResponse(dispatches));
        })
        .WithName("GetEventDispatches")
        .WithTags(ResourcesTag)
        .WithDescription("Get all dispatches for an event")
        .RequireAuthorization();
    }

    private static void MapGetResourceStatus(this RouteGroupBuilder group)
    {
        group.MapGet("/status", async (ResourceRoutingService service) =>
        {
            var status = await service.GetResourceStatusAsync();
            return Results.Ok(status);
        })
        .WithName("GetResourceStatus")
        .WithTags(ResourcesTag)
        .WithDescription("Get overall resource status summary")
        .RequireAuthorization();
    }
}

// DTOs
public record StationWithResourcesDto(
    Guid Id,
    string Name,
    string Code,
    string Type,
    double Latitude,
    double Longitude,
    int AvailableCount,
    int TotalResources,
    List<ResourceSummaryDto> Resources);

public record ResourceSummaryDto(
    Guid Id,
    string Name,
    string ResourceType,
    string Status,
    bool IsAvailable);

public record StationListResponse(List<StationWithResourcesDto> Stations);

public record AvailableResourcesResponse(List<AvailableResourceDto> Resources);

public record DispatchRequest(Guid ResourceId, Guid EventId);

public record EventDispatchesResponse(List<EventDispatchDto> Dispatches);