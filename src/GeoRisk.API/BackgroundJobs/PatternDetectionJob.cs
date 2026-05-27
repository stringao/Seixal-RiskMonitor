using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.BackgroundJobs;

public sealed class PatternDetectionJob(
    IServiceScopeFactory scopeFactory, ILogger<PatternDetectionJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(ct))
        {
            await DetectPatternsAsync(ct);
        }
    }

    private async Task DetectPatternsAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting pattern detection job at {Time}", DateTime.UtcNow);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

            await DetectClusterPatternsAsync(ct);
            await DetectTemporalPatternsAsync(ct);
            await DetectEscalationPatternsAsync(ct);

            logger.LogInformation("Pattern detection job completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pattern detection job failed");
        }
    }

    private async Task DetectClusterPatternsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recentEvents = await db.GeoEvents
            .Where(e => e.OccurredAt >= DateTime.UtcNow.AddDays(-7))
            .AsNoTracking()
            .ToListAsync(ct);

        var fireEvents = recentEvents.Where(e => e.EventType == EventType.Fire).ToList();
        if (fireEvents.Count >= 3)
        {
            logger.LogWarning("CLUSTER ALERT: {Count} fire events detected in the last 7 days. Possible wildfire cluster forming.", fireEvents.Count);
        }
    }

    private async Task DetectTemporalPatternsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var dailyCounts = await db.GeoEvents
            .Where(e => e.OccurredAt >= thirtyDaysAgo)
            .AsNoTracking()
            .GroupBy(e => e.OccurredAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var avgDaily = dailyCounts.Any() ? dailyCounts.Average(x => x.Count) : 0;
        var todayCount = dailyCounts.FirstOrDefault(x => x.Date == DateTime.UtcNow.Date)?.Count ?? 0;

        if (todayCount > avgDaily * 2 && todayCount > 5)
        {
            logger.LogWarning("TEMPORAL ALERT: Today's event count ({Today}) is significantly higher than average ({Avg:F1})", todayCount, avgDaily);
        }
    }

    private async Task DetectEscalationPatternsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

        var recentHighSeverity = await db.GeoEvents
            .Where(e => e.Severity >= RiskLevel.High && e.OccurredAt >= DateTime.UtcNow.AddHours(-24))
            .AsNoTracking()
            .CountAsync(ct);

        if (recentHighSeverity >= 5)
        {
            logger.LogWarning("ESCALATION ALERT: {Count} high-severity events in the last 24 hours. Risk conditions are elevating.", recentHighSeverity);
        }
    }
}