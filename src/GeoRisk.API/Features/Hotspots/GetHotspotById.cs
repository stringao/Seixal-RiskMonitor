using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Infrastructure.Services;

namespace GeoRisk.API.Features.Hotspots;

public sealed record GetHotspotByIdQuery(Guid HotspotId) : IQuery<HotspotDetailResponse?>;

public sealed record HotspotDetailResponse(
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
    DateTime LastUpdated,
    List<HotspotAlertResponse> Alerts,
    SeasonalPatternResponse? SeasonalPattern,
    List<HotspotEventResponse> RecentEvents);

public sealed record HotspotAlertResponse(
    Guid Id,
    string AlertType,
    string Message,
    DateTime CreatedAt,
    bool IsRead);

public sealed record SeasonalPatternResponse(Dictionary<int, int> FiresByMonth);

public sealed record HotspotEventResponse(
    Guid Id,
    string Title,
    string Severity,
    DateTime OccurredAt,
    double Latitude,
    double Longitude);

public sealed class GetHotspotByIdHandler(GeoRiskDbContext _db, HotspotAnalysisService service)
    : IQueryHandler<GetHotspotByIdQuery, HotspotDetailResponse?>
{
    public async Task<HotspotDetailResponse?> HandleAsync(GetHotspotByIdQuery query, CancellationToken ct)
    {
        var hotspot = await service.GetHotspotByIdAsync(query.HotspotId, ct);
        if (hotspot == null) return null;

        var events = await service.GetHotspotEventsAsync(hotspot.GridCellId, years: 2, ct);
        var seasonalPattern = await service.GetSeasonalPatternAsync(hotspot.GridCellId, ct);

        return new HotspotDetailResponse(
            hotspot.Id,
            hotspot.Name,
            hotspot.Location.Y,
            hotspot.Location.X,
            hotspot.GridCellId,
            hotspot.FireCount,
            hotspot.TotalAreaBurned,
            hotspot.AverageSeverity.ToString(),
            hotspot.PeakMonth,
            hotspot.PeakHour,
            hotspot.CommonWindDirection,
            hotspot.AverageFwi,
            hotspot.RiskLevel.ToString(),
            hotspot.LastUpdated,
            hotspot.Alerts.Select(a => new HotspotAlertResponse(
                a.Id,
                a.AlertType,
                a.Message,
                a.CreatedAt,
                a.IsRead)).ToList(),
            new SeasonalPatternResponse(seasonalPattern),
            events.Take(20).Select(e => new HotspotEventResponse(
                e.Id,
                e.Title,
                e.Severity.ToString(),
                e.OccurredAt,
                e.Geometry.Y,
                e.Geometry.X)).ToList());
    }
}

public static class GetHotspotByIdEndpoint
{
    public static RouteGroupBuilder MapHotspotById(this RouteGroupBuilder group)
    {
        group.MapGet("/hotspots/{id:guid}", async (
            Guid id,
            IQueryHandler<GetHotspotByIdQuery, HotspotDetailResponse?> handler,
            ILogger<GetHotspotByIdHandler> logger) =>
        {
            try
            {
                var result = await handler.HandleAsync(new GetHotspotByIdQuery(id), default);
                return result == null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Get hotspot by ID error");
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).WithTags("Insights");

        return group;
    }
}