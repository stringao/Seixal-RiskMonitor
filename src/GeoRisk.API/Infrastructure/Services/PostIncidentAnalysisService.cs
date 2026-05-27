using System.Text;
using System.Text.Json;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Service for post-incident analysis of fire events.
/// Generates fire reports with timeline reconstruction, cause analysis, and area estimation.
/// </summary>
public class PostIncidentAnalysisService
{
    private readonly GeoRiskDbContext _db;
    private readonly ILlmProvider _llm;
    private readonly ILlmSettingsService _llmSettings;
    private readonly ILogger<PostIncidentAnalysisService> _logger;
    private const string DefaultRegion = "Portugal";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public PostIncidentAnalysisService(
        GeoRiskDbContext db,
        ILlmProvider llm,
        ILlmSettingsService llmSettings,
        ILogger<PostIncidentAnalysisService> logger)
    {
        _db = db;
        _llm = llm;
        _llmSettings = llmSettings;
        _logger = logger;
    }

    /// <summary>
    /// Generates a complete fire report for a GeoEvent.
    /// </summary>
    public async Task<FireReport> GenerateFireReportAsync(Guid geoEventId, CancellationToken ct = default)
    {
        var geoEvent = await _db.GeoEvents.FirstOrDefaultAsync(e => e.Id == geoEventId, ct);
        if (geoEvent == null)
            throw new ArgumentException($"GeoEvent with ID {geoEventId} not found", nameof(geoEventId));

        var report = new FireReport
        {
            Id = Guid.NewGuid(),
            GeoEventId = geoEventId,
            Status = ReportStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            GeneratedBy = "AI"
        };

        _db.FireReports.Add(report);

        try
        {
            // Build timeline of fire evolution
            var timeline = await BuildTimelineAsync(geoEvent, ct);
            report.TimelineJson = JsonSerializer.Serialize(timeline, JsonOptions);

            // Analyze probable cause
            var causeResult = await AnalyzeCauseAsync(geoEvent, timeline, ct);
            report.ProbableCause = causeResult.Cause;
            report.CauseConfidence = causeResult.Confidence;

            // Estimate areas
            var areaEstimation = await EstimateAreasAsync(geoEvent, timeline, ct);
            report.PeakFireAreaHa = areaEstimation.PeakAreaHa;
            report.PeakFireTime = areaEstimation.PeakTime;
            report.TotalAreaHa = areaEstimation.TotalAreaHa;
            report.DurationHours = areaEstimation.DurationHours;
            report.AffectedAreas = areaEstimation.AffectedAreas;

            // Calculate FWI and weather conditions
            var fwiConditions = CalculateFwiConditions(timeline);
            report.FwiConditionsJson = JsonSerializer.Serialize(fwiConditions, JsonOptions);

            var weatherConditions = await CalculateWeatherConditionsAsync(geoEvent, timeline, ct);
            report.WeatherConditionsJson = JsonSerializer.Serialize(weatherConditions, JsonOptions);

            // Generate summary
            report.Summary = GenerateSummary(geoEvent, report, fwiConditions, weatherConditions);

            // Generate full markdown report using LLM
            report.GeneratedContent = await GenerateMarkdownReportAsync(geoEvent, report, timeline, ct);

            report.Status = ReportStatus.Completed;
            report.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate fire report for event {EventId}", geoEventId);
            report.Status = ReportStatus.Failed;
        }

        report.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return report;
    }

    /// <summary>
    /// Builds a chronological timeline of fire events based on related GeoEvents and predictions.
    /// </summary>
    public async Task<List<TimelineEntry>> BuildTimelineAsync(GeoEvent fireEvent, CancellationToken ct = default)
    {
        var timeline = new List<TimelineEntry>();

        // Find related events (same location over time, within 10km radius)
        var relatedEvents = await _db.GeoEvents
            .Where(e => e.EventType == Domain.Enums.EventType.Fire)
            .Where(e => e.OccurredAt >= fireEvent.OccurredAt.AddHours(-24))
            .Where(e => e.OccurredAt <= fireEvent.OccurredAt.AddDays(7))
            .Where(e => e.Geometry.Distance(fireEvent.Geometry) < 10000) // 10km in meters
            .AsNoTracking()
            .ToListAsync(ct);

        // Add ignition event
        timeline.Add(new TimelineEntry
        {
            Time = fireEvent.OccurredAt,
            Event = "Ignition",
            Description = $"Fire reported at location ({fireEvent.Geometry.Y:F4}, {fireEvent.Geometry.X:F4})",
            AreaHa = 0.1
        });

        // Add related events to timeline
        foreach (var evt in relatedEvents.OrderBy(e => e.OccurredAt))
        {
            if (evt.Id == fireEvent.Id) continue;

            var areaHa = await EstimateAreaAtTimeAsync(evt, ct);
            timeline.Add(new TimelineEntry
            {
                Time = evt.OccurredAt,
                Event = "Spread",
                Description = evt.Description ?? $"Fire spread detected - Severity: {evt.Severity}",
                AreaHa = areaHa
            });
        }

        // Get fire spread predictions to estimate peak
        var predictions = await _db.FireSpreadPredictions
            .Where(p => p.FireEventId == fireEvent.Id)
            .OrderByDescending(p => p.AreaKm2)
            .AsNoTracking()
            .ToListAsync(ct);

        if (predictions.Count > 0)
        {
            var peakPrediction = predictions[0];
            var peakTime = fireEvent.OccurredAt.AddHours(peakPrediction.HorizonHours);
            timeline.Add(new TimelineEntry
            {
                Time = peakTime,
                Event = "Peak",
                Description = $"Estimated peak fire size: {peakPrediction.AreaKm2 * 100:F2} ha",
                AreaHa = peakPrediction.AreaKm2 * 100,
                Fwi = peakPrediction.FWI
            });
        }

        // Add containment and extinction markers
        var lastEvent = relatedEvents.OrderByDescending(e => e.OccurredAt).FirstOrDefault();
        if (lastEvent != null)
        {
            timeline.Add(new TimelineEntry
            {
                Time = lastEvent.OccurredAt.AddHours(2),
                Event = "Contained",
                Description = "Fire reported as contained by emergency services"
            });

            timeline.Add(new TimelineEntry
            {
                Time = lastEvent.OccurredAt.AddHours(6),
                Event = "Extinguished",
                Description = "Fire reported as extinguished"
            });
        }

        return timeline.OrderBy(t => t.Time).ToList();
    }

    /// <summary>
    /// Analyzes the probable cause of a fire based on weather, time, and location data.
    /// </summary>
    public async Task<CauseAnalysisResult> AnalyzeCauseAsync(
        GeoEvent fireEvent,
        List<TimelineEntry> timeline,
        CancellationToken ct = default)
    {
        var settings = await _llmSettings.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
        {
            return new CauseAnalysisResult("unknown", 0.0);
        }

        // Gather weather data at time of fire
        var fireTime = fireEvent.OccurredAt;
        var weatherData = await _db.WeatherRiskDataPoints
            .Where(p => p.Timestamp >= fireTime.AddHours(-2))
            .Where(p => p.Timestamp <= fireTime.AddHours(2))
            .Where(p => p.Location.Distance(fireEvent.Geometry) < 5000)
            .OrderBy(p => Math.Abs(p.Timestamp.Ticks - fireTime.Ticks))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        var fwi = weatherData?.FWI ?? 0;
        var temperature = weatherData?.Temperature ?? 0;
        var humidity = weatherData?.Humidity ?? 0;
        var windSpeed = weatherData?.WindSpeed ?? 0;

        // Day of week analysis
        var dayOfWeek = fireTime.DayOfWeek;
        var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
        var hourOfDay = fireTime.Hour;

        // Build prompt for LLM
        var systemPrompt = "You are a fire cause analysis expert. Analyze the provided data and determine the most likely cause of the fire.";
        var userPrompt = $"""
            Analyze this fire event and determine its probable cause.

            Fire Details:
            - Date/Time: {fireTime:yyyy-MM-dd HH:mm}
            - Day of Week: {dayOfWeek}
            - Hour: {hourOfDay}:00
            - Is Weekend: {isWeekend}
            - Location: ({fireEvent.Geometry.Y:F4}, {fireEvent.Geometry.X:F4})

            Weather Conditions at Time of Fire:
            - FWI: {fwi:F1}
            - Temperature: {temperature:F1}°C
            - Humidity: {humidity:F1}%
            - Wind Speed: {windSpeed:F1} km/h

            Weather Risk Level: {FwiCalculator.GetDangerRating(fwi)}

            Return a JSON object with this structure:
            [COURSE_OUTPUT_START]
                "cause": "natural|accidental|intentional|unknown",
                "confidence": 0.0-1.0,
                "reasoning": "brief explanation in Portuguese (pt-BR)"
            [COURSE_OUTPUT_END]
            """.Replace("[COURSE_OUTPUT_START]", "{").Replace("[COURSE_OUTPUT_END]", "}");

        try
        {
            var result = await _llm.CompleteStructuredAsync<CauseAnalysisJson>(systemPrompt, userPrompt, ct);
            return new CauseAnalysisResult(result.Cause, result.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM cause analysis failed, using fallback");
            return AnalyzeCauseFallback(fwi, temperature, humidity, windSpeed, isWeekend, hourOfDay);
        }
    }

    private static CauseAnalysisResult AnalyzeCauseFallback(double fwi, double temp, double humidity, double windSpeed, bool isWeekend, int hour)
    {
        // Simple heuristic fallback
        string cause;
        double confidence;

        // High risk conditions + weekend + afternoon/evening = higher intentional probability
        if (isWeekend && hour >= 12 && hour <= 20 && fwi > 20)
        {
            cause = "intentional";
            confidence = 0.6;
        }
        else if (temp > 35 && humidity < 20 && windSpeed > 15)
        {
            cause = "natural";
            confidence = 0.7;
        }
        else if (fwi < 10)
        {
            cause = "accidental";
            confidence = 0.5;
        }
        else
        {
            cause = "unknown";
            confidence = 0.3;
        }

        return new CauseAnalysisResult(cause, confidence);
    }

    /// <summary>
    /// Estimates the areas affected by the fire.
    /// </summary>
    public async Task<AreaEstimationResult> EstimateAreasAsync(
        GeoEvent fireEvent,
        List<TimelineEntry> timeline,
        CancellationToken ct = default)
    {
        var result = new AreaEstimationResult();

        // Get fire spread predictions
        var predictions = await _db.FireSpreadPredictions
            .Where(p => p.FireEventId == fireEvent.Id)
            .AsNoTracking()
            .ToListAsync(ct);

        if (predictions.Count != 0)
        {
            var maxArea = predictions.Max(p => p.AreaKm2);
            result.PeakAreaHa = maxArea * 100; // Convert to hectares
            result.TotalAreaHa = result.PeakAreaHa * 0.9; // Estimate 90% of peak as final

            var maxPred = predictions.First(p => Math.Abs(p.AreaKm2 - maxArea) < 0.001);
            result.PeakTime = fireEvent.OccurredAt.AddHours(maxPred.HorizonHours);

            // Get affected municipalities from predictions
            result.AffectedAreas = predictions
                .SelectMany(p => p.AffectedMunicipalities)
                .Distinct()
                .ToList();
        }
        else
        {
            // Fallback estimation based on fire severity and duration
            var baseArea = fireEvent.Severity switch
            {
                RiskLevel.Low => 0.5,
                RiskLevel.Medium => 5,
                RiskLevel.High => 25,
                RiskLevel.Critical => 100,
                _ => 5
            };

            result.TotalAreaHa = baseArea;
            result.PeakAreaHa = baseArea;
            result.PeakTime = fireEvent.OccurredAt.AddHours(4);
        }

        // Calculate duration
        if (timeline.Count >= 2)
        {
            var first = timeline[0];
            var last = timeline.Last(t => t.Event is "Extinguished" or "Contained");
            if (first != null && last != null)
            {
                result.DurationHours = (last.Time - first.Time).TotalHours;
            }
        }
        else
        {
            result.DurationHours = 6; // Default estimate
        }

        return result;
    }

    private async Task<double> EstimateAreaAtTimeAsync(GeoEvent evt, CancellationToken ct)
    {
        // Simple estimation based on severity and time since start
        var predictions = await _db.FireSpreadPredictions
            .Where(p => p.FireEventId == evt.Id)
            .AsNoTracking()
            .ToListAsync(ct);

        if (predictions.Count > 0)
        {
            return predictions.Max(p => p.AreaKm2) * 100;
        }

        return evt.Severity switch
        {
            RiskLevel.Low => 0.5,
            RiskLevel.Medium => 5,
            RiskLevel.High => 25,
            RiskLevel.Critical => 100,
            _ => 5
        };
    }

    private static FwiCondition CalculateFwiConditions(List<TimelineEntry> timeline)
    {
        var fwiValues = timeline.Where(t => t.Fwi.HasValue).Select(t => t.Fwi!.Value).ToList();

        return new FwiCondition
        {
            StartFwi = fwiValues.FirstOrDefault(),
            PeakFwi = fwiValues.Max(),
            AverageFwi = fwiValues.Count > 0 ? fwiValues.Average() : 0,
            WindDirection = "NE" // Would need actual wind direction tracking
        };
    }

    private async Task<WeatherCondition> CalculateWeatherConditionsAsync(
        GeoEvent fireEvent,
        List<TimelineEntry> timeline,
        CancellationToken ct)
    {
        var startTime = timeline[0].Time;
        var endTime = timeline[timeline.Count - 1].Time;

        var weatherData = await _db.WeatherRiskDataPoints
            .Where(p => p.Timestamp >= startTime.AddHours(-1))
            .Where(p => p.Timestamp <= endTime.AddHours(1))
            .Where(p => p.Location.Distance(fireEvent.Geometry) < 10000)
            .AsNoTracking()
            .ToListAsync(ct);

        if (weatherData.Count == 0)
        {
            return new WeatherCondition { AvgTemp = 25, MaxTemp = 35, MinHumidity = 20 };
        }

        return new WeatherCondition
        {
            AvgTemp = weatherData.Average(w => w.Temperature),
            MaxTemp = weatherData.Max(w => w.Temperature),
            MinHumidity = weatherData.Min(w => w.Humidity),
            TotalPrecipitation = weatherData.Sum(w => w.Precipitation),
            DominantWindDir = GetDominantWindDirection(weatherData)
        };
    }

    private static string GetDominantWindDirection(List<WeatherRiskDataPoint> data)
    {
        var directions = data.Select(w => GetWindCardinal(w.WindDirection)).ToList();
        return directions.GroupBy(d => d).OrderByDescending(g => g.Count()).First().Key;
    }

    private static string GetWindCardinal(double degrees)
    {
        var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        var index = (int)Math.Round(degrees / 45) % 8;
        return directions[index];
    }

    private static string GenerateSummary(
        GeoEvent fireEvent,
        FireReport report,
        FwiCondition fwi,
        WeatherCondition weather)
    {
        var causeText = report.ProbableCause switch
        {
            "natural" => "causa natural",
            "accidental" => "causa acidental",
            "intentional" => "causa intencional",
            _ => "causa desconhecida"
        };

        return $"Incêndio {fireEvent.Title} ocorreu em {fireEvent.OccurredAt:dd/MM/yyyy HH:mm} " +
               $"na região de {report.AffectedAreas.FirstOrDefault() ?? "desconhecida"}. " +
               $"Área total afetada: {report.TotalAreaHa:F2} ha. " +
               $"Causa provável: {causeText} (confiança: {report.CauseConfidence:P0}). " +
               $"FWI médio: {fwi.AverageFwi:F1}, Temperatura máxima: {weather.MaxTemp:F1}°C.";
    }

    private async Task<string> GenerateMarkdownReportAsync(
        GeoEvent fireEvent,
        FireReport report,
        List<TimelineEntry> timeline,
        CancellationToken ct)
    {
        var settings = await _llmSettings.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
        {
            return GenerateFallbackReport(fireEvent, report, timeline);
        }

        var systemPrompt = "You are a municipal risk analyst generating fire incident reports in Brazilian Portuguese (pt-BR).";
        var timelineText = string.Join("\n", timeline.Select(t =>
            $"- **{t.Time:HH:mm}** - {t.Event}: {t.Description} (Área: {t.AreaHa?.ToString("F2") ?? "N/A"} ha)"));

        var userPrompt = $"""
            Generate a comprehensive fire incident report in Brazilian Portuguese (pt-BR) markdown format.

            ## Fire Event Information
            - Title: {fireEvent.Title}
            - Date/Time: {fireEvent.OccurredAt:yyyy-MM-dd HH:mm}
            - Location: ({fireEvent.Geometry.Y:F4}, {fireEvent.Geometry.X:F4})
            - Severity: {fireEvent.Severity}
            - Source: {fireEvent.Source}

            ## Analysis Results
            - Probable Cause: {report.ProbableCause} (confidence: {report.CauseConfidence:P0})
            - Total Area: {report.TotalAreaHa:F2} hectares
            - Peak Area: {report.PeakFireAreaHa:F2} hectares
            - Peak Time: {report.PeakFireTime:yyyy-MM-dd HH:mm}
            - Duration: {report.DurationHours:F1} hours
            - Affected Areas: {string.Join(", ", report.AffectedAreas)}

            ## Fire Evolution Timeline
            {timelineText}

            Generate a detailed markdown report with sections:
            1. Resumo Executivo
            2. Análise da Causa
            3. Evolução do Incêndio
            4. Condições Meteorológicas
            5. Áreas Afetadas
            6. Recomendações
            """;

        try
        {
            return await _llm.CompleteAsync(systemPrompt, userPrompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM report generation failed, using fallback");
            return GenerateFallbackReport(fireEvent, report, timeline);
        }
    }

    private static string GenerateFallbackReport(
        GeoEvent fireEvent,
        FireReport report,
        List<TimelineEntry> timeline)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"""
            # Relatório de Incêndio - {fireEvent.Title}

            ## Resumo Executivo
            {report.Summary}

            ## Detalhes do Evento
            - **Data/Hora:** {fireEvent.OccurredAt:yyyy-MM-dd HH:mm}
            - **Localização:** ({fireEvent.Geometry.Y:F4}, {fireEvent.Geometry.X:F4})
            - **Severidade:** {fireEvent.Severity}
            - **Fonte:** {fireEvent.Source}

            ## Análise da Causa
            - **Causa Provável:** {report.ProbableCause}
            - **Confiança:** {report.CauseConfidence:P0}

            ## Estatísticas do Incêndio
            - **Área Total:** {report.TotalAreaHa:F2} hectares
            - **Área de Pico:** {report.PeakFireAreaHa:F2} hectares
            - **Hora de Pico:** {report.PeakFireTime:yyyy-MM-dd HH:mm}
            - **Duração:** {report.DurationHours:F1} horas
            - **Áreas Afetadas:** {string.Join(", ", report.AffectedAreas)}

            ## Cronologia
            """);

        foreach (var entry in timeline)
        {
            sb.AppendLine($"- **{entry.Time:HH:mm}** - {entry.Event}: {entry.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("*Relatório gerado automaticamente pelo sistema GeoRisk.*");
        return sb.ToString();
    }

    /// <summary>
    /// Calculates or updates seasonal statistics for a given year/month/region.
    /// </summary>
    public async Task<SeasonalStatistics> CalculateSeasonalStatisticsAsync(
        int year,
        int month,
        string region = DefaultRegion,
        CancellationToken ct = default)
    {
        var (startDate, endDate) = CalculateDateRange(year, month);
        var events = await GetFireEventsAsync(startDate, endDate, region, ct);
        var weatherData = await GetWeatherDataAsync(startDate, endDate, region, ct);

        var stats = CalculateStatistics(year, month, region, events, weatherData);

        return await SaveOrUpdateStatisticsAsync(stats, year, month, region, ct);
    }

    private static (DateTime startDate, DateTime endDate) CalculateDateRange(int year, int month)
    {
        var startDate = month == 0
            ? new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        var endDate = month == 0
            ? startDate.AddYears(1).AddTicks(-1)
            : startDate.AddMonths(1).AddTicks(-1);

        return (startDate, endDate);
    }

    private async Task<List<GeoEvent>> GetFireEventsAsync(DateTime startDate, DateTime endDate, string region, CancellationToken ct)
    {
        var query = _db.GeoEvents
            .Where(e => e.EventType == Domain.Enums.EventType.Fire)
            .Where(e => e.OccurredAt >= startDate && e.OccurredAt <= endDate);

        if (region != DefaultRegion && region != "Setubal")
        {
            var regionPattern = $"%\"municipality\":\"{region}\"%";
            var regionLowerPattern = $"%\"municipality\":\"{region.ToLower()}%";
            query = query.Where(e => e.Metadata != null &&
                (EF.Functions.ILike(e.Metadata, regionPattern) ||
                 EF.Functions.ILike(e.Metadata, regionLowerPattern)));
        }

        return await query.AsNoTracking().ToListAsync(ct);
    }

    private async Task<List<WeatherRiskDataPoint>> GetWeatherDataAsync(DateTime startDate, DateTime endDate, string region, CancellationToken ct)
    {
        var weatherQuery = _db.WeatherRiskDataPoints
            .Where(p => p.Timestamp >= startDate && p.Timestamp <= endDate);

        if (region != DefaultRegion)
        {
            weatherQuery = weatherQuery.Where(p =>
                p.Municipality != null &&
                (EF.Functions.ILike(p.Municipality, region) ||
                 EF.Functions.ILike(p.Municipality, region.ToLower())));
        }

        return await weatherQuery.AsNoTracking().ToListAsync(ct);
    }

    private static SeasonalStatistics CalculateStatistics(int year, int month, string region, List<GeoEvent> events, List<WeatherRiskDataPoint> weatherData)
    {
        var stats = new SeasonalStatistics
        {
            Id = Guid.NewGuid(),
            Year = year,
            Month = month,
            Region = region,
            TotalFires = events.Count,
            TotalAreaHa = events.Sum(e => EstimateEventArea(e)),
            LargestFireHa = events.Max(e => EstimateEventArea(e)),
            AverageFwi = weatherData.Count > 0 ? weatherData.Average(w => w.FWI) : 0,
            AverageTemperature = weatherData.Count > 0 ? weatherData.Average(w => w.Temperature) : 0,
            TotalPrecipitationMm = weatherData.Sum(w => w.Precipitation),
            CalculatedAt = DateTime.UtcNow
        };

        if (month > 0)
        {
            stats.DailyFireCounts = CalculateDailyFireCounts(year, month, events);
            stats.PeakFireDay = events.OrderByDescending(e => EstimateEventArea(e)).FirstOrDefault()?.OccurredAt;
        }

        stats.FireCauseBreakdown = JsonSerializer.Serialize(new Dictionary<string, int>
        {
            { "unknown", events.Count }
        }, JsonOptions);

        return stats;
    }

    private static string CalculateDailyFireCounts(int year, int month, List<GeoEvent> events)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dailyCounts = new int[daysInMonth];
        foreach (var evt in events)
        {
            var day = evt.OccurredAt.Day - 1;
            if (day >= 0 && day < daysInMonth)
                dailyCounts[day]++;
        }
        return JsonSerializer.Serialize(dailyCounts, JsonOptions);
    }

    private async Task<SeasonalStatistics> SaveOrUpdateStatisticsAsync(SeasonalStatistics stats, int year, int month, string region, CancellationToken ct)
    {
        var existing = await _db.SeasonalStatistics
            .FirstOrDefaultAsync(s => s.Year == year && s.Month == month && s.Region == region, ct);

        if (existing != null)
        {
            existing.TotalFires = stats.TotalFires;
            existing.TotalAreaHa = stats.TotalAreaHa;
            existing.LargestFireHa = stats.LargestFireHa;
            existing.AverageFwi = stats.AverageFwi;
            existing.AverageTemperature = stats.AverageTemperature;
            existing.TotalPrecipitationMm = stats.TotalPrecipitationMm;
            existing.PeakFireDay = stats.PeakFireDay;
            existing.FireCauseBreakdown = stats.FireCauseBreakdown;
            existing.DailyFireCounts = stats.DailyFireCounts;
            existing.CalculatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.SeasonalStatistics.Add(stats);
        }

        await _db.SaveChangesAsync(ct);
        return existing ?? stats;
    }

    private static double EstimateEventArea(GeoEvent evt)
    {
        // Simple estimation based on severity
        return evt.Severity switch
        {
            RiskLevel.Low => 0.5,
            RiskLevel.Medium => 5,
            RiskLevel.High => 25,
            RiskLevel.Critical => 100,
            _ => 5
        };
    }

    /// <summary>
    /// Gets 5-year trend analysis for fires.
    /// </summary>
    public async Task<TrendAnalysisResult> GetTrendAnalysisAsync(
        string region = DefaultRegion,
        CancellationToken ct = default)
    {
        var fiveYearsAgo = DateTime.UtcNow.Year - 5;

        var yearlyStats = await _db.SeasonalStatistics
            .Where(s => s.Year >= fiveYearsAgo && s.Month == 0 && s.Region == region)
            .OrderBy(s => s.Year)
            .AsNoTracking()
            .ToListAsync(ct);

        var firesPerYear = yearlyStats.Select(s => s.TotalFires).ToList();
        var areaPerYear = yearlyStats.Select(s => s.TotalAreaHa).ToList();

        var trendDirection = CalculateTrendDirection(firesPerYear);

        return new TrendAnalysisResult(
            Enumerable.Range(0, yearlyStats.Count).Select(i => yearlyStats[i].Year).ToList(),
            firesPerYear,
            areaPerYear,
            trendDirection);
    }

    private static string CalculateTrendDirection(List<int> values)
    {
        if (values.Count < 2) return "stable";

        var firstHalf = values.Take(values.Count / 2).Average();
        var secondHalf = values.Skip(values.Count / 2).Average();

        var change = (secondHalf - firstHalf) / firstHalf;

        return change switch
        {
            > 0.2 => "increasing",
            < -0.2 => "decreasing",
            _ => "stable"
        };
    }

    /// <summary>
    /// Compares current season with historical average.
    /// </summary>
    public async Task<SeasonComparisonResult> CompareWithHistoricalAsync(
        int year,
        int month,
        string region = DefaultRegion,
        CancellationToken ct = default)
    {
        var currentStats = await _db.SeasonalStatistics
            .FirstOrDefaultAsync(s => s.Year == year && s.Month == month && s.Region == region, ct);

        var historicalStats = await _db.SeasonalStatistics
            .Where(s => s.Year < year && s.Month == month && s.Region == region)
            .AsNoTracking()
            .ToListAsync(ct);

        if (historicalStats.Count == 0)
        {
            return new SeasonComparisonResult(null, null, null, null, "insufficient_data");
        }

        var avgFires = historicalStats.Average(s => s.TotalFires);
        var avgArea = historicalStats.Average(s => s.TotalAreaHa);
        var avgFwi = historicalStats.Average(s => s.AverageFwi);

        string comparison;
        double? fireChange = null;
        double? areaChange = null;
        double? fwiChange = null;

        if (currentStats != null)
        {
            fireChange = avgFires > 0 ? (currentStats.TotalFires - avgFires) / avgFires : 0;
            areaChange = avgArea > 0 ? (currentStats.TotalAreaHa - avgArea) / avgArea : 0;
            fwiChange = avgFwi > 0 ? (currentStats.AverageFwi - avgFwi) / avgFwi : 0;

            comparison = fireChange switch
            {
                > 0.2 => "above_average",
                < -0.2 => "below_average",
                _ => "normal"
            };
        }
        else
        {
            comparison = "no_current_data";
        }

        return new SeasonComparisonResult(
            currentStats?.TotalFires,
            avgFires,
            currentStats?.TotalAreaHa,
            avgArea,
            comparison,
            fireChange,
            areaChange,
            fwiChange);
    }
}

public record CauseAnalysisResult(string Cause, double Confidence);

internal record CauseAnalysisJson(string Cause, double Confidence);

public record AreaEstimationResult
{
    public double PeakAreaHa { get; set; }
    public DateTime? PeakTime { get; set; }
    public double TotalAreaHa { get; set; }
    public double DurationHours { get; set; }
    public List<string> AffectedAreas { get; set; } = new();
}

public record TrendAnalysisResult(
    List<int> Years,
    List<int> FiresPerYear,
    List<double> AreaPerYear,
    string TrendDirection);

public record SeasonComparisonResult(
    int? CurrentFires,
    double? HistoricalAvgFires,
    double? CurrentAreaHa,
    double? HistoricalAvgAreaHa,
    string Comparison,
    double? FireChange = null,
    double? AreaChange = null,
    double? FwiChange = null);
