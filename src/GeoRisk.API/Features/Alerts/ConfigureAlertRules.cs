using System.Text.Json;
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
    AlertRuleEvaluationEngine _evaluationEngine,
    ILogger<ConfigureAlertRulesHandler> logger) : ICommandHandler<ConfigureAlertRulesCommand, AlertRuleResponse>
{
    public async Task<AlertRuleResponse> HandleAsync(ConfigureAlertRulesCommand command, CancellationToken ct)
    {
        return command.Action switch
        {
            ConfigureAlertRulesAction.Create => await CreateRuleAsync(command.CreateRequest!, ct),
            ConfigureAlertRulesAction.Update => await UpdateRuleAsync(command.RuleId!.Value, command.UpdateRequest!, ct),
            ConfigureAlertRulesAction.Delete => await DeleteRuleAsync(command.RuleId!.Value, ct),
            ConfigureAlertRulesAction.ToggleActive => await ToggleActiveAsync(command.RuleId!.Value, ct),
            _ => throw new ArgumentOutOfRangeException("command.Action")
        };
    }

    private async Task<AlertRuleResponse> CreateRuleAsync(CreateAlertRuleRequest request, CancellationToken ct)
    {
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            EventType = TryParseEventType(request.EventType),
            SeverityThreshold = TryParseRiskLevel(request.SeverityThreshold),
            Area = ParseArea(request.AreaWkt),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            MinFwi = request.MinFwi,
            MaxFwi = request.MaxFwi,
            MinWindSpeed = request.MinWindSpeed,
            MinTemperature = request.MinTemperature,
            SeasonStartMonth = request.SeasonStartMonth,
            SeasonEndMonth = request.SeasonEndMonth,
            AreaKm2Threshold = request.AreaKm2Threshold,
            ConsecutiveCount = request.ConsecutiveCount,
            EscalationMinutes = request.EscalationMinutes,
            NotifyRoles = request.NotifyRoles
        };

        ValidateRule(rule);

        db.AlertRules.Add(rule);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created alert rule {RuleName} with ID {RuleId}", rule.Name, rule.Id);

        return ToResponse(rule);
    }

    private async Task<AlertRuleResponse> UpdateRuleAsync(Guid ruleId, UpdateAlertRuleRequest request, CancellationToken ct)
    {
        var rule = await db.AlertRules.FindAsync([ruleId], ct)
            ?? throw new InvalidOperationException($"Rule {ruleId} not found");

        ApplyUpdateToRule(rule, request);
        ValidateRule(rule);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated alert rule {RuleName} with ID {RuleId}", rule.Name, rule.Id);

        return ToResponse(rule);
    }

    private static void ApplyUpdateToRule(AlertRule rule, UpdateAlertRuleRequest request)
    {
        ApplyBasicFields(rule, request);
        ApplyNumericThresholds(rule, request);
        ApplySeasonalFields(rule, request);
        ApplyNotificationFields(rule, request);
    }

    private static void ApplyBasicFields(AlertRule rule, UpdateAlertRuleRequest request)
    {
        if (request.Name is not null) rule.Name = request.Name;
        if (request.EventType is not null) rule.EventType = TryParseEventType(request.EventType);
        if (request.SeverityThreshold is not null) rule.SeverityThreshold = TryParseRiskLevel(request.SeverityThreshold);
        if (request.AreaWkt is not null)
        {
            rule.Area = string.IsNullOrWhiteSpace(request.AreaWkt) ? null : ParseArea(request.AreaWkt);
        }
        if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;
    }

    private static void ApplyNumericThresholds(AlertRule rule, UpdateAlertRuleRequest request)
    {
        if (request.MinFwi.HasValue) rule.MinFwi = request.MinFwi;
        if (request.MaxFwi.HasValue) rule.MaxFwi = request.MaxFwi;
        if (request.MinWindSpeed.HasValue) rule.MinWindSpeed = request.MinWindSpeed;
        if (request.MinTemperature.HasValue) rule.MinTemperature = request.MinTemperature;
        if (request.AreaKm2Threshold.HasValue) rule.AreaKm2Threshold = request.AreaKm2Threshold;
    }

    private static void ApplySeasonalFields(AlertRule rule, UpdateAlertRuleRequest request)
    {
        if (request.SeasonStartMonth.HasValue) rule.SeasonStartMonth = request.SeasonStartMonth;
        if (request.SeasonEndMonth.HasValue) rule.SeasonEndMonth = request.SeasonEndMonth;
    }

    private static void ApplyNotificationFields(AlertRule rule, UpdateAlertRuleRequest request)
    {
        if (request.ConsecutiveCount.HasValue) rule.ConsecutiveCount = request.ConsecutiveCount;
        if (request.EscalationMinutes.HasValue) rule.EscalationMinutes = request.EscalationMinutes;
        if (request.NotifyRoles is not null) rule.NotifyRoles = request.NotifyRoles;
    }

    private async Task<AlertRuleResponse> DeleteRuleAsync(Guid ruleId, CancellationToken ct)
    {
        var rule = await db.AlertRules.FindAsync([ruleId], ct)
            ?? throw new InvalidOperationException($"Rule {ruleId} not found");

        db.AlertRules.Remove(rule);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deleted alert rule {RuleName} with ID {RuleId}", rule.Name, ruleId);

        return ToResponse(rule);
    }

    private async Task<AlertRuleResponse> ToggleActiveAsync(Guid ruleId, CancellationToken ct)
    {
        var rule = await db.AlertRules.FindAsync([ruleId], ct)
            ?? throw new InvalidOperationException($"Rule {ruleId} not found");

        rule.IsActive = !rule.IsActive;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Toggled alert rule {RuleName} active status to {IsActive}", rule.Name, rule.IsActive);

        return ToResponse(rule);
    }

    private static void ValidateRule(AlertRule rule)
    {
        if (rule.MinFwi.HasValue && rule.MaxFwi.HasValue && rule.MinFwi > rule.MaxFwi)
            throw new InvalidOperationException("MinFwi cannot be greater than MaxFwi");

        if (rule.SeasonStartMonth.HasValue && (rule.SeasonStartMonth < 1 || rule.SeasonStartMonth > 12))
            throw new InvalidOperationException("SeasonStartMonth must be between 1 and 12");

        if (rule.SeasonEndMonth.HasValue && (rule.SeasonEndMonth < 1 || rule.SeasonEndMonth > 12))
            throw new InvalidOperationException("SeasonEndMonth must be between 1 and 12");

        if (rule.ConsecutiveCount.HasValue && rule.ConsecutiveCount < 1)
            throw new InvalidOperationException("ConsecutiveCount must be at least 1");

        if (rule.EscalationMinutes.HasValue && rule.EscalationMinutes < 0)
            throw new InvalidOperationException("EscalationMinutes cannot be negative");
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

    private static EventType? TryParseEventType(string? value) =>
        Enum.TryParse<EventType>(value, ignoreCase: true, out var result) ? result : null;

    private static RiskLevel? TryParseRiskLevel(string? value) =>
        Enum.TryParse<RiskLevel>(value, ignoreCase: true, out var result) ? result : null;

    private static Polygon? ParseArea(string? wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt)) return null;
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
    private static readonly JsonSerializerOptions CachedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static RouteGroupBuilder MapConfigureAlertRules(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            HttpContext http,
            ICommandHandler<ConfigureAlertRulesCommand, AlertRuleResponse> handler) =>
        {
            ConfigureAlertRulesRequest? request;
            try
            {
                request = await JsonSerializer.DeserializeAsync<ConfigureAlertRulesRequest>(http.Request.Body, CachedJsonOptions);
            }
            catch (JsonException ex)
            {
                return Results.Text($"{{\"error\":\"Deserialization failed: {ex.Message}\"}}", contentType: "application/json", statusCode: 400);
            }
            if (request is null)
                return Results.Text("{\"error\":\"Request body is required\"}", contentType: "application/json", statusCode: 400);

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
