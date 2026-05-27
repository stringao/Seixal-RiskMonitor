using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Infrastructure.Services;

namespace GeoRisk.API.Features.Hotspots;

public sealed record FindHotspotsNearQuery(
    double Lat,
    double Lon,
    double RadiusKm = 10) : IQuery<List<HotspotNearResponse>>;

public sealed record HotspotNearResponse(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string GridCellId,
    int FireCount,
    double TotalAreaBurned,
    string RiskLevel,
    double DistanceKm);

public sealed class FindHotspotsNearHandler(GeoRiskDbContext _db, HotspotAnalysisService service)
    : IQueryHandler<FindHotspotsNearQuery, List<HotspotNearResponse>>
{
    public async Task<List<HotspotNearResponse>> HandleAsync(FindHotspotsNearQuery query, CancellationToken ct)
    {
        var results = await service.FindHotspotsNearAsync(query.Lat, query.Lon, query.RadiusKm, ct);
        return results.Select(r => new HotspotNearResponse(
            r.Hotspot.Id,
            r.Hotspot.Name,
            r.Hotspot.Location.Y,
            r.Hotspot.Location.X,
            r.Hotspot.GridCellId,
            r.Hotspot.FireCount,
            r.Hotspot.TotalAreaBurned,
            r.Hotspot.RiskLevel.ToString(),
            Math.Round(r.DistanceKm, 2))).ToList();
    }
}

public static class FindHotspotsNearEndpoint
{
    public static RouteGroupBuilder MapHotspotsNear(this RouteGroupBuilder group)
    {
        group.MapGet("/hotspots/near/{lat:double}/{lon:double}", async (
            double lat,
            double lon,
            double radiusKm,
            IQueryHandler<FindHotspotsNearQuery, List<HotspotNearResponse>> handler,
            ILogger<FindHotspotsNearHandler> logger) =>
        {
            try
            {
                var result = await handler.HandleAsync(new FindHotspotsNearQuery(lat, lon, radiusKm), default);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Find hotspots near error");
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).WithTags("Insights");

        return group;
    }
}