using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.BackgroundJobs;

public sealed class ReportGenerationJob(
    IServiceScopeFactory scopeFactory, ILogger<ReportGenerationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;
        var nextMonday = now.Date.AddDays(7 - (int)now.DayOfWeek + (int)DayOfWeek.Monday);
        if (now.DayOfWeek == DayOfWeek.Monday && now.Hour >= 6)
            nextMonday = now.Date;
        var nextRun = nextMonday.AddHours(6);

        var delay = nextRun - now;
        if (delay > TimeSpan.Zero)
        {
            logger.LogInformation("Report generation job scheduled for {Time}", nextRun);
            await Task.Delay(delay, stoppingToken);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromDays(7));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await GenerateReportAsync(stoppingToken);
        }
    }

    private async Task GenerateReportAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting weekly report generation at {Time}", DateTime.UtcNow);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();

            var weekStart = DateTime.UtcNow.AddDays(-7);

            var eventsThisWeek = await db.GeoEvents
                .Where(e => e.CreatedAt >= weekStart)
                .AsNoTracking()
                .ToListAsync(ct);

            var totalEvents = eventsThisWeek.Count;
            var byType = eventsThisWeek.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count());
            var bySeverity = eventsThisWeek.GroupBy(e => e.Severity).ToDictionary(g => g.Key, g => g.Count());

            logger.LogInformation(
                "WEEKLY REPORT: Total={Total} Fire={Fire} Flood={Flood} Storm={Storm} " +
                "Critical={Critical} High={High} Medium={Medium} Low={Low}",
                totalEvents,
                byType.GetValueOrDefault(EventType.Fire),
                byType.GetValueOrDefault(EventType.Flood),
                byType.GetValueOrDefault(EventType.Storm),
                bySeverity.GetValueOrDefault(RiskLevel.Critical),
                bySeverity.GetValueOrDefault(RiskLevel.High),
                bySeverity.GetValueOrDefault(RiskLevel.Medium),
                bySeverity.GetValueOrDefault(RiskLevel.Low));

            logger.LogInformation("Report generation completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report generation job failed");
        }
    }
}