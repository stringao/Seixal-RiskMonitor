using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.BackgroundJobs;

public sealed class AIClassificationJob(
    GeoRiskDbContext db, ILogger<AIClassificationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(ct))
        {
            await ClassifyEventsAsync(ct);
        }
    }

    private async Task ClassifyEventsAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting AI classification job");

        try
        {
            var unclassified = await db.GeoEvents
                .Where(e => e.AIClassification == null)
                .Take(50)
                .ToListAsync(ct);

            if (unclassified.Count == 0)
            {
                logger.LogInformation("No events to classify");
                return;
            }

            foreach (var geoEvent in unclassified)
            {
                var classification = ClassifyEvent(geoEvent);
                geoEvent.AIClassification = classification.Label;
                geoEvent.AIInsight = classification.Insight;
                geoEvent.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Classified {Count} events", unclassified.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI classification job failed");
        }
    }

    private static (string Label, string Insight) ClassifyEvent(GeoEvent geoEvent)
    {
        return geoEvent.EventType switch
        {
            EventType.Fire => (
                "Wildfire - Urban Interface Risk",
                "Fire detected in proximity to urban areas. Recommend immediate verification with local authorities. Wind conditions and humidity are critical factors."),
            EventType.Flood => (
                "Flash Flood - Lowlying Area",
                "Water accumulation in low-lying areas. Monitor drainage systems and alert local emergency services if water level rises."),
            EventType.Storm => (
                "Severe Weather - Storm System",
                "Active storm system detected. High winds and heavy rainfall expected. Stay clear of exposed areas."),
            EventType.Landslide => (
                "Landslide - Terrain Instability",
                "Terrain instability detected. Avoid steep slopes and monitor for further movement. Notify civil protection."),
            EventType.Heatwave => (
                "Extreme Heat Event",
                "Prolonged high temperatures detected. Increase hydration alerts and monitor vulnerable populations."),
            _ => (
                "General Event",
                "Event requires manual analysis. Further investigation needed to determine risk profile.")
        };
    }
}