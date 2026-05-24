using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Risk.Dto;
using Microsoft.EntityFrameworkCore;
using GeoRisk.API.Infrastructure.Services;

namespace GeoRisk.API.Features.Risk;

public sealed record GetRiskDashboardQuery : IQuery<RiskDashboardResponse>;

public sealed class GetRiskDashboardHandler(GeoRiskDbContext db, RiskCalculationService calc)
    : IQueryHandler<GetRiskDashboardQuery, RiskDashboardResponse>
{
    public async Task<RiskDashboardResponse> HandleAsync(GetRiskDashboardQuery query, CancellationToken ct)
    {
        var zones = await db.RiskZones.AsNoTracking().ToListAsync(ct);
        var scores = await calc.CalculateZoneScoresAsync(db, ct);

        var zonesWithScores = zones.Select(z => new RiskZoneResponse(
            z.Id, z.Name, z.Geometry.AsText(),
            calc.ScoreToRiskLevel(scores.GetValueOrDefault(z.Id, 0)),
            Math.Round(scores.GetValueOrDefault(z.Id, 0), 2),
            z.CalculatedAt)).ToList();

        return new RiskDashboardResponse(
            zonesWithScores.Count,
            zonesWithScores.Count(z => z.RiskLevel == RiskLevel.Critical),
            zonesWithScores.Count(z => z.RiskLevel == RiskLevel.High),
            zonesWithScores);
    }
}

public static class GetRiskDashboardEndpoint
{
    public static RouteGroupBuilder MapGetRiskDashboard(this RouteGroupBuilder group)
    {
        group.MapGet("/dashboard", async (IQueryHandler<GetRiskDashboardQuery, RiskDashboardResponse> h) =>
            Results.Ok(await h.HandleAsync(new GetRiskDashboardQuery(), default)))
            .RequireAuthorization();
        return group;
    }
}
