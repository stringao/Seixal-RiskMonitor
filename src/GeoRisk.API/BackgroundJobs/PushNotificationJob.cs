using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.BackgroundJobs;

public sealed class PushNotificationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<PushNotificationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessNotificationsAsync(stoppingToken);
        }
    }

    private async Task ProcessNotificationsAsync(CancellationToken ct)
    {
        logger.LogDebug("Starting push notification job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var alertNotificationService = scope.ServiceProvider.GetRequiredService<AlertNotificationService>();
            var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

            // Process pending notifications
            await alertNotificationService.ProcessPendingNotificationsAsync(50, ct);

            // Cleanup expired push subscriptions
            var cleanedCount = await pushService.CleanupExpiredSubscriptionsAsync(ct);
            if (cleanedCount > 0)
            {
                logger.LogInformation("Cleaned up {Count} expired push subscriptions", cleanedCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Push notification job failed");
        }
    }
}