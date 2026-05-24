using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Insights;

public sealed record DetectPatternsCommand : IQuery<DetectPatternsResult>;

public sealed record DetectPatternsResult(List<DetectedPattern> Patterns);

public sealed record DetectedPattern(
    string PatternType,
    string Title,
    string Description,
    double Confidence,
    List<Guid> AffectedEventIds,
    string Recommendation);

public sealed class DetectPatternsHandler(
    GeoRiskDbContext db,
    ILlmProvider llm) : IQueryHandler<DetectPatternsCommand, DetectPatternsResult>
{
    public async Task<DetectPatternsResult> HandleAsync(DetectPatternsCommand cmd, CancellationToken ct)
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var events = await db.GeoEvents
            .Where(e => e.OccurredAt >= from)
            .AsNoTracking()
            .ToListAsync(ct);

        if (events.Count == 0)
            return new DetectPatternsResult([]);

        var systemPrompt = "You are a pattern recognition specialist for geographic risk analysis.";
        var userPrompt = LlmPromptBuilder.DetectPatterns(events);

        var patterns = await llm.CompleteStructuredAsync<List<PatternJson>>(systemPrompt, userPrompt, ct);

        return new DetectPatternsResult(
            patterns.Select(p => new DetectedPattern(
                p.PatternType,
                p.Title,
                p.Description,
                p.Confidence,
                p.AffectedEventIds,
                p.Recommendation)).ToList());
    }

    private sealed record PatternJson(
        string PatternType,
        string Title,
        string Description,
        double Confidence,
        List<Guid> AffectedEventIds,
        string Recommendation);
}

public static class DetectPatternsEndpoint
{
    public static RouteGroupBuilder MapDetectPatterns(this RouteGroupBuilder group)
    {
        group.MapGet("/patterns", async (
            IQueryHandler<DetectPatternsCommand, DetectPatternsResult> handler) =>
        {
            var result = await handler.HandleAsync(new DetectPatternsCommand(), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin").WithTags("Insights");
        return group;
    }
}