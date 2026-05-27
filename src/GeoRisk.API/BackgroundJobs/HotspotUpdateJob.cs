using GeoRisk.API.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeoRisk.API.BackgroundJobs;

/// <summary>
/// Background job that runs daily to analyze fire events and update hotspot data.
/// Aggregates historical data, identifies new hotspots, and triggers alerts for unusual activity.
/// </summary>
public sealed class HotspotUpdateJob(
    IServiceScopeFactory scopeFactory,
    ILogger<HotspotUpdateJob> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run immediately on startup, then on schedule
        await RunHotspotAnalysisAsync(stoppingToken);

        using var timer = new PeriodicTimer(RunInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunHotspotAnalysisAsync(stoppingToken);
        }
    }

    private async Task RunHotspotAnalysisAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting hotspot analysis job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var hotspotService = scope.ServiceProvider.GetRequiredService<HotspotAnalysisService>();

            // Step 1: Identify hotspots from 2 years of historical fire data
            var hotspots = await hotspotService.IdentifyHotspotsAsync(years: 2, ct);

            // Step 2: Sync hotspots to database (update existing, add new)
            await hotspotService.SyncHotspotsAsync(hotspots, ct);

            // Step 3: Check for alerts based on recent activity
            var alerts = await hotspotService.CheckForAlertsAsync(ct);
            if (alerts.Count > 0)
            {
                await hotspotService.CreateAlertsAsync(alerts, ct);
                logger.LogWarning("Created {Count} hotspot alerts: {AlertSummary}", alerts.Count,
                    string.Join("; ", alerts.Select(a => $"{a.AlertType} at {a.FireHotspotId}")));
            }

            logger.LogInformation("Hotspot analysis completed: Identified {HotspotCount} hotspots, Created {AlertCount} alerts", hotspots.Count, alerts.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hotspot analysis job failed");
        }
    }
}