using System.Text.Json;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Reports;

/// <summary>
/// API endpoints for fire reports (post-incident analysis).
/// </summary>
public static class FireReportsEndpoints
{
    public static RouteGroupBuilder MapFireReports(this RouteGroupBuilder group)
    {
        group.MapGet("/fires/{eventId:guid}", GetFireReport)
            .WithTags("Reports")
            .WithName("GetFireReport")
            .WithSummary("Get fire report for an event")
            .RequireAuthorization("AnalystOrAdmin");

        group.MapGet("/fires/{eventId:guid}/timeline", GetFireTimeline)
            .WithTags("Reports")
            .WithName("GetFireTimeline")
            .WithSummary("Get just the timeline of fire evolution")
            .RequireAuthorization("AnalystOrAdmin");

        return group;
    }

    /// <summary>
    /// Get fire report for an event, including timeline, cause analysis, and area estimate.
    /// </summary>
    private static async Task<IResult> GetFireReport(
        Guid eventId,
        PostIncidentAnalysisService analysisService,
        GeoRiskDbContext db,
        CancellationToken ct)
    {
        var report = await db.FireReports
            .Where(r => r.GeoEventId == eventId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (report == null)
        {
            // Generate report on-demand if not exists
            try
            {
                report = await analysisService.GenerateFireReportAsync(eventId, ct);
            }
            catch (ArgumentException)
            {
                return Results.NotFound(new { error = "Fire event not found" });
            }
        }

        var response = MapToResponse(report);
        return Results.Ok(response);
    }

    /// <summary>
    /// Get just the timeline of fire evolution for an event.
    /// </summary>
    private static async Task<IResult> GetFireTimeline(
        Guid eventId,
        PostIncidentAnalysisService analysisService,
        GeoRiskDbContext db,
        CancellationToken ct)
    {
        var geoEvent = await db.GeoEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (geoEvent == null)
        {
            return Results.NotFound(new { error = "Fire event not found" });
        }

        var timeline = await analysisService.BuildTimelineAsync(geoEvent, ct);

        var response = new FireTimelineResponse(
            EventId: eventId,
            EventTitle: geoEvent.Title,
            OccurredAt: geoEvent.OccurredAt,
            Timeline: timeline.Select(t => new TimelineEntryResponse(
                t.Time,
                t.Event,
                t.Description,
                t.AreaHa,
                t.Fwi
            )).ToList()
        );

        return Results.Ok(response);
    }

    private static FireReportResponse MapToResponse(FireReport report)
    {
        FwiCondition? fwiConditions = null;
        WeatherCondition? weatherConditions = null;
        List<TimelineEntry>? timeline = null;

        if (!string.IsNullOrEmpty(report.FwiConditionsJson))
        {
            fwiConditions = JsonSerializer.Deserialize<FwiCondition>(report.FwiConditionsJson);
        }

        if (!string.IsNullOrEmpty(report.WeatherConditionsJson))
        {
            weatherConditions = JsonSerializer.Deserialize<WeatherCondition>(report.WeatherConditionsJson);
        }

        if (!string.IsNullOrEmpty(report.TimelineJson))
        {
            timeline = JsonSerializer.Deserialize<List<TimelineEntry>>(report.TimelineJson);
        }

        return new FireReportResponse(
            Id: report.Id,
            GeoEventId: report.GeoEventId,
            Status: report.Status.ToString(),
            StartedAt: report.StartedAt,
            CompletedAt: report.CompletedAt,
            GeneratedBy: report.GeneratedBy,
            Summary: report.Summary,
            ProbableCause: report.ProbableCause,
            CauseConfidence: report.CauseConfidence,
            PeakFireTime: report.PeakFireTime,
            PeakFireAreaHa: report.PeakFireAreaHa,
            TotalAreaHa: report.TotalAreaHa,
            AffectedAreas: report.AffectedAreas,
            EvacuationCount: report.EvacuationCount,
            StructuresDestroyed: report.StructuresDestroyed,
            FirefightersDeployed: report.FirefightersDeployed,
            DurationHours: report.DurationHours,
            FwiConditions: fwiConditions != null ? new FwiConditionResponse(
                fwiConditions.StartFwi,
                fwiConditions.PeakFwi,
                fwiConditions.AverageFwi,
                fwiConditions.WindDirection
            ) : null,
            WeatherConditions: weatherConditions != null ? new WeatherConditionResponse(
                weatherConditions.AvgTemp,
                weatherConditions.MaxTemp,
                weatherConditions.MinHumidity,
                weatherConditions.TotalPrecipitation,
                weatherConditions.DominantWindDir
            ) : null,
            Timeline: timeline?.Select(t => new TimelineEntryResponse(
                t.Time,
                t.Event,
                t.Description,
                t.AreaHa,
                t.Fwi
            )).ToList(),
            GeneratedContent: report.GeneratedContent,
            CreatedAt: report.CreatedAt
        );
    }
}

public record FireReportResponse(
    Guid Id,
    Guid GeoEventId,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string GeneratedBy,
    string? Summary,
    string? ProbableCause,
    double? CauseConfidence,
    DateTime? PeakFireTime,
    double? PeakFireAreaHa,
    double TotalAreaHa,
    List<string> AffectedAreas,
    int? EvacuationCount,
    int? StructuresDestroyed,
    int? FirefightersDeployed,
    double? DurationHours,
    FwiConditionResponse? FwiConditions,
    WeatherConditionResponse? WeatherConditions,
    List<TimelineEntryResponse>? Timeline,
    string? GeneratedContent,
    DateTime CreatedAt);

public record FwiConditionResponse(
    double StartFwi,
    double PeakFwi,
    double AverageFwi,
    string? WindDirection);

public record WeatherConditionResponse(
    double AvgTemp,
    double MaxTemp,
    double MinHumidity,
    double TotalPrecipitation,
    string? DominantWindDir);

public record TimelineEntryResponse(
    DateTime Time,
    string Event,
    string Description,
    double? AreaHa,
    double? Fwi);

public record FireTimelineResponse(
    Guid EventId,
    string EventTitle,
    DateTime OccurredAt,
    List<TimelineEntryResponse> Timeline);
