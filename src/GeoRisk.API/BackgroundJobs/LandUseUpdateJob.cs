using GeoRisk.API.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeoRisk.API.BackgroundJobs;

/// <summary>
/// Background job that runs weekly to update land use data from DGT COS.
/// Also marks Wildland-Urban Interface (WUI) zones.
/// </summary>
public sealed class LandUseUpdateJob(
    IServiceScopeFactory scopeFactory,
    ILogger<LandUseUpdateJob> logger) : BackgroundService
{
    // Run weekly on Sunday at 2 AM
    private static readonly TimeSpan RunInterval = TimeSpan.FromDays(7);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to let app start
        await Task.Delay(InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(RunInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunUpdateAsync(stoppingToken);
        }
    }

    private async Task RunUpdateAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting land use data update job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var landUseService = scope.ServiceProvider.GetRequiredService<LandUseService>();

            var pointsUpdated = await landUseService.UpdateLandUseDataAsync(ct);

            logger.LogInformation("Land use update job completed. Updated {Count} points", pointsUpdated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Land use update job failed");
        }
    }
}