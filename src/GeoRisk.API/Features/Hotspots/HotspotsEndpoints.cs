using GeoRisk.API.Common.CQRS;

namespace GeoRisk.API.Features.Hotspots;

public static class HotspotsEndpointExtensions
{
    public static RouteGroupBuilder MapHotspots(this RouteGroupBuilder group)
    {
        GetHotspotsEndpoint.MapHotspots(group);
        GetActiveHotspotsEndpoint.MapActiveHotspots(group);
        GetHotspotByIdEndpoint.MapHotspotById(group);
        FindHotspotsNearEndpoint.MapHotspotsNear(group);
        return group;
    }
}