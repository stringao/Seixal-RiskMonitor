using GeoRisk.API.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeoRisk.API.BackgroundJobs;

public sealed class SatelliteFireUpdateJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SatelliteFireUpdateJob> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SatelliteFireUpdateJob starting");

        // Run immediately on startup, then every 6 hours
        await RunUpdateAsync(stoppingToken);

        using var timer = new PeriodicTimer(RunInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunUpdateAsync(stoppingToken);
        }
    }

    private async Task RunUpdateAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting satellite fire data update at {Time}", DateTime.UtcNow);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var satelliteService = scope.ServiceProvider.GetRequiredService<SatelliteFireService>();

            await satelliteService.FetchAndStoreAsync(ct);

            logger.LogInformation("Satellite fire data update completed at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Satellite fire data update failed");
        }
    }
}
