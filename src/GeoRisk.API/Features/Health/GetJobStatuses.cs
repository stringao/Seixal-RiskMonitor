using GeoRisk.API.Common.CQRS;

namespace GeoRisk.API.Features.Health;

public sealed record GetJobStatusesQuery() : IQuery<IReadOnlyList<JobStatusResponse>>;

public sealed record JobStatusResponse(
    string Name,
    string Description,
    DateTime? LastRun,
    string Status,
    string? Error);

public sealed class GetJobStatusesHandler : IQueryHandler<GetJobStatusesQuery, IReadOnlyList<JobStatusResponse>>
{
    private static readonly Dictionary<string, (string Description, DateTime? LastRun, string Status, string? Error)> JobStatuses = new();
    private static DateTime _lastEventImport;
    private static DateTime _lastAiClassification;
    private static DateTime _lastPatternDetection;
    private static DateTime _lastReportGeneration;
    private static DateTime _lastAlertEvaluation;

    public Task<IReadOnlyList<JobStatusResponse>> HandleAsync(GetJobStatusesQuery query, CancellationToken ct)
    {
        var statuses = new List<JobStatusResponse>
        {
            new("EventImportJob", "Imports events from ICNF/IPMA/ANEPC every 30 minutes",
                _lastEventImport == default ? null : _lastEventImport, "Running", null),
            new("AIClassificationJob", "Classifies unclassified events every 5 minutes",
                _lastAiClassification == default ? null : _lastAiClassification, "Running", null),
            new("PatternDetectionJob", "Detects event patterns daily at 02:00",
                _lastPatternDetection == default ? null : _lastPatternDetection, "Running", null),
            new("ReportGenerationJob", "Generates weekly reports on Monday at 06:00",
                _lastReportGeneration == default ? null : _lastReportGeneration, "Running", null),
            new("AlertEvaluationJob", "Evaluates alert rules every 5 minutes",
                _lastAlertEvaluation == default ? null : _lastAlertEvaluation, "Running", null)
        };
        return Task.FromResult<IReadOnlyList<JobStatusResponse>>(statuses);
    }

    public static void RecordRun(string jobName)
    {
        var now = DateTime.UtcNow;
        switch (jobName)
        {
            case "EventImportJob": _lastEventImport = now; break;
            case "AIClassificationJob": _lastAiClassification = now; break;
            case "PatternDetectionJob": _lastPatternDetection = now; break;
            case "ReportGenerationJob": _lastReportGeneration = now; break;
            case "AlertEvaluationJob": _lastAlertEvaluation = now; break;
        }
    }
}

public static class GetJobStatusesEndpoint
{
    public static RouteGroupBuilder MapGetJobStatuses(this RouteGroupBuilder group)
    {
        group.MapGet("/jobs", async (IQueryHandler<GetJobStatusesQuery, IReadOnlyList<JobStatusResponse>> handler) =>
        {
            var result = await handler.HandleAsync(new GetJobStatusesQuery(), default);
            return Results.Ok(result);
        }).WithTags("Health");

        return group;
    }
}