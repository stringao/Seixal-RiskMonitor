using GeoRisk.API.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeoRisk.API.BackgroundJobs;

/// <summary>
/// Background job that runs every 5 minutes to:
/// - Update ETAs based on current conditions
/// - Check for overdue arrivals
/// - Update resource availability based on expected return times
/// </summary>
public sealed class ResourceStatusUpdateJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ResourceStatusUpdateJob> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit on startup to let the system settle
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunUpdateAsync(stoppingToken);
            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task RunUpdateAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting resource status update at {Time}", DateTime.UtcNow);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.GeoRiskDbContext>();
            var routingService = scope.ServiceProvider.GetRequiredService<ResourceRoutingService>();

            // 1. Check for overdue arrivals (resources that should have arrived but haven't)
            var overdueDispatches = await db.ResourceDispatches
                .Include(d => d.Resource)
                .Where(d => d.Status == "EnRoute")
                .Where(d => d.DispatchedAt.AddMinutes(d.TravelTimeMinutes!.Value * 1.5) < DateTime.UtcNow)
                .ToListAsync(ct);

            foreach (var dispatch in overdueDispatches)
            {
                logger.LogWarning(
                    "Potential overdue dispatch: {ResourceName} to event {EventId}. Dispatched at {DispatchedAt}",
                    dispatch.Resource?.Name ?? "Unknown",
                    dispatch.EventId,
                    dispatch.DispatchedAt);
            }

            // 2. Check for resources that should have returned
            var dueForReturn = await db.FireResources
                .Where(r => r.Status == "OnScene" && r.ExpectedReturnAt != null)
                .Where(r => r.ExpectedReturnAt < DateTime.UtcNow.AddMinutes(-30)) // 30 min grace period
                .ToListAsync(ct);

            foreach (var resource in dueForReturn)
            {
                logger.LogInformation(
                    "Resource {ResourceName} overdue for return. Expected at {ExpectedReturn}",
                    resource.Name,
                    resource.ExpectedReturnAt);
            }

            // 3. Update availability for resources that have returned
            var shouldBeAvailable = await db.FireResources
                .Where(r => r.Status == "Returning")
                .Where(r => r.ExpectedReturnAt != null && r.ExpectedReturnAt <= DateTime.UtcNow)
                .ToListAsync(ct);

            foreach (var resource in shouldBeAvailable)
            {
                await routingService.UpdateResourceStatusAsync(resource.Id, "Available", ct: ct);
                logger.LogInformation("Resource {ResourceName} now available", resource.Name);
            }

            // 4. Get current status summary for logging
            var status = await routingService.GetResourceStatusAsync(ct);
            logger.LogInformation(
                "Resource status: {Available} available, {EnRoute} en-route, {OnScene} on scene, {Total} total",
                status.Available,
                status.EnRoute,
                status.OnScene,
                status.TotalResources);

            logger.LogInformation("Resource status update completed at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resource status update job failed");
        }
    }
}