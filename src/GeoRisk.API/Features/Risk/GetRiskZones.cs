using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Risk.Dto;
using Microsoft.EntityFrameworkCore;
using GeoRisk.API.Infrastructure.Services;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Risk;

public sealed record GetRiskZonesQuery(Guid? ZoneId) : IQuery<List<RiskZoneResponse>>;

public sealed class GetRiskZonesHandler(GeoRiskDbContext db, RiskCalculationService calc)
    : IQueryHandler<GetRiskZonesQuery, List<RiskZoneResponse>>
{
    public async Task<List<RiskZoneResponse>> HandleAsync(GetRiskZonesQuery query, CancellationToken ct)
    {
        var zones = query.ZoneId.HasValue
            ? await db.RiskZones.Where(z => z.Id == query.ZoneId.Value).ToListAsync(ct)
            : await db.RiskZones.AsNoTracking().ToListAsync(ct);

        var scores = await calc.CalculateZoneScoresAsync(db, ct);

        return zones.Select(z => new RiskZoneResponse(
            z.Id, z.Name, z.Geometry.AsText(),
            calc.ScoreToRiskLevel(scores.GetValueOrDefault(z.Id, 0)),
            Math.Round(scores.GetValueOrDefault(z.Id, 0), 2),
            z.CalculatedAt)).ToList();
    }
}

public static class GetRiskZonesEndpoint
{
    public static RouteGroupBuilder MapGetRiskZones(this RouteGroupBuilder group)
    {
        group.MapGet("/zones", async (Guid? zoneId, IQueryHandler<GetRiskZonesQuery, List<RiskZoneResponse>> h) =>
            Results.Ok(await h.HandleAsync(new GetRiskZonesQuery(zoneId), default)))
            .RequireAuthorization();
        return group;
    }
}
