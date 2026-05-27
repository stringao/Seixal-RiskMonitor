using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.Features.Alerts;

public static class AlertsEndpoints
{
    public static RouteGroupBuilder MapAlerts(this RouteGroupBuilder group)
    {
        group.MapGetAlerts();
        group.MapGetActiveAlerts();
        group.MapMarkAlertRead();
        group.MapGetAlertHistory();
        group.MapConfigureAlertRules();
        group.MapGetRuleConditions();
        group.MapTestAlertRule();
        group.MapCloneAlertRule();

        group.MapGet("/rules", async (GeoRiskDbContext db) =>
        {
            var rules = await db.AlertRules.AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AlertRuleResponse(
                    r.Id, r.Name,
                    r.EventType.HasValue ? Enum.GetName(r.EventType.Value) : null,
                    r.SeverityThreshold.HasValue ? Enum.GetName(r.SeverityThreshold.Value) : null,
                    r.Area != null ? r.Area.AsText() : null,
                    r.IsActive, r.CreatedAt,
                    r.MinFwi, r.MaxFwi, r.MinWindSpeed, r.MinTemperature,
                    r.SeasonStartMonth, r.SeasonEndMonth,
                    r.AreaKm2Threshold, r.ConsecutiveCount,
                    r.EscalationMinutes, r.NotifyRoles))
                .ToListAsync();
            return Results.Ok(new AlertRuleListResponse(rules));
        }).RequireAuthorization("AnalystOrAdmin");

        return group;
    }
}

public class AlertTriggerService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AlertTriggerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task EvaluateRulesAsync(GeoRiskDbContext db, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var evaluationEngine = scope.ServiceProvider.GetRequiredService<AlertRuleEvaluationEngine>();
        await evaluationEngine.EvaluateAllRulesAsync(ct);
    }
}
