using GeoRisk.API.Infrastructure.Services;

namespace GeoRisk.API.Features.Ai;

/// <summary>
/// AI-powered query endpoints using RAG approach.
/// </summary>
public static class AiEndpoints
{
    public static RouteGroupBuilder MapAiEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/query", HandleQuery)
            .WithTags("AI")
            .WithName("QueryAI")
            .WithDescription("Ask questions in natural language about fire events and risk data");

        group.MapGet("/compare/{eventId}", HandleCompare)
            .WithTags("AI")
            .WithName("CompareEvent")
            .WithDescription("Find similar historical events and compare with the given event");

        group.MapPost("/summary", HandleSummary)
            .WithTags("AI")
            .WithName("GenerateSummary")
            .WithDescription("Generate a dashboard-style summary of current situation");

        return group;
    }

    private static async Task<IResult> HandleQuery(
        AiQueryRequestDto request,
        RagQueryService ragService,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return Results.BadRequest(new { error = "Question is required" });
        }

        var serviceRequest = new AiQueryRequest
        {
            Question = request.Question,
            ContextType = request.ContextType,
            EventId = request.EventId,
            HotspotId = request.HotspotId
        };
        var response = await ragService.QueryAsync(serviceRequest, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> HandleCompare(
        Guid eventId,
        int topN,
        RagQueryService ragService,
        CancellationToken ct)
    {
        if (eventId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Valid event ID is required" });
        }

        var response = await ragService.CompareEventAsync(eventId, topN > 0 ? topN : 5, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> HandleSummary(
        RagQueryService ragService,
        CancellationToken ct)
    {
        var summary = await ragService.GenerateSummaryAsync(ct);
        return Results.Ok(summary);
    }
}

// Request/Response DTOs
public class AiQueryRequestDto
{
    public string Question { get; set; } = string.Empty;
    public string? ContextType { get; set; }
    public Guid? EventId { get; set; }
    public Guid? HotspotId { get; set; }
}

public class AiQueryResponse
{
    public string Answer { get; set; } = string.Empty;
    public string[] Sources { get; set; } = Array.Empty<string>();
    public double Confidence { get; set; }
    public string Language { get; set; } = "pt";
}

public class CompareEventsResponse
{
    public Guid TargetEventId { get; set; }
    public SimilarEvent[] SimilarEvents { get; set; } = Array.Empty<SimilarEvent>();
    public string Analysis { get; set; } = string.Empty;
}

public class SimilarEvent
{
    public Guid EventId { get; set; }
    public double Score { get; set; }
    public string? Title { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Severity { get; set; } = string.Empty;
}

public class SituationSummary
{
    public int TotalEvents { get; set; }
    public int FireEvents { get; set; }
    public int ActiveHotspots { get; set; }
    public string CurrentRiskLevel { get; set; } = string.Empty;
    public string WeatherTrend { get; set; } = string.Empty;
    public double FWI { get; set; }
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public string SummaryText { get; set; } = string.Empty;
}