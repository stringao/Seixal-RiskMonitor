using System.Security.Cryptography;
using System.Text;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Builds context data for RAG queries from existing database records.
/// Uses text-based retrieval (no embeddings needed initially).
/// </summary>
public class RagContextBuilder
{
    private readonly GeoRiskDbContext _db;

    public RagContextBuilder(GeoRiskDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Builds context for a specific event including weather and predictions.
    /// </summary>
    public async Task<string> BuildEventContext(Guid eventId, CancellationToken ct = default)
    {
        var geoEvent = await _db.GeoEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (geoEvent == null)
            return "Evento não encontrado.";

        var predictions = await _db.FireSpreadPredictions
            .AsNoTracking()
            .Where(p => p.FireEventId == eventId)
            .ToListAsync(ct);

        var nearbyWeather = await _db.WeatherRiskDataPoints
            .AsNoTracking()
            .Where(w => w.Timestamp >= geoEvent.OccurredAt.AddHours(-2) &&
                        w.Timestamp <= geoEvent.OccurredAt.AddHours(2))
            .OrderBy(w => Math.Abs((w.Timestamp - geoEvent.OccurredAt).TotalMinutes))
            .Take(5)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine($"# Evento: {geoEvent.Title}");
        sb.AppendLine($"Tipo: {geoEvent.EventType}");
        sb.AppendLine($"Severidade: {geoEvent.Severity}");
        sb.AppendLine($"Data/Hora: {geoEvent.OccurredAt:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Localização: Lat {geoEvent.Geometry.Y:F4}, Lon {geoEvent.Geometry.X:F4}");

        if (!string.IsNullOrEmpty(geoEvent.Description))
            sb.AppendLine($"Descrição: {geoEvent.Description}");

        if (nearbyWeather.Count > 0)
        {
            sb.AppendLine("\n## Condições Meteorológicas no momento:");
            foreach (var w in nearbyWeather)
            {
                sb.AppendLine($"- {w.Timestamp:HH:mm}: Temp {w.Temperature}°C, Humidade {w.Humidity}%, Vento {w.WindSpeed} km/h, FWI {w.FWI:F1}");
            }
        }

        if (predictions.Count > 0)
        {
            sb.AppendLine("\n## Previsões de Propagação:");
            foreach (var p in predictions.OrderBy(x => x.HorizonHours))
            {
                sb.AppendLine($"- {p.HorizonHours}h: Área {p.AreaKm2:F2} km², ROS {p.RosKmh:F2} km/h, Cenário {p.Scenario}");
                if (!string.IsNullOrEmpty(p.Conclusion))
                    sb.AppendLine($"  Conclusão: {p.Conclusion}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds context for a specific hotspot.
    /// </summary>
    public async Task<string> BuildHotspotContext(Guid hotspotId, CancellationToken ct = default)
    {
        var hotspot = await _db.FireHotspots
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == hotspotId, ct);

        if (hotspot == null)
            return "Hotspot não encontrado.";

        var recentEvents = await _db.GeoEvents
            .AsNoTracking()
            .Where(e => e.EventType == EventType.Fire)
            .Where(e => e.OccurredAt >= DateTime.UtcNow.AddYears(-2))
            .OrderByDescending(e => e.OccurredAt)
            .Take(10)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine($"# Hotspot: {hotspot.Name}");
        sb.AppendLine($"Grid Cell: {hotspot.GridCellId}");
        sb.AppendLine($"Localização: Lat {hotspot.Location.Y:F4}, Lon {hotspot.Location.X:F4}");
        sb.AppendLine($"Total de incêndios: {hotspot.FireCount}");
        sb.AppendLine($"Área total ardida: {hotspot.TotalAreaBurned:F2} hectares");
        sb.AppendLine($"Severidade média: {hotspot.AverageSeverity}");
        sb.AppendLine($"Mês de pico: {hotspot.PeakMonth}");
        sb.AppendLine($"Hora de pico: {hotspot.PeakHour:D2}:00");
        sb.AppendLine($"FWI médio: {hotspot.AverageFwi:F1}");
        sb.AppendLine($"Nível de risco: {hotspot.RiskLevel}");

        if (!string.IsNullOrEmpty(hotspot.CommonWindDirection))
            sb.AppendLine($"Direção do vento mais comum: {hotspot.CommonWindDirection}");

        if (recentEvents.Count > 0)
        {
            sb.AppendLine("\n## Eventos Recentes (últimos 2 anos):");
            foreach (var e in recentEvents.Take(5))
            {
                sb.AppendLine($"- {e.OccurredAt:dd/MM/yyyy}: {e.Title} ({e.Severity})");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds context for seasonal statistics.
    /// </summary>
    public async Task<string> BuildSeasonalContext(int year, string? region, CancellationToken ct = default)
    {
        var fromDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var events = await _db.GeoEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= fromDate && e.OccurredAt <= toDate)
            .ToListAsync(ct);

        if (events.Count == 0)
            return $"Não foram encontrados dados para o ano {year}.";

        var fireEvents = events.Where(e => e.EventType == EventType.Fire).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"# Estatísticas Sazonais {year}" + (region != null ? $" - {region}" : ""));
        sb.AppendLine($"Total de eventos: {events.Count}");
        sb.AppendLine($"Incêndios: {fireEvents.Count}");

        if (fireEvents.Count > 0)
        {
            var byMonth = fireEvents.GroupBy(e => e.OccurredAt.Month)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            var avgSeverity = fireEvents.Average(e => (int)e.Severity);
            var maxSeverity = fireEvents.Max(e => e.Severity);

            sb.AppendLine($"\n## Análise de Incêndios");
            sb.AppendLine($"Severidade média: {(RiskLevel)avgSeverity:F1}");
            sb.AppendLine($"Severidade máxima: {maxSeverity}");
            sb.AppendLine($"Meses mais críticos:");

            foreach (var kvp in byMonth.OrderByDescending(x => x.Value).Take(3))
            {
                sb.AppendLine($"- Mês {kvp.Key}: {kvp.Value} incêndios");
            }

            sb.AppendLine($"\n## Distribuição por região (aproximada):");
            var byLocation = fireEvents
                .GroupBy(e => e.Geometry.X >= -9 ? "Norte" : "Sul")
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in byLocation)
            {
                sb.AppendLine($"- {kvp.Key}: {kvp.Value} incêndios");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds global context with summary statistics for all Portugal.
    /// </summary>
    public async Task<string> BuildGlobalContext(CancellationToken ct = default)
    {
        var last30Days = DateTime.UtcNow.AddDays(-30);

        var recentEvents = await GetRecentEventsAsync(last30Days, ct);
        var activeHotspots = await GetActiveHotspotsAsync(ct);
        var latestWeather = await GetLatestWeatherAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("# Situação Atual de Risco de Incêndio em Portugal");
        sb.AppendLine($"Data da análise: {DateTime.UtcNow:dd/MM/yyyy HH:mm} (UTC)");

        AppendEventSummary(sb, recentEvents, last30Days);
        AppendSeverityDistribution(sb, recentEvents);
        AppendActiveHotspots(sb, activeHotspots);
        AppendWeatherConditions(sb, latestWeather);

        return sb.ToString();
    }

    private async Task<List<GeoEvent>> GetRecentEventsAsync(DateTime since, CancellationToken ct) =>
        await _db.GeoEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= since)
            .ToListAsync(ct);

    private async Task<List<FireHotspot>> GetActiveHotspotsAsync(CancellationToken ct) =>
        await _db.FireHotspots
            .AsNoTracking()
            .Where(h => h.RiskLevel >= RiskLevel.Medium)
            .OrderByDescending(h => h.FireCount)
            .Take(10)
            .ToListAsync(ct);

    private async Task<List<WeatherRiskDataPoint>> GetLatestWeatherAsync(CancellationToken ct) =>
        await _db.WeatherRiskDataPoints
            .AsNoTracking()
            .OrderByDescending(w => w.Timestamp)
            .Take(20)
            .ToListAsync(ct);

    private void AppendEventSummary(StringBuilder sb, List<GeoEvent> recentEvents, DateTime last30Days)
    {
        sb.AppendLine($"\n## Resumo dos Últimos 30 Dias");
        sb.AppendLine($"Total de eventos: {recentEvents.Count}");
        sb.AppendLine($"Incêndios: {recentEvents.Count(e => e.EventType == EventType.Fire)}");
        sb.AppendLine($"Alertas ativos: {_db.Alerts.Count(a => !a.IsRead && a.CreatedAt >= last30Days)}");
    }

    private static void AppendSeverityDistribution(StringBuilder sb, List<GeoEvent> recentEvents)
    {
        if (recentEvents.Count == 0) return;

        var fireEvents = recentEvents.Where(e => e.EventType == EventType.Fire).ToList();
        var severityCounts = fireEvents.GroupBy(e => e.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        sb.AppendLine($"\n## Distribuição por Severidade:");
        foreach (var level in Enum.GetValues<RiskLevel>())
        {
            var count = severityCounts.GetValueOrDefault(level, 0);
            sb.AppendLine($"- {level}: {count}");
        }
    }

    private static void AppendActiveHotspots(StringBuilder sb, List<FireHotspot> activeHotspots)
    {
        if (activeHotspots.Count == 0) return;

        sb.AppendLine($"\n## Hotspots Mais Ativos ({activeHotspots.Count}):");
        foreach (var h in activeHotspots.Take(5))
        {
            sb.AppendLine($"- {h.Name}: {h.FireCount} incêndios, Risco {h.RiskLevel}, FWI médio {h.AverageFwi:F1}");
        }
    }

    private static void AppendWeatherConditions(StringBuilder sb, List<WeatherRiskDataPoint> latestWeather)
    {
        if (latestWeather.Count == 0) return;

        var avgFwi = latestWeather.Average(w => w.FWI);
        var maxFwi = latestWeather.Max(w => w.FWI);
        var avgTemp = latestWeather.Average(w => w.Temperature);
        var avgWind = latestWeather.Average(w => w.WindSpeed);

        sb.AppendLine($"\n## Condições Meteorológicas Atuais:");
        sb.AppendLine($"FWI médio: {avgFwi:F1} (máx: {maxFwi:F1})");
        sb.AppendLine($"Temperatura média: {avgTemp:F1}°C");
        sb.AppendLine($"Velocidade do vento média: {avgWind:F1} km/h");

        var riskLevel = DetermineRiskLevel(maxFwi);
        sb.AppendLine($"\nNível de risco geral: {riskLevel}");
    }

    private static RiskLevel DetermineRiskLevel(double maxFwi)
    {
        if (maxFwi >= 30) return RiskLevel.Critical;
        if (maxFwi >= 20) return RiskLevel.High;
        if (maxFwi >= 10) return RiskLevel.Medium;
        return RiskLevel.Low;
    }

    /// <summary>
    /// Builds regional context for a specific area.
    /// </summary>
    public async Task<string> BuildRegionalContext(string region, CancellationToken ct = default)
    {
        // Approximate bounding box for Setúbal region area
        var boundingBoxes = new Dictionary<string, (double MinLat, double MaxLat, double MinLon, double MaxLon)>
        {
            ["Setúbal"] = (38.3, 38.8, -9.0, -8.3),
            ["Lisboa"] = (38.5, 39.2, -9.5, -8.9),
            ["Algarve"] = (37.0, 37.5, -8.5, -7.4),
            ["Alentejo"] = (37.5, 39.0, -9.0, -7.0),
            ["Norte"] = (41.0, 42.0, -8.5, -7.5),
        };

        if (!boundingBoxes.TryGetValue(region, out var bbox))
        {
            return $"Região '{region}' não reconhecida. Regiões disponíveis: {string.Join(", ", boundingBoxes.Keys)}";
        }

        var last90Days = DateTime.UtcNow.AddDays(-90);
        var events = await _db.GeoEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= last90Days)
            .Where(e => e.Geometry.Y >= bbox.MinLat && e.Geometry.Y <= bbox.MaxLat &&
                        e.Geometry.X >= bbox.MinLon && e.Geometry.X <= bbox.MaxLon)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine($"# Contexto Regional: {region}");
        sb.AppendLine($"Total de eventos (90 dias): {events.Count}");
        sb.AppendLine($"Incêndios: {events.Count(e => e.EventType == EventType.Fire)}");

        if (events.Count > 0)
        {
            var fireEvents = events.Where(e => e.EventType == EventType.Fire).ToList();
            if (fireEvents.Count > 0)
            {
                sb.AppendLine($"\n## Estatísticas de Incêndios:");
                sb.AppendLine($"Total: {fireEvents.Count}");
                sb.AppendLine($"Severidade máxima: {fireEvents.Max(e => e.Severity)}");

                var peakMonth = fireEvents.GroupBy(e => e.OccurredAt.Month).OrderByDescending(g => g.Count()).First();
                sb.AppendLine($"Mês mais ativo: {peakMonth.Key} ({peakMonth.Count()} incêndios)");

                // Get weather data for region
                var weatherForRegion = await _db.WeatherRiskDataPoints
                    .AsNoTracking()
                    .Where(w => w.Location.Y >= bbox.MinLat && w.Location.Y <= bbox.MaxLat &&
                                w.Location.X >= bbox.MinLon && w.Location.X <= bbox.MaxLon)
                    .OrderByDescending(w => w.Timestamp)
                    .Take(10)
                    .ToListAsync(ct);

                if (weatherForRegion.Count > 0)
                {
                    sb.AppendLine($"\n## Condições Meteorológicas:");
                    sb.AppendLine($"FWI atual: {weatherForRegion[0].FWI:F1}");
                    sb.AppendLine($"Temperatura: {weatherForRegion[0].Temperature}°C");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Computes a hash for content deduplication.
    /// </summary>
    public static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }
}