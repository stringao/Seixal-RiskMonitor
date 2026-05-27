using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Hotspots;

public sealed record GetHotspotsQuery(RiskLevel? RiskLevel = null) : IQuery<List<HotspotResponse>>;

public sealed record HotspotResponse(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    string GridCellId,
    int FireCount,
    double TotalAreaBurned,
    string AverageSeverity,
    int PeakMonth,
    int PeakHour,
    string? CommonWindDirection,
    double AverageFwi,
    string RiskLevel,
    DateTime LastUpdated);

public sealed class GetHotspotsHandler(GeoRiskDbContext _db, HotspotAnalysisService service)
    : IQueryHandler<GetHotspotsQuery, List<HotspotResponse>>
{
    public async Task<List<HotspotResponse>> HandleAsync(GetHotspotsQuery query, CancellationToken ct)
    {
        var hotspots = await service.GetHotspotsAsync(query.RiskLevel, ct);
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

public static class GetHotspotsEndpoint
{
    public static RouteGroupBuilder MapHotspots(this RouteGroupBuilder group)
    {
        group.MapGet("/hotspots", async (
            string? riskLevel,
            IQueryHandler<GetHotspotsQuery, List<HotspotResponse>> handler,
            ILogger<GetHotspotsHandler> logger) =>
        {
            try
            {
                RiskLevel? risk = null;
                if (!string.IsNullOrEmpty(riskLevel) && Enum.TryParse<RiskLevel>(riskLevel, true, out var parsed))
                {
                    risk = parsed;
                }

                var result = await handler.HandleAsync(new GetHotspotsQuery(risk), default);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Get hotspots error");
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).WithTags("Insights");

        return group;
    }
}