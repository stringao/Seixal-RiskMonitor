using System.Globalization;
using System.Text.Json;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Features.Ai.Dashboard;
using GeoRisk.API.Infrastructure.AI;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.Services;

public interface IDashboardGeneratorService
{
    Task<DashboardResponse?> GenerateRiskSummaryAsync(CancellationToken ct = default);
    Task<EventSummaryResponse?> GenerateEventSummaryAsync(List<GeoEvent> events, CancellationToken ct = default);
    Task<HotspotAnalysisResponse?> GenerateHotspotAnalysisAsync(List<FireHotspot> hotspots, CancellationToken ct = default);
    Task<WeeklyReportResponse?> GenerateWeeklyReportAsync(CancellationToken ct = default);
    Task<SituationReportResponse?> GenerateSituationReportAsync(CancellationToken ct = default);
    Task<string?> GetCachedDashboardAsync(string cacheKey, CancellationToken ct = default);
    Task SetCachedDashboardAsync(string cacheKey, string content, string model, TimeSpan expiration, CancellationToken ct = default);
}

public sealed class DashboardGeneratorService(
    GeoRiskDbContext db,
    ILlmProvider llm,
    ILlmSettingsService settingsService,
    ILogger<DashboardGeneratorService> logger) : IDashboardGeneratorService
{
    private const string StableTrend = "stable";

    public async Task<DashboardResponse?> GenerateRiskSummaryAsync(CancellationToken ct = default)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
        {
            logger.LogWarning("LLM not configured for dashboard generation");
            return null;
        }

        try
        {
            // Get current FWI data
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var fwiData = await db.WeatherRiskDataPoints
                .Where(w => w.Timestamp >= todayStart)
                .AsNoTracking()
                .ToListAsync(ct);

            // Get historical average for this time of year (last 30 days for comparison)
            var lastMonthStart = now.AddDays(-30);
            var historicalFwi = await db.WeatherRiskDataPoints
                .Where(w => w.Timestamp >= lastMonthStart && w.Timestamp < todayStart)
                .AsNoTracking()
                .ToListAsync(ct);

            var currentAvgFwi = fwiData.Count > 0 ? fwiData.Average(w => w.FWI) : 0;
            var historicalAvgFwi = historicalFwi.Count > 0 ? historicalFwi.Average(w => w.FWI) : 0;

            // Get forecast data
            var forecastData = await db.FwiForecasts
                .Where(f => f.ForecastDate >= todayStart && f.ForecastDate <= todayStart.AddDays(3))
                .AsNoTracking()
                .ToListAsync(ct);

            var systemPrompt = "You are a fire risk analysis expert for the Setúbal region in Portugal. Generate JSON output only.";
            var userPrompt = BuildRiskSummaryPrompt(currentAvgFwi, historicalAvgFwi, fwiData, forecastData);

            var result = await llm.CompleteStructuredAsync<DashboardResponse>(systemPrompt, userPrompt, ct);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate risk summary");
            return await GenerateFallbackRiskSummaryAsync(ct);
        }
    }

    public async Task<EventSummaryResponse?> GenerateEventSummaryAsync(List<GeoEvent> events, CancellationToken ct = default)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured || events.Count == 0)
        {
            return GenerateFallbackEventSummary(events);
        }

        try
        {
            var systemPrompt = "You are an event analysis expert for municipal risk monitoring. Generate JSON output only in pt-BR.";
            var userPrompt = BuildEventSummaryPrompt(events);

            return await llm.CompleteStructuredAsync<EventSummaryResponse>(systemPrompt, userPrompt, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate event summary");
            return GenerateFallbackEventSummary(events);
        }
    }

    public async Task<HotspotAnalysisResponse?> GenerateHotspotAnalysisAsync(List<FireHotspot> hotspots, CancellationToken ct = default)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured || hotspots.Count == 0)
        {
            return GenerateFallbackHotspotAnalysis(hotspots);
        }

        try
        {
            var systemPrompt = "You are a fire hotspot analysis expert for Portuguese municipal risk monitoring. Generate JSON output only in pt-BR.";
            var userPrompt = BuildHotspotAnalysisPrompt(hotspots);

            return await llm.CompleteStructuredAsync<HotspotAnalysisResponse>(systemPrompt, userPrompt, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate hotspot analysis");
            return GenerateFallbackHotspotAnalysis(hotspots);
        }
    }

    public async Task<WeeklyReportResponse?> GenerateWeeklyReportAsync(CancellationToken ct = default)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
        {
            logger.LogWarning("LLM not configured for weekly report generation");
            return null;
        }

        try
        {
            var now = DateTime.UtcNow;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            // Get this week's events
            var thisWeekEvents = await db.GeoEvents
                .Where(e => e.OccurredAt >= weekStart && e.OccurredAt < weekEnd)
                .AsNoTracking()
                .ToListAsync(ct);

            // Get previous week's events
            var prevWeekStart = weekStart.AddDays(-7);
            var prevWeekEvents = await db.GeoEvents
                .Where(e => e.OccurredAt >= prevWeekStart && e.OccurredAt < weekStart)
                .AsNoTracking()
                .ToListAsync(ct);

            // Get FWI data for the week
            var weekFwiData = await db.WeatherRiskDataPoints
                .Where(w => w.Timestamp >= weekStart && w.Timestamp < weekEnd)
                .AsNoTracking()
                .ToListAsync(ct);

            // Get forecast
            var forecastData = await db.FwiForecasts
                .Where(f => f.ForecastDate >= now.Date && f.ForecastDate <= now.Date.AddDays(7))
                .AsNoTracking()
                .ToListAsync(ct);

            // Get historical data for same week over 5 years (simplified - using last 5 weeks as proxy)
            var fiveWeeksAgo = weekStart.AddDays(-35);
            var historicalEvents = await db.GeoEvents
                .Where(e => e.OccurredAt >= fiveWeeksAgo && e.OccurredAt < weekStart)
                .AsNoTracking()
                .ToListAsync(ct);

            var systemPrompt = "You are a weekly risk report analyst for Portuguese municipal monitoring. Generate JSON output only in pt-BR.";
            var userPrompt = BuildWeeklyReportPrompt(thisWeekEvents, prevWeekEvents, historicalEvents, weekFwiData, forecastData, weekStart);

            return await llm.CompleteStructuredAsync<WeeklyReportResponse>(systemPrompt, userPrompt, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate weekly report");
            return null;
        }
    }

    public async Task<SituationReportResponse?> GenerateSituationReportAsync(CancellationToken ct = default)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
        {
            logger.LogWarning("LLM not configured for situation report generation");
            return null;
        }

        try
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;

            // Get active events (last 24h)
            var activeEvents = await db.GeoEvents
                .Where(e => e.OccurredAt >= todayStart.AddDays(-1))
                .AsNoTracking()
                .ToListAsync(ct);

            // Get current FWI
            var currentFwi = await db.WeatherRiskDataPoints
                .Where(w => w.Timestamp >= todayStart)
                .AsNoTracking()
                .ToListAsync(ct);

            // Get hotspots
            var hotspots = await db.FireHotspots
                .AsNoTracking()
                .ToListAsync(ct);

            // Get weather trend
            var weatherHistory = await db.WeatherRiskDataPoints
                .Where(w => w.Timestamp >= todayStart.AddDays(-3))
                .AsNoTracking()
                .ToListAsync(ct);

            var systemPrompt = "You are a real-time situation analyst for Portuguese municipal risk monitoring. Generate JSON output only in pt-BR.";
            var userPrompt = BuildSituationReportPrompt(activeEvents, currentFwi, hotspots, weatherHistory);

            return await llm.CompleteStructuredAsync<SituationReportResponse>(systemPrompt, userPrompt, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate situation report");
            return null;
        }
    }

    public async Task<string?> GetCachedDashboardAsync(string cacheKey, CancellationToken ct = default)
    {
        var cached = await db.DashboardCaches
            .Where(c => c.CacheKey == cacheKey && c.ExpiresAt > DateTime.UtcNow)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        return cached?.Content;
    }

    public async Task SetCachedDashboardAsync(string cacheKey, string content, string model, TimeSpan expiration, CancellationToken ct = default)
    {
        var existing = await db.DashboardCaches
            .Where(c => c.CacheKey == cacheKey)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            existing.Content = content;
            existing.GeneratedBy = model;
            existing.GeneratedAt = DateTime.UtcNow;
            existing.ExpiresAt = DateTime.UtcNow.Add(expiration);
        }
        else
        {
            db.DashboardCaches.Add(new DashboardCache
            {
                CacheKey = cacheKey,
                Content = content,
                GeneratedBy = model,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static string BuildRiskSummaryPrompt(double currentAvgFwi, double historicalAvgFwi,
        List<WeatherRiskDataPoint> fwiData, List<FwiForecast> forecastData)
    {
        var fwiTrend = DetermineFwiTrend(currentAvgFwi, historicalAvgFwi);
        var riskLevel = currentAvgFwi switch
        {
            > 30 => "Critical",
            > 20 => "High",
            > 10 => "Medium",
            _ => "Low"
        };

        var forecastSummary = forecastData.Count > 0
            ? $"Forecast FWI for next 3 days: min={forecastData.Min(x => x.FWI):F1}, max={forecastData.Max(x => x.FWI):F1}, avg={forecastData.Average(x => x.FWI):F1}"
            : "No forecast data available";

        var weatherSummary = fwiData.Count > 0
            ? $"Temperature: avg={fwiData.Average(w => w.Temperature):F1}°C, Humidity: avg={fwiData.Average(w => w.Humidity):F1}%, Wind: avg={fwiData.Average(w => w.WindSpeed):F1}km/h"
            : "No weather data available";

        return $@"Analise o risco de incêndio atual para a região de Setúbal e gere um JSON com a estrutura especificada.

DADOS ATUAIS:
- FWI atual médio: {currentAvgFwi:F1}
- Média histórica (últimos 30 dias): {historicalAvgFwi:F1}
- Tendência: {fwiTrend}
- Nível de risco: {riskLevel}

CONDIÇÕES METEOROLÓGICAS ATUAIS:
{weatherSummary}

PREVISÃO FWI (PRÓXIMOS 3 DIAS):
{forecastSummary}

FONTES DE DADOS:
- WeatherRiskDataPoints: {fwiData.Count} registros hoje
- FwiForecasts: {forecastData.Count} registros de previsão

Gerar JSON com esta estrutura exata (em pt-BR):
{{
  ""riskLevel"": ""Low|Medium|High|Critical"",
  ""trend"": ""increasing|decreasing|stable"",
  ""confidence"": 0.0-1.0,
  ""keyFactors"": [
    {{""factor"": ""FWI"", ""value"": 38.5, ""description"": ""descrição""}},
    {{""factor"": ""Temperatura"", ""value"": ""35°C"", ""description"": ""descrição""}}
  ],
  ""summary"": ""resumo em 2-3 frases"",
  ""recommendations"": [""rec1"", ""rec2""],
  ""sources"": [""WeatherRiskDataPoint:guid1"", ""FwiForecast:guid2""]
}}";
    }

    private static string BuildEventSummaryPrompt(List<GeoEvent> events)
    {
        var eventsJson = string.Join("\n", events.Select(e =>
            $"- ID:{e.Id} | {e.EventType} | {e.Title} | {e.Severity} | {e.Source} | {e.OccurredAt:yyyy-MM-dd HH:mm} | Location:({e.Geometry.Y:F4},{e.Geometry.X:F4})"));

        return $@"Analise os seguintes eventos e gere um JSON com o resumo em pt-BR.

EVENTOS ({events.Count} total):
{eventsJson}

Gerar JSON com esta estrutura exata:
{{
  ""topEvents"": [
    {{""id"": ""guid"", ""title"": ""título"", ""eventType"": ""Fire|Flood|Storm"", ""severity"": ""Low|Medium|High|Critical"", ""occurredAt"": ""2024-01-15T10:30:00Z"", ""location"": ""lat,lon"", ""significance"": ""significado""}}
  ],
  ""patterns"": [""padrão1"", ""padrão2""],
  ""severityDistribution"": {{""critical"": 0, ""high"": 0, ""medium"": 0, ""low"": 0}},
  ""sourceBreakdown"": {{""icnf"": 0, ""anepc"": 0, ""ipma"": 0, ""manual"": 0, ""aiDetected"": 0}}
}}";
    }

    private static string BuildHotspotAnalysisPrompt(List<FireHotspot> hotspots)
    {
        var hotspotsJson = string.Join("\n", hotspots.Select(h =>
            $"- ID:{h.Id} | {h.Name} | Grid:{h.GridCellId} | FWI:{h.AverageFwi:F1} | Severity:{h.RiskLevel} | Fires:{h.FireCount} | Area:{h.TotalAreaBurned}ha | PeakMonth:{h.PeakMonth}"));

        return $@"Analise os hotspots de incêndio e gere um JSON com recomendações em pt-BR.

HOTSPOTS ({hotspots.Count} total):
{hotspotsJson}

Gerar JSON com esta estrutura exata:
{{
  ""concerningHotspots"": [
    {{""id"": ""guid"", ""name"": ""nome"", ""currentFwi"": 35.5, ""riskLevel"": ""High"", ""concernReason"": ""razão""}}
  ],
  ""recentActivity"": ""descrição da atividade recente"",
  ""historicalAverage"": 25.0,
  ""recommendations"": [""rec1"", ""rec2""]
}}";
    }

    private static string BuildWeeklyReportPrompt(List<GeoEvent> thisWeek, List<GeoEvent> prevWeek,
        List<GeoEvent> historical, List<WeatherRiskDataPoint> fwiData, List<FwiForecast> forecastData, DateTime weekStart)
    {
        var thisWeekByDay = thisWeek.GroupBy(e => e.OccurredAt.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var prevWeekCount = prevWeek.Count;
        var historicalAvg = historical.Count / 5.0;

        var fwiTrend = fwiData.Count > 0 ? $"FWI médio: {fwiData.Average(f => f.FWI):F1}" : "Sem dados FWI";
        var forecastSummary = forecastData.Count > 0
            ? $"Máx previsto: {forecastData.Max(f => f.FWI):F1}"
            : "Sem previsão";

        return $@"Gere um relatório semanal completo em JSON para pt-BR.

DADOS DA SEMANA ATUAL ({weekStart:yyyy-MM-dd} a {weekStart.AddDays(6):yyyy-MM-dd}):
- Total eventos: {thisWeek.Count}
- Por dia: {string.Join(", ", thisWeekByDay.Select(d => $"{d.Key:ddd}:{d.Value.Count}"))}

COMPARATIVO:
- Semana anterior: {prevWeekCount} eventos
- Média 5 anos (semana equivalente): {historicalAvg:F0} eventos

CONDIÇÕES FWI:
- {fwiTrend}
- {forecastSummary}

Gerar JSON com esta estrutura exata:
{{
  ""weekNumber"": {ISOWeek.GetWeekOfYear(weekStart)},
  ""year"": {weekStart.Year},
  ""dayByDayBreakdown"": [
    {{""date"": ""2024-01-15"", ""dayName"": ""Segunda"", ""eventCount"": 5, ""dominantRisk"": ""High"", ""weatherSummary"": ""Sunny""}}
  ],
  ""comparisonPreviousWeek"": {{""changePercent"": 15.5, ""changeDescription"": ""aumento"", ""isIncrease"": true}},
  ""comparisonFiveYearAverage"": {{""changePercent"": -10.0, ""changeDescription"": ""abaixo da média"", ""isIncrease"": false}},
  ""weatherForecastImpact"": ""impacto descrito"",
  ""strategicRecommendations"": [""rec1"", ""rec2""],
  ""summary"": ""resumo executivo""
}}";
    }

    private static string BuildSituationReportPrompt(List<GeoEvent> activeEvents, List<WeatherRiskDataPoint> currentFwi,
        List<FireHotspot> hotspots, List<WeatherRiskDataPoint> weatherHistory)
    {
        var eventsJson = string.Join("\n", activeEvents.Take(10).Select(e =>
            $"- {e.EventType} | {e.Title} | {e.Severity} | {e.OccurredAt:HH:mm}"));

        var currentFwiAvg = currentFwi.Count > 0 ? currentFwi.Average(f => f.FWI) : 0;
        var currentRisk = currentFwiAvg switch
        {
            > 30 => "Critical",
            > 20 => "High",
            > 10 => "Medium",
            _ => "Low"
        };

        var tempTrend = DetermineTempTrend(weatherHistory);

        return $@"Gere um relatório de situação em tempo real em JSON para pt-BR.

EVENTOS ATIVOS (últimas 24h):
{eventsJson}
Total: {activeEvents.Count} eventos

FWI ATUAL:
- Valor médio: {currentFwiAvg:F1}
- Risco: {currentRisk}
- Registros: {currentFwi.Count}

HOTSPOTS ATIVOS:
{string.Join("\n", hotspots.Take(5).Select(h => $"- {h.Name} | {h.RiskLevel} | {h.FireCount} fogos históricos"))}

TENDÊNCIA METEOROLÓGICA (últimos 3 dias):
- Temperatura tendência: {tempTrend}
- Registros: {weatherHistory.Count}

Gerar JSON com esta estrutura exata:
{{
  ""activeEvents"": [
    {{""id"": ""guid"", ""title"": ""título"", ""eventType"": ""Fire"", ""severity"": ""High"", ""occurredAt"": ""2024-01-15T10:00:00Z""}}
  ],
  ""currentFwi"": {{""value"": {currentFwiAvg:F1}, ""riskLevel"": ""{currentRisk}"", ""trend"": ""{tempTrend}"", ""averageRegion"": {currentFwiAvg:F1}}},
  ""hotspots"": [
    {{""id"": ""guid"", ""name"": ""nome"", ""riskLevel"": ""High"", ""fireCount"": 15}}
  ],
  ""weatherTrend"": ""tendência"",
  ""overallRiskLevel"": ""{currentRisk}"",
  ""immediateRecommendations"": [""rec1"", ""rec2""],
  ""generatedAt"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
}}";
    }

    private async Task<DashboardResponse> GenerateFallbackRiskSummaryAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var fwiData = await db.WeatherRiskDataPoints
            .Where(w => w.Timestamp >= todayStart)
            .AsNoTracking()
            .ToListAsync(ct);

        var currentAvgFwi = fwiData.Count > 0 ? fwiData.Average(w => w.FWI) : 0;
        var riskLevel = currentAvgFwi switch
        {
            > 30 => "Critical",
            > 20 => "High",
            > 10 => "Medium",
            _ => "Low"
        };

        return new DashboardResponse(
            riskLevel,
            StableTrend,
            0.5,
            new List<KeyFactorDto>
            {
                new("FWI", currentAvgFwi.ToString("F1"), "Fire Weather Index atual"),
                new("Eventos", "0", "Eventos nas últimas 24h")
            },
            "Dados insuficientes para análise completa. Configure a API de IA para obter insights detalhados.",
            new List<string> { "Consulte fontes oficiais de meteorologia" },
            new List<string>(),
            DateTime.UtcNow);
    }

    private static EventSummaryResponse GenerateFallbackEventSummary(List<GeoEvent> events)
    {
        if (events.Count == 0)
        {
            return new EventSummaryResponse(
                new List<TopEventDto>(),
                new List<string> { "Nenhum evento registrado" },
                new SeverityDistributionDto(0, 0, 0, 0),
                new SourceBreakdownDto(0, 0, 0, 0, 0));
        }

        var topEvents = events.OrderByDescending(e => e.Severity).Take(3)
            .Select(e => new TopEventDto(
                e.Id, e.Title, e.EventType.ToString(), e.Severity.ToString(),
                e.OccurredAt, $"({e.Geometry.Y:F4},{e.Geometry.X:F4})", ""))
            .ToList();

        return new EventSummaryResponse(
            topEvents,
            new List<string> { "Análise AI não disponível" },
            new SeverityDistributionDto(
                events.Count(e => e.Severity == RiskLevel.Critical),
                events.Count(e => e.Severity == RiskLevel.High),
                events.Count(e => e.Severity == RiskLevel.Medium),
                events.Count(e => e.Severity == RiskLevel.Low)),
            new SourceBreakdownDto(
                events.Count(e => e.Source == EventSource.ICNF),
                events.Count(e => e.Source == EventSource.ANEPC),
                events.Count(e => e.Source == EventSource.IPMA),
                events.Count(e => e.Source == EventSource.Manual),
                events.Count(e => e.Source == EventSource.AI_Detected)));
    }

    private static HotspotAnalysisResponse GenerateFallbackHotspotAnalysis(List<FireHotspot> hotspots)
    {
        if (hotspots.Count == 0)
        {
            return new HotspotAnalysisResponse(
                new List<ConcerningHotspotDto>(),
                "Nenhum hotspot registrado",
                0,
                new List<string> { "Monitorize a evolução dos índices de risco" });
        }

        var concerning = hotspots.OrderByDescending(h => h.RiskLevel).Take(3)
            .Select(h => new ConcerningHotspotDto(
                h.Id, h.Name, h.AverageFwi, h.RiskLevel.ToString(), "Elevado histórico de incêndios"))
            .ToList();

        return new HotspotAnalysisResponse(
            concerning,
            "Atividade recente consistente com padrões históricos",
            hotspots.Average(h => h.AverageFwi),
            new List<string> { "Reforçar vigilância nos hotspots prioritários" });
    }

    private static string DetermineFwiTrend(double currentFwi, double historicalFwi)
    {
        if (currentFwi > historicalFwi) return "increasing";
        if (currentFwi < historicalFwi) return "decreasing";
        return StableTrend;
    }

    private static string DetermineTempTrend(List<WeatherRiskDataPoint> weatherHistory)
    {
        if (weatherHistory.Count == 0) return StableTrend;
        var lastTemp = weatherHistory[weatherHistory.Count - 1].Temperature;
        var firstTemp = weatherHistory[0].Temperature;
        if (lastTemp > firstTemp) return "increasing";
        if (lastTemp < firstTemp) return "decreasing";
        return StableTrend;
    }
}
