using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Infrastructure.Services;

namespace GeoRisk.API.Features.Hotspots;

public sealed record GetActiveHotspotsQuery : IQuery<List<HotspotResponse>>;

public sealed class GetActiveHotspotsHandler(GeoRiskDbContext _db, HotspotAnalysisService service)
    : IQueryHandler<GetActiveHotspotsQuery, List<HotspotResponse>>
{
    public async Task<List<HotspotResponse>> HandleAsync(GetActiveHotspotsQuery query, CancellationToken ct)
    {
        var hotspots = await service.GetActiveHotspotsAsync(ct);
        return hotspots.Select(MapToResponse).ToList();
    }

    private static HotspotResponse MapToResponse(Domain.Entities.FireHotspot h) => new(
        h.Id,
        h.Name,
        h.Location.Y,
        h.Location.X,
        h.GridCellId,
        h.FireCount,
        h.TotalAreaBurned,
        h.AverageSeverity.ToString(),
        h.PeakMonth,
        h.PeakHour,
        h.CommonWindDirection,
        h.AverageFwi,
        h.RiskLevel.ToString(),
        h.LastUpdated);
}

public static class GetActiveHotspotsEndpoint
{
    public static RouteGroupBuilder MapActiveHotspots(this RouteGroupBuilder group)
    {
        group.MapGet("/hotspots/active", async (
            IQueryHandler<GetActiveHotspotsQuery, List<HotspotResponse>> handler,
            ILogger<GetActiveHotspotsHandler> logger) =>
        {
            try
            {
                var result = await handler.HandleAsync(new GetActiveHotspotsQuery(), default);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Get active hotspots error");
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).WithTags("Insights");

        return group;
    }
}