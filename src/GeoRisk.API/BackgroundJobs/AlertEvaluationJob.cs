using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.BackgroundJobs;

public sealed class AlertEvaluationJob(
    IServiceScopeFactory scopeFactory, ILogger<AlertEvaluationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(ct))
        {
            await EvaluateAlertsAsync(ct);
        }
    }

    private async Task EvaluateAlertsAsync(CancellationToken ct)
    {
        logger.LogDebug("Starting alert evaluation job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

            var activeRules = await db.AlertRules
                .Where(r => r.IsActive)
                .AsNoTracking()
                .ToListAsync(ct);

            if (activeRules.Count == 0) return;

            var recentEvents = await db.GeoEvents
                .Where(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var rule in activeRules)
            {
                var matchingEvents = EvaluateRule(rule, recentEvents).ToList();
                if (matchingEvents.Count > 0)
                {
                    var name = rule.Name;
                    var count = matchingEvents.Count;
                    logger.LogInformation("ALERT TRIGGERED: Rule {RuleName} matched {EventCount} events", name, count);

                    foreach (var evt in matchingEvents)
                    {
                        var existingAlert = await db.Alerts
                            .AnyAsync(a => a.GeoEventId == evt.Id, ct);

                        if (!existingAlert)
                        {
                            var alert = new Alert
                            {
                                Id = Guid.NewGuid(),
                                GeoEventId = evt.Id,
                                Severity = AlertSeverity.Warning,
                                Message = $"Alert rule '{rule.Name}' triggered by event: {evt.Title}",
                                CreatedAt = DateTime.UtcNow
                            };
                            db.Alerts.Add(alert);
                        }
                    }
                }
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Alert evaluation job failed");
        }
    }

    private static IEnumerable<GeoEvent> EvaluateRule(AlertRule rule, List<GeoEvent> events)
    {
        return events.Where(e =>
            (rule.EventType == null || rule.EventType == e.EventType) &&
            (rule.SeverityThreshold == null || e.Severity >= rule.SeverityThreshold));
    }
}