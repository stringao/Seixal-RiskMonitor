using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.BackgroundJobs;

/// <summary>
/// Background job that periodically detects event chains and correlations
/// between fire events. Runs every 30 minutes to identify new chains
/// as events come in and update existing chain analyses.
/// </summary>
public sealed class EventChainAnalysisJob(
    IServiceScopeFactory scopeFactory,
    ILogger<EventChainAnalysisJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await AnalyzeEventChainsAsync(stoppingToken);
        }
    }

    private async Task AnalyzeEventChainsAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting event chain analysis job at {Time}", DateTime.UtcNow);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<Infrastructure.Services.EventChainAnalysisService>();

            // Detect all types of chains
            var simultaneousChains = await service.DetectSimultaneousFiresAsync(ct);
            var sequentialChains = await service.DetectSequentialCorrelationAsync(ct);
            var resourceChains = await service.DetectResourceContentionAsync(ct);
            var emberChains = await service.DetectEmberCastEventsAsync(ct);

            // Combine all detected chains
            var allChains = new List<Domain.Entities.EventChainAnalysis>();
            allChains.AddRange(simultaneousChains);
            allChains.AddRange(sequentialChains);
            allChains.AddRange(resourceChains);
            allChains.AddRange(emberChains);

            logger.LogInformation("Detected {Total} chain patterns ({Simultaneous} simultaneous, {Sequential} sequential, {Resource} resource, {Ember} ember)",
                allChains.Count, simultaneousChains.Count, sequentialChains.Count, resourceChains.Count, emberChains.Count);

            if (allChains.Count > 0)
            {
                await service.SaveChainAnalysesAsync(allChains, ct);
                logger.LogInformation("Saved {Count} new chain analyses", allChains.Count);
            }

            // Log high-confidence chains as warnings for immediate attention
            var highConfidenceChains = allChains.Where(c => c.ConfidenceScore >= 0.7).ToList();
            foreach (var chain in highConfidenceChains)
            {
                logger.LogWarning(
                    "HIGH CONFIDENCE CHAIN: {Type} (confidence: {Confidence:P0}) - {Description}",
                    chain.AnalysisType,
                    chain.ConfidenceScore,
                    chain.Description);
            }

            logger.LogInformation("Event chain analysis job completed. Total chains detected: {Count}", allChains.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Event chain analysis job failed");
        }
    }
}
