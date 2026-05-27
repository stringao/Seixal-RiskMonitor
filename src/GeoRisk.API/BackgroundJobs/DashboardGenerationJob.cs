using System.Globalization;
using System.Text.Json;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.BackgroundJobs;

public sealed class DashboardGenerationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<DashboardGenerationJob> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Schedule daily run at 7am
        var now = DateTime.UtcNow;
        var nextDaily = now.Date.AddHours(7);
        if (nextDaily <= now) nextDaily = nextDaily.AddDays(1);

        // Schedule weekly run on Monday at 6am
        var nextMonday = now.Date.AddDays(7 - (int)now.DayOfWeek + (int)DayOfWeek.Monday);
        if (now.DayOfWeek == DayOfWeek.Monday && now.Hour >= 6)
            nextMonday = now.Date;
        var nextWeekly = nextMonday.AddHours(6);

        var nextRun = nextDaily < nextWeekly ? nextDaily : nextWeekly;
        var delay = nextRun - now;

        if (delay > TimeSpan.Zero)
        {
            logger.LogInformation("Dashboard generation job scheduled. Next run: {Time}", nextRun);
            await Task.Delay(delay, stoppingToken);
        }

        // Main loop - check every hour what needs to be generated
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAndGenerateAsync(stoppingToken);
        }
    }

    private async Task CheckAndGenerateAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Daily briefing at 7am
        if (now.Hour == 7 && now.Minute < 5)
        {
            await GenerateDailyBriefingAsync(ct);
        }

        // Weekly report on Monday at 6am
        if (now.DayOfWeek == DayOfWeek.Monday && now.Hour == 6 && now.Minute < 5)
        {
            await GenerateWeeklyReportAsync(ct);
        }
    }

    private async Task GenerateDailyBriefingAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting daily dashboard generation at {Time}", DateTime.UtcNow);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardGeneratorService>();

            // Generate today's risk summary
            var riskSummary = await dashboardService.GenerateRiskSummaryAsync(ct);
            if (riskSummary != null)
            {
                var json = JsonSerializer.Serialize(riskSummary, JsonOptions);
                await dashboardService.SetCachedDashboardAsync(
                    "dashboard_today",
                    json,
                    "deepseek",
                    TimeSpan.FromMinutes(30),
                    ct);
                logger.LogInformation("Daily risk summary cached successfully");
            }

            // Generate situation report
            var situationReport = await dashboardService.GenerateSituationReportAsync(ct);
            if (situationReport != null)
            {
                var json = JsonSerializer.Serialize(situationReport, JsonOptions);
                await dashboardService.SetCachedDashboardAsync(
                    "situation_report",
                    json,
                    "deepseek",
                    TimeSpan.FromMinutes(5),
                    ct);
                logger.LogInformation("Situation report cached successfully");
            }

            // Send to scheduled report recipients
            await SendToRecipientsAsync(scope.ServiceProvider, ReportType.DailySummary, ct);

            logger.LogInformation("Daily dashboard generation completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Daily dashboard generation failed");
        }
    }

    private async Task GenerateWeeklyReportAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting weekly report generation at {Time}", DateTime.UtcNow);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardGeneratorService>();

            var weeklyReport = await dashboardService.GenerateWeeklyReportAsync(ct);
            if (weeklyReport != null)
            {
                var weekKey = $"weekly_report_w{ISOWeek.GetWeekOfYear(DateTime.UtcNow)}";
                var json = JsonSerializer.Serialize(weeklyReport, JsonOptions);
                await dashboardService.SetCachedDashboardAsync(
                    weekKey,
                    json,
                    "deepseek",
                    TimeSpan.FromHours(6),
                    ct);
                logger.LogInformation("Weekly report cached successfully");
            }

            // Send to scheduled report recipients
            await SendToRecipientsAsync(scope.ServiceProvider, ReportType.WeeklyReport, ct);

            logger.LogInformation("Weekly report generation completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weekly report generation failed");
        }
    }

    private async Task SendToRecipientsAsync(IServiceProvider sp, ReportType reportType, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardGeneratorService>();

        var reports = await db.ScheduledReports
            .Where(r => r.Type == reportType && r.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var report in reports)
        {
            var cacheKey = report.Type switch
            {
                ReportType.DailySummary => "dashboard_today",
                ReportType.WeeklyReport => $"weekly_report_w{ISOWeek.GetWeekOfYear(DateTime.UtcNow)}",
                _ => null
            };

            if (cacheKey == null) continue;

            var content = await dashboardService.GetCachedDashboardAsync(cacheKey, ct);
            if (content == null) continue;

            // Log email sending (stub implementation)
            foreach (var recipient in report.Recipients)
            {
                logger.LogInformation(
                    "Would send {ReportType} to {Recipient}. Content length: {Length} chars",
                    report.Type,
                    recipient,
                    content.Length);
            }

            // Update last generated info
            report.LastGeneratedAt = DateTime.UtcNow;
            report.LastContent = content;
            await db.SaveChangesAsync(ct);
        }
    }
}
