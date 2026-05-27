using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Alerts;

public sealed record CloneAlertRuleCommand(
    Guid SourceRuleId) : ICommand<AlertRuleResponse>;

public sealed class CloneAlertRuleHandler(
    GeoRiskDbContext db,
    ILogger<CloneAlertRuleHandler> logger) : ICommandHandler<CloneAlertRuleCommand, AlertRuleResponse>
{
    public async Task<AlertRuleResponse> HandleAsync(CloneAlertRuleCommand command, CancellationToken ct)
    {
        var sourceRule = await db.AlertRules.FindAsync([command.SourceRuleId], ct);
        if (sourceRule == null)
        {
            throw new InvalidOperationException($"Rule {command.SourceRuleId} not found");
        }

        var clonedRule = new AlertRule
        {
            Id = Guid.NewGuid(),
            Name = $"{sourceRule.Name} (Copy)",
            EventType = sourceRule.EventType,
            SeverityThreshold = sourceRule.SeverityThreshold,
            Area = sourceRule.Area,
            IsActive = false, // Clone is created inactive by default
            CreatedAt = DateTime.UtcNow,
            MinFwi = sourceRule.MinFwi,
            MaxFwi = sourceRule.MaxFwi,
            MinWindSpeed = sourceRule.MinWindSpeed,
            MinTemperature = sourceRule.MinTemperature,
            SeasonStartMonth = sourceRule.SeasonStartMonth,
            SeasonEndMonth = sourceRule.SeasonEndMonth,
            AreaKm2Threshold = sourceRule.AreaKm2Threshold,
            ConsecutiveCount = sourceRule.ConsecutiveCount,
            EscalationMinutes = sourceRule.EscalationMinutes,
            NotifyRoles = sourceRule.NotifyRoles != null ? new List<string>(sourceRule.NotifyRoles) : null
        };

        db.AlertRules.Add(clonedRule);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Cloned alert rule {SourceRuleName} to new rule {NewRuleName} with ID {NewRuleId}",
            sourceRule.Name, clonedRule.Name, clonedRule.Id);

        return ToResponse(clonedRule);
    }

    private static AlertRuleResponse ToResponse(AlertRule r) => new(
        r.Id,
        r.Name,
        r.EventType?.ToString(),
        r.SeverityThreshold?.ToString(),
        r.Area?.AsText(),
        r.IsActive,
        r.CreatedAt,
        r.MinFwi,
        r.MaxFwi,
        r.MinWindSpeed,
        r.MinTemperature,
        r.SeasonStartMonth,
        r.SeasonEndMonth,
        r.AreaKm2Threshold,
        r.ConsecutiveCount,
        r.EscalationMinutes,
        r.NotifyRoles);
}

public static class CloneAlertRuleEndpoint
{
    public static RouteGroupBuilder MapCloneAlertRule(this RouteGroupBuilder group)
    {
        group.MapPost("/rules/clone/{id:guid}", async (
            Guid id,
            ICommandHandler<CloneAlertRuleCommand, AlertRuleResponse> handler) =>
        {
            var result = await handler.HandleAsync(new CloneAlertRuleCommand(id), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin");

        return group;
    }
}
