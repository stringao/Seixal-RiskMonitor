using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

public class RiskCalculationService
{
    private static readonly int[] SeverityWeights = { 1, 3, 7, 15 };
    private const double DecayHalfLifeDays = 30.0;

    public async Task<Dictionary<Guid, double>> CalculateZoneScoresAsync(
        GeoRiskDbContext db,
        CancellationToken ct = default)
    {
        var zones = await db.RiskZones.AsNoTracking().ToListAsync(ct);
        var now = DateTime.UtcNow;
        var scores = new Dictionary<Guid, double>();

        foreach (var zone in zones)
        {
            var eventsInZone = await db.GeoEvents
                .Where(e => zone.Geometry.Contains(e.Geometry))
                .Select(e => new { e.Severity, e.OccurredAt })
                .ToListAsync(ct);

            double score = 0;
            foreach (var evt in eventsInZone)
            {
                var weight = SeverityWeights[(int)evt.Severity];
                var daysOld = (now - evt.OccurredAt).TotalDays;
                var decay = Math.Pow(0.5, daysOld / DecayHalfLifeDays);
                score += weight * decay;
            }

            scores[zone.Id] = Math.Min(score, 100);
        }

        return scores;
    }

    public RiskLevel ScoreToRiskLevel(double score) => score switch
    {
        >= 60 => RiskLevel.Critical,
        >= 30 => RiskLevel.High,
        >= 10 => RiskLevel.Medium,
        _ => RiskLevel.Low
    };
}
