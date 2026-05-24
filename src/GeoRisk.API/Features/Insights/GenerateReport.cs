using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Insights;

public sealed record GenerateReportCommand(DateTime From, DateTime To) : IQuery<string>;

public sealed class GenerateReportHandler(
    GeoRiskDbContext db,
    ILlmProvider llm) : IQueryHandler<GenerateReportCommand, string>
{
    public async Task<string> HandleAsync(GenerateReportCommand query, CancellationToken ct)
    {
        var events = await db.GeoEvents
            .Where(e => e.OccurredAt >= query.From && e.OccurredAt <= query.To)
            .AsNoTracking()
            .ToListAsync(ct);

        if (events.Count == 0)
            return "# No Events Found\n\nNo events were recorded in the specified date range.";

        var systemPrompt = "You are a municipal risk analyst generating executive reports.";
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