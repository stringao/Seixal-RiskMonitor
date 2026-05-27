using GeoRisk.API.Infrastructure.Cache;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.BackgroundJobs;

public sealed class EventImportJob(
    IIcnfClient icnf, IIpmaClient ipma, IAnepcClient anepc,
    IServiceScopeFactory scopeFactory, ISyncStatusService syncStatus,
    ILogger<EventImportJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        while (await timer.WaitForNextTickAsync(ct))
        {
            await ImportEventsAsync(ct);
        }
    }

    private async Task ImportEventsAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting event import job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

            var existingSourceIds = await db.GeoEvents
                .AsNoTracking()
                .Where(e => e.SourceId != null)
                .Select(e => e.SourceId!)
                .ToListAsync(ct);

            await ImportFromIcnfAsync(db, ct, existingSourceIds);
            await ImportFromAnepcAsync(db, ct, existingSourceIds);
            await ImportIpmaFireRiskAsync(db, ct, existingSourceIds);

            logger.LogInformation("Event import job completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Event import job failed");
        }
    }

    private async Task ImportFromIcnfAsync(GeoRiskDbContext db, CancellationToken ct, List<string> existingSourceIds)
    {
        try
        {
            var fires = await icnf.GetActiveFiresAsync(ct);
            var newFires = fires.Where(f => !existingSourceIds.Contains(f.Id)).ToList();

            foreach (var fire in newFires)
            {
                var geoEvent = new GeoEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = EventType.Fire,
                    Title = $"Fire in {fire.County}",
                    Description = $"Active fire reported in {fire.Region} region, {fire.County}. Area: {fire.AreaHa}ha",
                    Geometry = new Point(fire.Longitude, fire.Latitude) { SRID = 4326 },
                    Severity = DetermineSeverity(fire.AreaHa),
                    Source = EventSource.ICNF,
                    OccurredAt = fire.DetectedAt,
                    SourceId = fire.Id,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { fire.AreaHa, fire.Status })
                };

                db.GeoEvents.Add(geoEvent);
            }

            await db.SaveChangesAsync(ct);
            await syncStatus.RecordSuccessAsync("icnf", newFires.Count, ct);
            logger.LogInformation("Imported {Count} ICNF events", newFires.Count());
        }
        catch (Exception ex)
        {
            await syncStatus.RecordFailureAsync("icnf", ex.Message, ct);
            logger.LogError(ex, "Failed to import from ICNF");
            throw;
        }
    }

    private async Task ImportFromAnepcAsync(GeoRiskDbContext db, CancellationToken ct, List<string> existingSourceIds)
    {
        try
        {
            var emergencies = await anepc.GetActiveEmergenciesAsync(ct);
            var newEmergencies = emergencies.Where(e => !existingSourceIds.Contains(e.Id)).ToList();

            foreach (var em in newEmergencies)
            {
                var eventType = em.Type switch
                {
                    "Fire" => EventType.Fire,
                    "Flood" => EventType.Flood,
                    "Storm" => EventType.Storm,
                    _ => EventType.Other
                };

                var geoEvent = new GeoEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = eventType,
                    Title = $"{em.Type} emergency in {em.County}",
                    Description = $"{em.Type} emergency in {em.District} district, {em.County}. Status: {em.Status}",
                    Geometry = new Point(em.Longitude, em.Latitude) { SRID = 4326 },
                    Severity = RiskLevel.High,
                    Source = EventSource.ANEPC,
                    OccurredAt = em.DeclaredAt,
                    SourceId = em.Id,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { em.Status, em.AffectedPopulation })
                };

                db.GeoEvents.Add(geoEvent);
            }

            await db.SaveChangesAsync(ct);
            await syncStatus.RecordSuccessAsync("anepc", newEmergencies.Count, ct);
            logger.LogInformation("Imported {Count} ANEPC events", newEmergencies.Count);
        }
        catch (Exception ex)
        {
            await syncStatus.RecordFailureAsync("anepc", ex.Message, ct);
            logger.LogError(ex, "Failed to import from ANEPC");
            throw;
        }
    }

    private async Task ImportIpmaFireRiskAsync(GeoRiskDbContext db, CancellationToken ct, List<string> existingSourceIds)
    {
        try
        {
            var fireRisks = await ipma.GetFireRiskAsync(ct);
            var count = 0;

            foreach (var risk in fireRisks.Where(r => r.RiskLevel is "Very High" or "High"))
            {
                var sourceId = $"IPMA-FIRE-{risk.County}-{risk.Latitude:F4}-{risk.Longitude:F4}";
                if (existingSourceIds.Contains(sourceId)) continue;

                var geoEvent = new GeoEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = EventType.Fire,
                    Title = $"High fire risk in {risk.County}",
                    Description = $"IPMA fire risk index: {risk.RiskIndex} ({risk.RiskLevel})",
                    Geometry = new Point(risk.Longitude, risk.Latitude) { SRID = 4326 },
                    Severity = RiskLevel.Medium,
                    Source = EventSource.IPMA,
                    OccurredAt = DateTime.UtcNow,
                    SourceId = sourceId,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { risk.RiskIndex, risk.RiskLevel })
                };

                db.GeoEvents.Add(geoEvent);
                count++;
            }

            await db.SaveChangesAsync(ct);
            await syncStatus.RecordSuccessAsync("ipma", count, ct);
            logger.LogInformation("Imported IPMA fire risk events");
        }
        catch (Exception ex)
        {
            await syncStatus.RecordFailureAsync("ipma", ex.Message, ct);
            logger.LogError(ex, "Failed to import from IPMA");
            throw;
        }
    }

    private static RiskLevel DetermineSeverity(double? areaHa) => areaHa switch
    {
        > 10 => RiskLevel.Critical,
        > 5 => RiskLevel.High,
        > 1 => RiskLevel.Medium,
        _ => RiskLevel.Low
    };
}