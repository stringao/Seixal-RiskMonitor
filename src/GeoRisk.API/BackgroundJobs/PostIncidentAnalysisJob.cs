using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.BackgroundJobs;

/// <summary>
/// Background job that runs every 30 minutes to:
/// 1. Find GeoEvents that have ended (no updates for 4+ hours)
/// 2. Generate FireReport automatically using PostIncidentAnalysisService
/// 3. Update SeasonalStatistics
/// </summary>
public sealed class PostIncidentAnalysisJob(
    IServiceScopeFactory scopeFactory,
    ILogger<PostIncidentAnalysisJob> logger) : BackgroundService
{
    // Fire is considered "ended" if no updates for 4 hours
    private static readonly TimeSpan FireEndThreshold = TimeSpan.FromHours(4);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to let app start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAnalysisCycleAsync(stoppingToken);
        }
    }

    private async Task RunAnalysisCycleAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting post-incident analysis job");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();
            var analysisService = scope.ServiceProvider.GetRequiredService<PostIncidentAnalysisService>();

            // Step 1: Find fire events that may have ended
            var endedFires = await FindEndedFiresAsync(db, ct);

            // Step 2: Generate reports for fires without reports or with failed reports
            var reportCount = 0;
#pragma warning disable S3267 // Loop contains try/catch + async — cannot be simplified with LINQ
            foreach (var fire in endedFires)
#pragma warning restore S3267
            {
                try
                {
                    var existingReport = await db.FireReports
                        .Where(r => r.GeoEventId == fire.Id && r.Status == ReportStatus.Completed)
                        .FirstOrDefaultAsync(ct);

                    if (existingReport != null)
                    {
                        continue;
                    }

                    await analysisService.GenerateFireReportAsync(fire.Id, ct);
                    reportCount++;

                    // Small delay to avoid overwhelming the system
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to generate report for fire {FireId}", fire.Id);
                }
            }

            // Step 3: Update seasonal statistics
            await UpdateSeasonalStatisticsAsync(analysisService, ct);

            logger.LogInformation("Post-incident analysis completed: Found {EndedCount} ended fires, Generated {ReportCount} new reports", endedFires.Count, reportCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Post-incident analysis job failed");
        }
    }

    private static async Task<List<GeoEvent>> FindEndedFiresAsync(GeoRiskDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.Subtract(FireEndThreshold);

        // Find fire events that:
        // 1. Have no updates in the last 4 hours
        // 2. Are marked as Critical or High severity (more likely to be real fires)
        // 3. Don't already have a completed report
        var endedFires = await db.GeoEvents
            .Where(e => e.EventType == EventType.Fire)
            .Where(e => e.UpdatedAt < cutoff)
            .Where(e => e.Severity >= RiskLevel.High)
            .Where(e => !db.FireReports.Any(r => r.GeoEventId == e.Id && r.Status == ReportStatus.Completed))
            .AsNoTracking()
            .ToListAsync(ct);

        return endedFires;
    }

    private async Task UpdateSeasonalStatisticsAsync(
        PostIncidentAnalysisService analysisService,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var currentYear = now.Year;
        var currentMonth = now.Month;

        try
        {
            // Update current month statistics
            logger.LogDebug("Updating seasonal statistics for {Year}/{Month}", currentYear, currentMonth);
            await analysisService.CalculateSeasonalStatisticsAsync(currentYear, currentMonth, PortugalRegion, ct);
            await analysisService.CalculateSeasonalStatisticsAsync(currentYear, currentMonth, SetubalRegion, ct);

            // Update annual statistics for current year
            await analysisService.CalculateSeasonalStatisticsAsync(currentYear, 0, PortugalRegion, ct);
            await analysisService.CalculateSeasonalStatisticsAsync(currentYear, 0, SetubalRegion, ct);

            // Update previous month if we're in the first week of the month
            if (now.Day <= 7 && currentMonth > 1)
            {
                var prevMonth = currentMonth - 1;
                await analysisService.CalculateSeasonalStatisticsAsync(currentYear, prevMonth, PortugalRegion, ct);
                await analysisService.CalculateSeasonalStatisticsAsync(currentYear, prevMonth, SetubalRegion, ct);
            }
            else if (now.Day <= 7 && currentMonth == 1)
            {
                // Previous year December
                await analysisService.CalculateSeasonalStatisticsAsync(currentYear - 1, 12, PortugalRegion, ct);
                await analysisService.CalculateSeasonalStatisticsAsync(currentYear - 1, 12, SetubalRegion, ct);
            }

            logger.LogInformation("Seasonal statistics updated successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update seasonal statistics");
        }
    }

    private const string PortugalRegion = "Portugal";
    private const string SetubalRegion = "Setubal";
}
