using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Alerts;

public sealed record ConfigureAlertRulesCommand(
    ConfigureAlertRulesAction Action,
    Guid? RuleId,
    CreateAlertRuleRequest? CreateRequest,
    UpdateAlertRuleRequest? UpdateRequest) : ICommand<AlertRuleResponse>;

public enum ConfigureAlertRulesAction { Create, Update, Delete, ToggleActive }

public sealed class ConfigureAlertRulesHandler(
    GeoRiskDbContext db,
    AlertTriggerService trigger) : ICommandHandler<ConfigureAlertRulesCommand, AlertRuleResponse>
{
    public async Task<AlertRuleResponse> HandleAsync(ConfigureAlertRulesCommand command, CancellationToken ct)
    {
        return command.Action switch
        {
            ConfigureAlertRulesAction.Create => await CreateRuleAsync(command.CreateRequest!, ct),
            ConfigureAlertRulesAction.Update => await UpdateRuleAsync(command.RuleId!.Value, command.UpdateRequest!, ct),
            ConfigureAlertRulesAction.Delete => await DeleteRuleAsync(command.RuleId!.Value, ct),
            ConfigureAlertRulesAction.ToggleActive => await ToggleActiveAsync(command.RuleId!.Value, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Action))
        };
    }

    private async Task<AlertRuleResponse> CreateRuleAsync(CreateAlertRuleRequest request, CancellationToken ct)
    {
        Polygon? area = null;
        if (!string.IsNullOrWhiteSpace(request.AreaWkt))
        {
            try
            {
                area = (Polygon)new NetTopologySuite.IO.WKTReader().Read(request.AreaWkt);
            }
            catch
            {
                throw new InvalidOperationException("Invalid WKT geometry format");
            }
        }

        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            EventType = request.EventType,
            SeverityThreshold = request.SeverityThreshold,
            Area = area,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        db.AlertRules.Add(rule);
        await db.SaveChangesAsync(ct);
        await trigger.EvaluateRulesAsync(db, ct);

        return ToResponse(rule);
    }

    private async Task<AlertRuleResponse> UpdateRuleAsync(Guid ruleId, UpdateAlertRuleRequest request, CancellationToken ct)
    {
        var rule = await db.AlertRules.FindAsync([ruleId], ct)
            ?? throw new InvalidOperationException($"Rule {ruleId} not found");

        if (request.Name is not null) rule.Name = request.Name;
        if (request.EventType is not null) rule.EventType = request.EventType;
        if (request.SeverityThreshold is not null) rule.SeverityThreshold = request.SeverityThreshold;
        if (request.AreaWkt is not null)
        {
            rule.Area = string.IsNullOrWhiteSpace(request.AreaWkt)
                ? null
                : TryParseWkt(request.AreaWkt);
        }
        if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync(ct);
        await trigger.EvaluateRulesAsync(db, ct);

        return ToResponse(rule);
    }

    private async Task<AlertRuleResponse> DeleteRuleAsync(Guid ruleId, CancellationToken ct)
    {
        var rule = await db.AlertRules.FindAsync([ruleId], ct)
            ?? throw new InvalidOperationException($"Rule {ruleId} not found");

        db.AlertRules.Remove(rule);
        await db.SaveChangesAsync(ct);
        return ToResponse(rule);
    }

    private async Task<AlertRuleResponse> ToggleActiveAsync(Guid ruleId, CancellationToken ct)
    {
        var rule = await db.AlertRules.FindAsync([ruleId], ct)
            ?? throw new InvalidOperationException($"Rule {ruleId} not found");

        rule.IsActive = !rule.IsActive;
        await db.SaveChangesAsync(ct);
        return ToResponse(rule);
    }

    private static AlertRuleResponse ToResponse(AlertRule r) =>
        new(r.Id, r.Name, r.EventType, r.SeverityThreshold, r.Area?.AsText(), r.IsActive, r.CreatedAt);

    private static Polygon TryParseWkt(string wkt)
    {
        try
        {
            return (Polygon)new NetTopologySuite.IO.WKTReader().Read(wkt);
        }
        catch
        {
            throw new InvalidOperationException("Invalid WKT geometry format");
        }
    }
}

public static class ConfigureAlertRulesEndpoint
{
    public static RouteGroupBuilder MapConfigureAlertRules(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            ConfigureAlertRulesRequest request,
            ICommandHandler<ConfigureAlertRulesCommand, AlertRuleResponse> handler) =>
        {
            var result = await handler.HandleAsync(request.ToCommand(), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin");

        group.MapDelete("/{ruleId:guid}", async (
            Guid ruleId,
            ICommandHandler<ConfigureAlertRulesCommand, AlertRuleResponse> handler) =>
        {
            var result = await handler.HandleAsync(
                new ConfigureAlertRulesCommand(ConfigureAlertRulesAction.Delete, ruleId, null, null), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin");

        group.MapPatch("/{ruleId:guid}/toggle", async (
            Guid ruleId,
            ICommandHandler<ConfigureAlertRulesCommand, AlertRuleResponse> handler) =>
        {
            var result = await handler.HandleAsync(
                new ConfigureAlertRulesCommand(ConfigureAlertRulesAction.ToggleActive, ruleId, null, null), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin");

        return group;
    }
}

public sealed record ConfigureAlertRulesRequest(
    string Action,
    Guid? RuleId,
    CreateAlertRuleRequest? CreateRequest,
    UpdateAlertRuleRequest? UpdateRequest)
{
    public ConfigureAlertRulesCommand ToCommand()
    {
        if (!Enum.TryParse<ConfigureAlertRulesAction>(Action, ignoreCase: true, out var action))
        {
            throw new InvalidOperationException($"Invalid action: {Action}");
        }
        return new ConfigureAlertRulesCommand(action, RuleId, CreateRequest, UpdateRequest);
    }
}