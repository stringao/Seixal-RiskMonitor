using System.Text.Json;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Insights;

public sealed record ClassifyIncidentCommand(Guid EventId) : ICommand<ClassifyIncidentResult>;

public sealed record ClassifyIncidentResult(
    Guid EventId,
    string Classification,
    double Confidence,
    string Reasoning,
    List<string> RelatedRisks);

public sealed class ClassifyIncidentHandler(
    GeoRiskDbContext db,
    ILlmProvider llm,
    ILlmSettingsService settingsService) : ICommandHandler<ClassifyIncidentCommand, ClassifyIncidentResult>
{
    public async Task<ClassifyIncidentResult> HandleAsync(ClassifyIncidentCommand cmd, CancellationToken ct)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("AI features are not configured. Please configure the API key in Settings.");
        }

        var geoEvent = await db.GeoEvents.FirstOrDefaultAsync(e => e.Id == cmd.EventId, ct)
            ?? throw new InvalidOperationException($"Event {cmd.EventId} not found");

        var systemPrompt = "You are a risk analysis expert for municipal geographic monitoring systems.";
        var userPrompt = LlmPromptBuilder.ClassifyIncident(geoEvent);

        var result = await llm.CompleteStructuredAsync<ClassifyIncidentJson>(systemPrompt, userPrompt, ct);

        geoEvent.AIClassification = result.Classification;
        geoEvent.AIInsight = result.Reasoning;
        await db.SaveChangesAsync(ct);

        return new ClassifyIncidentResult(
            geoEvent.Id,
            result.Classification,
            result.Confidence,
            result.Reasoning,
            result.RelatedRisks);
    }

    private sealed record ClassifyIncidentJson(
        string Classification,
        double Confidence,
        string Reasoning,
        List<string> RelatedRisks);
}

public static class ClassifyIncidentEndpoint
{
    public static RouteGroupBuilder MapClassifyIncident(this RouteGroupBuilder group)
    {
        group.MapPost("/classify/{eventId:guid}", async (Guid eventId,
            ICommandHandler<ClassifyIncidentCommand, ClassifyIncidentResult> handler) =>
        {
            var result = await handler.HandleAsync(new ClassifyIncidentCommand(eventId), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin").WithTags("Insights");
        return group;
    }
}