using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Alerts;

public sealed record TestAlertRuleQuery(
    Guid RuleId,
    int HoursBack = 24) : IQuery<TestRuleResponse>;

public sealed class TestAlertRuleHandler(
    GeoRiskDbContext db,
    AlertRuleEvaluationEngine evaluationEngine) : IQueryHandler<TestAlertRuleQuery, TestRuleResponse>
{
    public async Task<TestRuleResponse> HandleAsync(TestAlertRuleQuery query, CancellationToken ct)
    {
        var rule = await db.AlertRules.FindAsync([query.RuleId], ct);
        if (rule == null)
        {
            throw new InvalidOperationException($"Rule {query.RuleId} not found");
        }

        var recentEvents = await db.GeoEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-query.HoursBack))
            .AsNoTracking()
            .ToListAsync(ct);

        var weatherData = await db.WeatherRiskDataPoints
            .Where(w => w.Timestamp >= DateTime.UtcNow.AddHours(-query.HoursBack))
            .AsNoTracking()
            .ToListAsync(ct);

        var evaluatedAlerts = await evaluationEngine.EvaluateRuleAsync(rule, recentEvents, weatherData, ct);

        var matches = evaluatedAlerts
            .Where(e => e.GeoEvent != null)
            .Select(e => new TestRuleMatch(
                e.GeoEvent!.Id,
                e.GeoEvent.Title,
                e.GeoEvent.EventType.ToString(),
                e.GeoEvent.OccurredAt,
                new GeoLocation(e.GeoEvent.Geometry.Y, e.GeoEvent.Geometry.X),
                e.FwiValue,
                e.WindSpeed,
                e.Temperature,
                e.AreaKm2))
            .ToList();

        return new TestRuleResponse(
            query.RuleId,
            matches.Count > 0,
            matches.Count,
            matches);
    }
}

public static class TestAlertRuleEndpoint
{
    public static RouteGroupBuilder MapTestAlertRule(this RouteGroupBuilder group)
    {
        group.MapPut("/rules/{id:guid}/test", async (
            Guid id,
            int? hoursBack,
            IQueryHandler<TestAlertRuleQuery, TestRuleResponse> handler) =>
        {
            var hours = hoursBack ?? 24;
            var result = await handler.HandleAsync(new TestAlertRuleQuery(id, hours), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin");

        return group;
    }
}
