using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Cache;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Health;

public sealed record GetJobStatusesQuery() : IQuery<IReadOnlyList<JobStatusResponse>>;

public sealed record JobStatusResponse(
    string Name,
    string Description,
    DateTime? LastRun,
    string Status,
    string? Error,
    string? Source,
    string? DisplayName,
    int ItemsSyncedLastRun,
    string? LastError);

public sealed class GetJobStatusesHandler : IQueryHandler<GetJobStatusesQuery, IReadOnlyList<JobStatusResponse>>
{
    private readonly ISyncStatusService _syncStatus;
    private static DateTime _lastAiClassification;
    private static DateTime _lastPatternDetection;
    private static DateTime _lastReportGeneration;
    private static DateTime _lastAlertEvaluation;
    private const string RunningStatus = "Running";
    private const string AnepcSource = "ANEPC";

    public GetJobStatusesHandler(ISyncStatusService syncStatus)
    {
        _syncStatus = syncStatus;
    }

    public async Task<IReadOnlyList<JobStatusResponse>> HandleAsync(GetJobStatusesQuery query, CancellationToken ct)
    {
        var syncStatuses = await _syncStatus.GetAllStatusesAsync(ct);

        var statuses = new List<JobStatusResponse>
        {
            new("EventImportJob", "Imports events from ICNF/IPMA/ANEPC every 30 minutes",
                syncStatuses.FirstOrDefault(s => s.Source == "ICNF")?.LastSuccessAt, RunningStatus, null,
                "ICNF", "ICNF (Incêndios)",
                syncStatuses.FirstOrDefault(s => s.Source == "ICNF")?.ItemsSyncedLastRun ?? 0,
                syncStatuses.FirstOrDefault(s => s.Source == "ICNF")?.LastError),
            new("EventImportJob", "Imports events from ICNF/IPMA/ANEPC every 30 minutes",
                syncStatuses.FirstOrDefault(s => s.Source == AnepcSource)?.LastSuccessAt, RunningStatus, null,
                AnepcSource, "ANEPC (Emergências)",
                syncStatuses.FirstOrDefault(s => s.Source == AnepcSource)?.ItemsSyncedLastRun ?? 0,
                syncStatuses.FirstOrDefault(s => s.Source == AnepcSource)?.LastError),
            new("EventImportJob", "Imports events from ICNF/IPMA/ANEPC every 30 minutes",
                syncStatuses.FirstOrDefault(s => s.Source == "IPMA")?.LastSuccessAt, RunningStatus, null,
                "IPMA", "IPMA (Risco de Fogo)",
                syncStatuses.FirstOrDefault(s => s.Source == "IPMA")?.ItemsSyncedLastRun ?? 0,
                syncStatuses.FirstOrDefault(s => s.Source == "IPMA")?.LastError),
            new("AIClassificationJob", "Classifies unclassified events every 5 minutes",
                _lastAiClassification == default ? null : _lastAiClassification, RunningStatus, null, null, null, 0, null),
            new("PatternDetectionJob", "Detects event patterns daily at 02:00",
                _lastPatternDetection == default ? null : _lastPatternDetection, RunningStatus, null, null, null, 0, null),
            new("ReportGenerationJob", "Generates weekly reports on Monday at 06:00",
                _lastReportGeneration == default ? null : _lastReportGeneration, RunningStatus, null, null, null, 0, null),
            new("AlertEvaluationJob", "Evaluates alert rules every 5 minutes",
                _lastAlertEvaluation == default ? null : _lastAlertEvaluation, RunningStatus, null, null, null, 0, null)
        };
        return statuses;
    }

    public static void RecordRun(string jobName)
    {
        var now = DateTime.UtcNow;
        switch (jobName)
        {
            case "AIClassificationJob": _lastAiClassification = now; break;
            case "PatternDetectionJob": _lastPatternDetection = now; break;
            case "ReportGenerationJob": _lastReportGeneration = now; break;
            case "AlertEvaluationJob": _lastAlertEvaluation = now; break;
        }
    }
}

public static class GetJobStatusesEndpoint
{
    public static RouteGroupBuilder MapGetJobStatuses(this RouteGroupBuilder group)
    {
        group.MapGet("/jobs", async (IQueryHandler<GetJobStatusesQuery, IReadOnlyList<JobStatusResponse>> handler) =>
        {
            var result = await handler.HandleAsync(new GetJobStatusesQuery(), default);
            return Results.Ok(result);
        }).WithTags("Health");

        group.MapPost("/jobs/sync", async (
            IServiceScopeFactory scopeFactory,
            ISyncStatusService syncStatus,
            ILogger<Program> logger) =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

                var existingSourceIds = await db.GeoEvents
                    .AsNoTracking()
                    .Where(e => e.SourceId != null)
                    .Select(e => e.SourceId!)
                    .ToListAsync();

                var totalImported = await SyncAllSourcesAsync(scopeFactory, db, existingSourceIds, syncStatus, logger);

                return Results.Ok(new { success = true, imported = totalImported, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Manual sync failed");
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithTags("Health");

        return group;
    }

    private static async Task<int> SyncAllSourcesAsync(
        IServiceScopeFactory scopeFactory,
        GeoRiskDbContext db,
        List<string> existingSourceIds,
        ISyncStatusService syncStatus,
        ILogger logger)
    {
        int totalImported = 0;

        totalImported += await SyncIcnfFiresAsync(scopeFactory, db, existingSourceIds, syncStatus, logger);
        totalImported += await SyncAnepcEmergenciesAsync(scopeFactory, db, existingSourceIds, syncStatus, logger);
        totalImported += await SyncIpmaFireRisksAsync(scopeFactory, db, existingSourceIds, syncStatus, logger);

        return totalImported;
    }

    private static async Task<int> SyncIcnfFiresAsync(
        IServiceScopeFactory scopeFactory,
        GeoRiskDbContext db,
        List<string> existingSourceIds,
        ISyncStatusService syncStatus,
        ILogger logger)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var icnf = scope.ServiceProvider.GetRequiredService<IIcnfClient>();

            var fires = await icnf.GetActiveFiresAsync();
            var newFires = fires.Where(f => !existingSourceIds.Contains(f.Id)).ToList();

            foreach (var fire in newFires)
            {
                db.GeoEvents.Add(new GeoEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = EventType.Fire,
                    Title = $"Fire in {fire.County}",
                    Description = $"Active fire in {fire.Region}, {fire.County}. Area: {fire.AreaHa}ha",
                    Geometry = new Point(fire.Longitude, fire.Latitude) { SRID = 4326 },
                    Severity = DetermineFireSeverity(fire.AreaHa),
                    Source = EventSource.ICNF,
                    OccurredAt = fire.DetectedAt,
                    SourceId = fire.Id,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { fire.AreaHa, fire.Status })
                });
            }

            await db.SaveChangesAsync();
            await syncStatus.RecordSuccessAsync("icnf", newFires.Count);
            return newFires.Count;
        }
        catch (Exception ex)
        {
            await syncStatus.RecordFailureAsync("icnf", ex.Message);
            logger.LogError(ex, "ICNF sync failed");
            return 0;
        }
    }

    private static async Task<int> SyncAnepcEmergenciesAsync(
        IServiceScopeFactory scopeFactory,
        GeoRiskDbContext db,
        List<string> existingSourceIds,
        ISyncStatusService syncStatus,
        ILogger logger)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var anepc = scope.ServiceProvider.GetRequiredService<IAnepcClient>();

            var emergencies = await anepc.GetActiveEmergenciesAsync();
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
                db.GeoEvents.Add(new GeoEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = eventType,
                    Title = $"{em.Type} emergency in {em.County}",
                    Description = $"{em.Type} in {em.District}, {em.County}. Status: {em.Status}",
                    Geometry = new Point(em.Longitude, em.Latitude) { SRID = 4326 },
                    Severity = RiskLevel.High,
                    Source = EventSource.ANEPC,
                    OccurredAt = em.DeclaredAt,
                    SourceId = em.Id,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { em.Status, em.AffectedPopulation })
                });
            }

            await db.SaveChangesAsync();
            await syncStatus.RecordSuccessAsync("anepc", newEmergencies.Count);
            return newEmergencies.Count;
        }
        catch (Exception ex)
        {
            await syncStatus.RecordFailureAsync("anepc", ex.Message);
            logger.LogError(ex, "ANEPC sync failed");
            return 0;
        }
    }

    private static async Task<int> SyncIpmaFireRisksAsync(
        IServiceScopeFactory scopeFactory,
        GeoRiskDbContext db,
        List<string> existingSourceIds,
        ISyncStatusService syncStatus,
        ILogger logger)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var ipma = scope.ServiceProvider.GetRequiredService<IIpmaClient>();

            var fireRisks = await ipma.GetFireRiskAsync();
            int ipmaCount = 0;

            foreach (var risk in fireRisks.Where(r => r.RiskLevel is "Very High" or "High"))
            {
                var sourceId = $"IPMA-FIRE-{risk.County}-{risk.Latitude:F4}-{risk.Longitude:F4}";
                if (!existingSourceIds.Contains(sourceId))
                {
                    db.GeoEvents.Add(new GeoEvent
                    {
                        Id = Guid.NewGuid(),
                        EventType = EventType.Fire,
                        Title = $"High fire risk in {risk.County}",
                        Description = $"IPMA fire risk: {risk.RiskIndex} ({risk.RiskLevel})",
                        Geometry = new Point(risk.Longitude, risk.Latitude) { SRID = 4326 },
                        Severity = RiskLevel.Medium,
                        Source = EventSource.IPMA,
                        OccurredAt = DateTime.UtcNow,
                        SourceId = sourceId,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new { risk.RiskIndex, risk.RiskLevel })
                    });
                    ipmaCount++;
                }
            }

            await db.SaveChangesAsync();
            await syncStatus.RecordSuccessAsync("ipma", ipmaCount);
            return ipmaCount;
        }
        catch (Exception ex)
        {
            await syncStatus.RecordFailureAsync("ipma", ex.Message);
            logger.LogError(ex, "IPMA sync failed");
            return 0;
        }
    }

    private static RiskLevel DetermineFireSeverity(double? areaHa)
    {
        if (areaHa is null or 0) return RiskLevel.Low;
        if (areaHa > 10) return RiskLevel.Critical;
        if (areaHa > 5) return RiskLevel.High;
        if (areaHa > 1) return RiskLevel.Medium;
        return RiskLevel.Low;
    }
}
