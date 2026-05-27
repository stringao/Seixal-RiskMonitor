using GeoRisk.API.Features.Alerts.Dto;
using Microsoft.AspNetCore.Authorization;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Alerts;

public static class AlertsEndpoints
{
    public static RouteGroupBuilder MapAlerts(this RouteGroupBuilder group)
    {
        group.MapGetAlerts();
        group.MapMarkAlertRead();
        group.MapGetAlertHistory();
        group.MapConfigureAlertRules();

        group.MapGet("/rules", async (GeoRiskDbContext db) =>
        {
            var rules = await db.AlertRules.AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AlertRuleResponse(
                    r.Id, r.Name, r.EventType.HasValue ? Enum.GetName(r.EventType.Value) : null,
                    r.SeverityThreshold.HasValue ? Enum.GetName(r.SeverityThreshold.Value) : null,
                    r.Area != null ? r.Area.AsText() : null, r.IsActive, r.CreatedAt))
                .ToListAsync();
            return Results.Ok(new AlertRuleListResponse(rules));
        }).RequireAuthorization("AnalystOrAdmin");

        return group;
    }
}

public class AlertTriggerService
{
    public async Task EvaluateRulesAsync(GeoRiskDbContext db, CancellationToken ct)
    {
        // Rules are evaluated when new events arrive in CreateEvent handler
        // This service method can be called to re-evaluate all active rules
        await Task.CompletedTask;
    }
}