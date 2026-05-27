using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Insights;

/// <summary>
/// API endpoints for event chain analysis.
/// Provides detection and querying of correlated fire event chains.
/// </summary>
public static class ChainAnalysisEndpoints
{
    private const string InsightsTag = "Insights";

    public static RouteGroupBuilder MapChainAnalysis(this RouteGroupBuilder group)
    {
        group.MapGet("/chains", GetChainAnalyses)
            .WithTags(InsightsTag)
            .WithName("GetChainAnalyses")
            .WithSummary("Get all detected event chains with optional filters")
            .RequireAuthorization("AnalystOrAdmin");

        group.MapGet("/chains/current", GetActiveChains)
            .WithTags(InsightsTag)
            .WithName("GetActiveChains")
            .WithSummary("Get currently active chains (ongoing multi-fire situations)")
            .RequireAuthorization("AnalystOrAdmin");

        group.MapPost("/chains/analyze", AnalyzeChains)
            .WithTags(InsightsTag)
            .WithName("AnalyzeChains")
            .WithSummary("Trigger on-demand chain analysis for specific events")
            .RequireAuthorization("AnalystOrAdmin");

        group.MapGet("/chains/types", GetChainTypes)
            .WithTags(InsightsTag)
            .WithName("GetChainTypes")
            .WithSummary("Get available chain analysis types")
            .RequireAuthorization();

        return group;
    }

    /// <summary>
    /// Get all detected event chains with optional filtering.
    /// </summary>
    private static async Task<IResult> GetChainAnalyses(
        string? type,
        DateTime? from,
        DateTime? to,
        double? minConfidence,
        EventChainAnalysisService service,
        CancellationToken ct)
    {
        var chains = await service.GetChainAnalysesAsync(type, from, to, minConfidence, ct);

        var response = chains.Select(c => new ChainAnalysisResponse(
            Id: c.Id,
            AnalysisType: c.AnalysisType,
            PrimaryEventId: c.PrimaryEventId,
            Description: c.Description ?? string.Empty,
            ConfidenceScore: c.ConfidenceScore,
            Findings: ParseJson(c.Findings),
            ContributingFactors: c.ContributingFactors,
            RecommendedAction: c.RecommendedAction,
            AnalyzedAt: c.AnalyzedAt
        )).ToList();

        return Results.Ok(new ChainAnalysesResult(response));
    }

    /// <summary>
    /// Get currently active chains - chains involving events from the last 24 hours.
    /// </summary>
    private static async Task<IResult> GetActiveChains(
        EventChainAnalysisService service,
        CancellationToken ct)
    {
        var chains = await service.GetActiveChainsAsync(ct);

        var response = chains.Select(c => new ChainAnalysisResponse(
            Id: c.Id,
            AnalysisType: c.AnalysisType,
            PrimaryEventId: c.PrimaryEventId,
            Description: c.Description ?? string.Empty,
            ConfidenceScore: c.ConfidenceScore,
            Findings: ParseJson(c.Findings),
            ContributingFactors: c.ContributingFactors,
            RecommendedAction: c.RecommendedAction,
            AnalyzedAt: c.AnalyzedAt
        )).ToList();

        return Results.Ok(new ChainAnalysesResult(response));
    }

    /// <summary>
    /// Trigger on-demand chain analysis for specific events.
    /// </summary>
    private static async Task<IResult> AnalyzeChains(
        AnalyzeChainsRequest request,
        EventChainAnalysisService service,
        CancellationToken ct)
    {
        if (request.EventIds == null || request.EventIds.Count < 2)
        {
            return Results.BadRequest(new { error = "At least 2 event IDs are required for chain analysis" });
        }

        var chain = await service.GenerateChainReportAsync(request.EventIds, ct);

        var response = new ChainAnalysisResponse(
            Id: chain.Id,
            AnalysisType: chain.AnalysisType,
            PrimaryEventId: chain.PrimaryEventId,
            Description: chain.Description ?? string.Empty,
            ConfidenceScore: chain.ConfidenceScore,
            Findings: ParseJson(chain.Findings),
            ContributingFactors: chain.ContributingFactors,
            RecommendedAction: chain.RecommendedAction,
            AnalyzedAt: chain.AnalyzedAt
        );

        return Results.Ok(response);
    }

    /// <summary>
    /// Get available chain analysis types.
    /// </summary>
    private static IResult GetChainTypes()
    {
        var types = new[]
        {
            new ChainTypeInfo("Simultaneous", "Fires happening at the same time under similar conditions"),
            new ChainTypeInfo("Sequential", "Fires where one ends and another starts nearby"),
            new ChainTypeInfo("ResourceContention", "Multiple fires competing for the same firefighting units"),
            new ChainTypeInfo("EmberCast", "Secondary ignitions caused by ember cast from a large fire")
        };

        return Results.Ok(types);
    }

    private static object? ParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(json);
        }
        catch
        {
            return null;
        }
    }
}

// DTOs

public record ChainAnalysesResult(List<ChainAnalysisResponse> Chains);

public record ChainAnalysisResponse(
    Guid Id,
    string AnalysisType,
    Guid? PrimaryEventId,
    string Description,
    double ConfidenceScore,
    object? Findings,
    List<string> ContributingFactors,
    string? RecommendedAction,
    DateTime AnalyzedAt);

public record ChainTypeInfo(string Type, string Description);

public record AnalyzeChainsRequest(List<Guid> EventIds);
