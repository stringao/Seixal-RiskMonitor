using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeoRisk.API.BackgroundJobs;

public sealed class AlertEvaluationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<AlertEvaluationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await EvaluateAlertsAsync(stoppingToken);
        }
    }

    private async Task EvaluateAlertsAsync(CancellationToken ct)
    {
        logger.LogDebug("Starting alert evaluation job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();
            var evaluationEngine = scope.ServiceProvider.GetRequiredService<AlertRuleEvaluationEngine>();
            var notificationService = scope.ServiceProvider.GetRequiredService<AlertNotificationService>();

            // Evaluate all rules
            var evaluatedAlerts = await evaluationEngine.EvaluateAllRulesAsync(ct);

            // Check for escalations
            var escalatedAlerts = await evaluationEngine.CheckEscalationsAsync(ct);
            evaluatedAlerts.AddRange(escalatedAlerts);

            if (evaluatedAlerts.Count == 0) return;

            var newAlerts = new List<Alert>();

            foreach (var evaluated in evaluatedAlerts)
            {
                if (evaluated.GeoEvent == null || evaluated.AlertRule == null)
                    continue;

                // Check for duplicate alerts
                var existingAlert = evaluated.IsEscalation
                    ? await db.Alerts.AnyAsync(a => a.EscalatedFromAlertId == evaluated.EscalatedFromAlertId, ct)
                    : await db.Alerts.AnyAsync(a => a.GeoEventId == evaluated.GeoEvent.Id && a.AlertRuleId == evaluated.AlertRule.Id, ct);

                if (existingAlert)
                    continue;

                var alert = new Alert
                {
                    Id = Guid.NewGuid(),
                    GeoEventId = evaluated.GeoEvent.Id,
                    AlertRuleId = evaluated.AlertRule.Id,
                    FwiValue = evaluated.FwiValue,
                    WindSpeed = evaluated.WindSpeed,
                    Temperature = evaluated.Temperature,
                    AreaKm2 = evaluated.AreaKm2,
                    IsEscalated = evaluated.IsEscalation,
                    EscalatedFromAlertId = evaluated.EscalatedFromAlertId,
                    Severity = evaluated.SuggestedSeverity,
                    Title = BuildAlertTitle(evaluated),
                    Message = BuildAlertMessage(evaluated),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = CalculateExpiry(evaluated)
                };

                db.Alerts.Add(alert);
                newAlerts.Add(alert);

                logger.LogInformation(
                    "ALERT TRIGGERED: Rule {RuleName} (Severity: {Severity}) - Event: {EventTitle}",
                    evaluated.AlertRule.Name,
                    evaluated.SuggestedSeverity,
                    evaluated.GeoEvent.Title);
            }

            await db.SaveChangesAsync(ct);

            // Queue notifications for newly created alerts
            if (newAlerts.Count > 0)
            {
                foreach (var alert in newAlerts)
                {
                    await notificationService.QueueNotificationsForAlertAsync(alert, ct: ct);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Alert evaluation job failed");
        }
    }

    private static string BuildAlertTitle(EvaluatedAlert evaluated)
    {
        var ruleName = evaluated.AlertRule?.Name ?? "Unknown Rule";
        var eventType = evaluated.GeoEvent?.EventType.ToString() ?? "Event";
        return evaluated.IsEscalation
            ? $"ESCALATED: {ruleName}"
            : $"{eventType} Alert: {ruleName}";
    }

    private static string BuildAlertMessage(EvaluatedAlert evaluated)
    {
        var evt = evaluated.GeoEvent;
        var rule = evaluated.AlertRule;
        if (evt == null || rule == null)
            return "Alert triggered";

        var parts = new List<string>();

        if (evaluated.FwiValue.HasValue)
            parts.Add($"FWI: {evaluated.FwiValue:F1}");
        if (evaluated.WindSpeed.HasValue)
            parts.Add($"Wind: {evaluated.WindSpeed:F1} km/h");
        if (evaluated.Temperature.HasValue)
            parts.Add($"Temp: {evaluated.Temperature:F1}°C");
        if (evaluated.AreaKm2.HasValue)
            parts.Add($"Area: {evaluated.AreaKm2:F2} km²");

        var conditions = parts.Count > 0 ? $" Conditions: {string.Join(", ", parts)}" : "";
        return $"Rule '{rule.Name}' triggered by event: {evt.Title}.{conditions}";
    }

    private static DateTime CalculateExpiry(EvaluatedAlert evaluated)
    {
        // Default expiry: 24 hours for regular alerts, 48 hours for escalated
        return evaluated.IsEscalation
            ? DateTime.UtcNow.AddHours(48)
            : DateTime.UtcNow.AddHours(24);
    }
}
