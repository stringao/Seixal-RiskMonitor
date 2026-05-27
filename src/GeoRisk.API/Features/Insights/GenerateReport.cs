using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Insights;

public sealed record GenerateReportCommand(DateTime From, DateTime To) : IQuery<string>;

public sealed class GenerateReportHandler(
    GeoRiskDbContext db,
    ILlmProvider llm,
    ILlmSettingsService settingsService) : IQueryHandler<GenerateReportCommand, string>
{
    public async Task<string> HandleAsync(GenerateReportCommand query, CancellationToken ct)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
        {
            return "# AI Not Configured\n\nPlease configure an API key in Settings to enable AI-powered report generation.";
        }

        // Convert to UTC for PostgreSQL query while preserving original values for prompt
        var fromUtc = DateTime.SpecifyKind(query.From, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(query.To.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var events = await db.GeoEvents
            .Where(e => e.OccurredAt >= fromUtc && e.OccurredAt <= toUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        if (events.Count == 0)
            return "# No Events Found\n\nNo events were recorded in the specified date range.";

        var systemPrompt = "You are a municipal risk analyst generating executive reports in Brazilian Portuguese (pt-BR).";
        var userPrompt = LlmPromptBuilder.GenerateReport(events, query.From, query.To);

        return await llm.CompleteAsync(systemPrompt, userPrompt, ct);
    }
}

public static class GenerateReportEndpoint
{
    public static RouteGroupBuilder MapGenerateReport(this RouteGroupBuilder group)
    {
        group.MapGet("/report", async (DateTime from, DateTime to,
            IQueryHandler<GenerateReportCommand, string> handler) =>
        {
            var result = await handler.HandleAsync(new GenerateReportCommand(from, to), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin").WithTags("Insights");
        return group;
    }
}