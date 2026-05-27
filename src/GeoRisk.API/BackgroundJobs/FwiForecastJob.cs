using GeoRisk.API.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.BackgroundJobs;

/// <summary>
/// Background job that runs every 6 hours to calculate FWI forecasts for 1-7 days ahead.
/// Uses Open-Meteo forecast data with carry-over effect for FWI components.
/// </summary>
public sealed class FwiForecastJob(
    IServiceScopeFactory scopeFactory,
    ILogger<FwiForecastJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to let app start
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunForecastCycleAsync(stoppingToken);
        }
    }

    private async Task RunForecastCycleAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting FWI forecast job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<FwiForecastService>();
            await service.CalculateForecastsAsync(ct);
            logger.LogInformation("FWI forecast job completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FWI forecast job failed");
        }
    }
}