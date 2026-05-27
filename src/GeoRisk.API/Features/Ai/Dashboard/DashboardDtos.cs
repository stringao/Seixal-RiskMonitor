using System.Text.Json.Serialization;

namespace GeoRisk.API.Features.Ai.Dashboard;

public sealed record DashboardResponse(
    [property: JsonPropertyName("riskLevel")] string RiskLevel,
    [property: JsonPropertyName("trend")] string Trend,
    [property: JsonPropertyName("confidence")] double Confidence,
    [property: JsonPropertyName("keyFactors")] List<KeyFactorDto> KeyFactors,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("recommendations")] List<string> Recommendations,
    [property: JsonPropertyName("sources")] List<string> Sources,
    [property: JsonPropertyName("generatedAt")] DateTime GeneratedAt);

public sealed record KeyFactorDto(
    [property: JsonPropertyName("factor")] string Factor,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("description")] string Description);

public sealed record EventSummaryResponse(
    [property: JsonPropertyName("topEvents")] List<TopEventDto> TopEvents,
    [property: JsonPropertyName("patterns")] List<string> Patterns,
    [property: JsonPropertyName("severityDistribution")] SeverityDistributionDto SeverityDistribution,
    [property: JsonPropertyName("sourceBreakdown")] SourceBreakdownDto SourceBreakdown);

public sealed record TopEventDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("occurredAt")] DateTime OccurredAt,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("significance")] string Significance);

public sealed record SeverityDistributionDto(
    [property: JsonPropertyName("critical")] int Critical,
    [property: JsonPropertyName("high")] int High,
    [property: JsonPropertyName("medium")] int Medium,
    [property: JsonPropertyName("low")] int Low);

public sealed record SourceBreakdownDto(
    [property: JsonPropertyName("icnf")] int Icnf,
    [property: JsonPropertyName("anepc")] int Anepc,
    [property: JsonPropertyName("ipma")] int Ipma,
    [property: JsonPropertyName("manual")] int Manual,
    [property: JsonPropertyName("aiDetected")] int AiDetected);

public sealed record HotspotAnalysisResponse(
    [property: JsonPropertyName("concerningHotspots")] List<ConcerningHotspotDto> ConcerningHotspots,
    [property: JsonPropertyName("recentActivity")] string RecentActivity,
    [property: JsonPropertyName("historicalAverage")] double HistoricalAverageFwi,
    [property: JsonPropertyName("recommendations")] List<string> Recommendations);

public sealed record ConcerningHotspotDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("currentFwi")] double CurrentFwi,
    [property: JsonPropertyName("riskLevel")] string RiskLevel,
    [property: JsonPropertyName("concernReason")] string ConcernReason);

public sealed record WeeklyReportResponse(
    [property: JsonPropertyName("weekNumber")] int WeekNumber,
    [property: JsonPropertyName("year")] int Year,
    [property: JsonPropertyName("dayByDayBreakdown")] List<DayBreakdownDto> DayByDayBreakdown,
    [property: JsonPropertyName("comparisonPreviousWeek")] ComparisonDto ComparisonPreviousWeek,
    [property: JsonPropertyName("comparisonFiveYearAverage")] ComparisonDto ComparisonFiveYearAverage,
    [property: JsonPropertyName("weatherForecastImpact")] string WeatherForecastImpact,
    [property: JsonPropertyName("strategicRecommendations")] List<string> StrategicRecommendations,
    [property: JsonPropertyName("summary")] string Summary);

public sealed record DayBreakdownDto(
    [property: JsonPropertyName("date")] DateTime Date,
    [property: JsonPropertyName("dayName")] string DayName,
    [property: JsonPropertyName("eventCount")] int EventCount,
    [property: JsonPropertyName("dominantRisk")] string DominantRisk,
    [property: JsonPropertyName("weatherSummary")] string WeatherSummary);

public sealed record ComparisonDto(
    [property: JsonPropertyName("changePercent")] double ChangePercent,
    [property: JsonPropertyName("changeDescription")] string ChangeDescription,
    [property: JsonPropertyName("isIncrease")] bool IsIncrease);

public sealed record SituationReportResponse(
    [property: JsonPropertyName("activeEvents")] List<SituationEventDto> ActiveEvents,
    [property: JsonPropertyName("currentFwi")] CurrentFwiDto CurrentFwi,
    [property: JsonPropertyName("hotspots")] List<SituationHotspotDto> Hotspots,
    [property: JsonPropertyName("weatherTrend")] string WeatherTrend,
    [property: JsonPropertyName("overallRiskLevel")] string OverallRiskLevel,
    [property: JsonPropertyName("immediateRecommendations")] List<string> ImmediateRecommendations,
    [property: JsonPropertyName("generatedAt")] DateTime GeneratedAt);

public sealed record SituationEventDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("occurredAt")] DateTime OccurredAt);

public sealed record CurrentFwiDto(
    [property: JsonPropertyName("value")] double Value,
    [property: JsonPropertyName("riskLevel")] string RiskLevel,
    [property: JsonPropertyName("trend")] string Trend,
    [property: JsonPropertyName("averageRegion")] double AverageRegion);

public sealed record SituationHotspotDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("riskLevel")] string RiskLevel,
    [property: JsonPropertyName("fireCount")] int FireCount);
