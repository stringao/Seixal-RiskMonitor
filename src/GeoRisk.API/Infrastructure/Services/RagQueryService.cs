using System.Text;
using System.Text.RegularExpressions;
using GeoRisk.API.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Processes natural language queries using RAG approach.
/// Selects appropriate context, builds prompt, and returns answer with sources.
/// </summary>
public class RagQueryService
{
    private readonly GeoRiskDbContext _db;
    private readonly RagContextBuilder _contextBuilder;
    private readonly ILlmProvider _llmProvider;
    private readonly ILlmSettingsService _settingsService;

    private static readonly string[] PortugueseWords = { "qual", "foi", "o", "a", "na", "do", "da", "em", "por", "com", "mais", "como", "onde", "quando", "é", "são" };
    private static readonly string[] SpanishWords = { "cual", "fue", "el", "la", "en", "por", "con", "mas", "como", "donde", "cuando", "es", "son" };
    private static readonly string[] EnglishWords = { "what", "was", "the", "in", "is", "are", "how", "where", "when", "most", "largest", "biggest" };
    private static readonly char[] WordSeparators = { ' ', ',', '.', '?', '!' };

    public RagQueryService(
        GeoRiskDbContext db,
        RagContextBuilder contextBuilder,
        ILlmProvider llmProvider,
        ILlmSettingsService settingsService)
    {
        _db = db;
        _contextBuilder = contextBuilder;
        _llmProvider = llmProvider;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Processes a query and returns an AI-generated response.
    /// </summary>
    public async Task<AiQueryResponse> QueryAsync(AiQueryRequest request, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
        {
            return new AiQueryResponse
            {
                Answer = "IA não configurada. Por favor, configure uma chave API nas Definições.",
                Sources = Array.Empty<string>(),
                Confidence = 0,
                Language = "pt"
            };
        }

        // Detect language from query
        var language = DetectLanguage(request.Question);

        // Build context based on request
        var context = await BuildContextAsync(request, ct);

        if (string.IsNullOrEmpty(context))
        {
            return new AiQueryResponse
            {
                Answer = "Não foram encontrados dados relevantes para responder à sua pergunta.",
                Sources = Array.Empty<string>(),
                Confidence = 0.3,
                Language = language
            };
        }

        // Build system prompt based on language
        var systemPrompt = BuildSystemPrompt(language);

        // Build user prompt with context
        var userPrompt = $"Com base nos seguintes dados:\n\n{context}\n\nResponde à seguinte pergunta: {request.Question}";

        try
        {
            var answer = await _llmProvider.CompleteAsync(systemPrompt, userPrompt, ct);

            // Extract sources from context used
            var sources = ExtractSources(context);

            return new AiQueryResponse
            {
                Answer = answer,
                Sources = sources,
                Confidence = CalculateConfidence(answer, context),
                Language = language
            };
        }
        catch (Exception ex)
        {
            return new AiQueryResponse
            {
                Answer = $"Erro ao processar a pergunta: {ex.Message}",
                Sources = Array.Empty<string>(),
                Confidence = 0,
                Language = language
            };
        }
    }

    /// <summary>
    /// Compares an event with similar historical events.
    /// </summary>
    public async Task<CompareEventsResponse> CompareEventAsync(Guid eventId, int topN = 5, CancellationToken ct = default)
    {
        var targetEvent = await _db.GeoEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (targetEvent == null)
        {
            return new CompareEventsResponse
            {
                TargetEventId = eventId,
                SimilarEvents = Array.Empty<SimilarEvent>(),
                Analysis = "Evento não encontrado."
            };
        }

        var similarityService = new SimilaritySearchService(_db);
        var similarEvents = await similarityService.FindSimilarEventsAsync(eventId, topN, ct: ct);

        // Build comparison prompt
        var targetContext = await _contextBuilder.BuildEventContext(eventId, ct);

        var sb = new StringBuilder();
        sb.AppendLine($"# Análise Comparativa do Evento: {targetEvent.Title}");
        sb.AppendLine($"\n## Evento Alvo:");
        sb.AppendLine(targetContext);

        sb.AppendLine("\n## Eventos Similares Encontrados:");
        foreach (var similar in similarEvents)
        {
            var similarContext = await _contextBuilder.BuildEventContext(similar.EventId, ct);
            sb.AppendLine($"\n--- Similaridade: {similar.Score:P0} ---");
            sb.AppendLine(similarContext);
        }

        var language = "pt";
        var systemPrompt = BuildSystemPrompt(language);
        var userPrompt = $"Analisa os seguintes eventos e identifica padrões e diferenças:\n\n{sb}";

        try
        {
            var analysis = await _llmProvider.CompleteAsync(systemPrompt, userPrompt, ct);

            return new CompareEventsResponse
            {
                TargetEventId = eventId,
                SimilarEvents = similarEvents,
                Analysis = analysis
            };
        }
        catch (Exception ex)
        {
            return new CompareEventsResponse
            {
                TargetEventId = eventId,
                SimilarEvents = similarEvents,
                Analysis = $"Erro ao gerar análise: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Generates a dashboard-style summary of current situation.
    /// </summary>
    public async Task<SituationSummary> GenerateSummaryAsync(CancellationToken ct = default)
    {
        var globalContext = await _contextBuilder.BuildGlobalContext(ct);

        var systemPrompt = "You are a risk analyst assistant. Generate structured summaries in Portuguese.";
        var userPrompt = $"Based on the following data, generate a structured situation summary:\n\n{globalContext}\n\nProvide a summary with: total events, risk level, active hotspots, and weather trend.";

        try
        {
            var summaryText = await _llmProvider.CompleteAsync(systemPrompt, userPrompt, ct);

            // Parse structured data from the response
            var recentEvents = await _db.GeoEvents
                .AsNoTracking()
                .Where(e => e.OccurredAt >= DateTime.UtcNow.AddDays(-30))
                .ToListAsync(ct);

            var activeHotspots = await _db.FireHotspots
                .AsNoTracking()
                .Where(h => h.RiskLevel >= RiskLevel.Medium)
                .CountAsync(ct);

            var latestWeather = await _db.WeatherRiskDataPoints
                .AsNoTracking()
                .OrderByDescending(w => w.Timestamp)
                .FirstOrDefaultAsync(ct);

            var fireEvents = recentEvents.Where(e => e.EventType == EventType.Fire).ToList();

            return new SituationSummary
            {
                TotalEvents = recentEvents.Count,
                FireEvents = fireEvents.Count,
                ActiveHotspots = activeHotspots,
                CurrentRiskLevel = latestWeather?.RiskLevel ?? RiskLevel.Low,
                WeatherTrend = DetermineTrend(latestWeather),
                FWI = latestWeather?.FWI ?? 0,
                Temperature = latestWeather?.Temperature ?? 0,
                WindSpeed = latestWeather?.WindSpeed ?? 0,
                SummaryText = summaryText
            };
        }
        catch (Exception ex)
        {
            return new SituationSummary
            {
                SummaryText = $"Erro ao gerar sumário: {ex.Message}"
            };
        }
    }

    private async Task<string> BuildContextAsync(AiQueryRequest request, CancellationToken ct)
    {
        return request.ContextType?.ToLower() switch
        {
            "event" => await _contextBuilder.BuildEventContext(Guid.Empty, ct), // Would need eventId from question
            "hotspot" => await _contextBuilder.BuildHotspotContext(Guid.Empty, ct), // Would need hotspotId
            "seasonal" => await _contextBuilder.BuildSeasonalContext(DateTime.UtcNow.Year, null, ct),
            "regional" => await ExtractRegionAndBuildContext(request.Question, ct),
            "full" or _ => await _contextBuilder.BuildGlobalContext(ct)
        };
    }

    private async Task<string> ExtractRegionAndBuildContext(string question, CancellationToken ct)
    {
        // Extract region from question using simple keyword matching
        var regions = new[] { "Setúbal", "Lisboa", "Algarve", "Alentejo", "Norte", "Sul" };
        var foundRegion = regions.FirstOrDefault(r =>
            question.Contains(r, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(foundRegion))
        {
            return await _contextBuilder.BuildRegionalContext(foundRegion, ct);
        }

        // Default to global context if no region found
        return await _contextBuilder.BuildGlobalContext(ct);
    }

    private static string DetectLanguage(string text)
    {
        // Simple language detection based on common words
        var words = text.ToLower().Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

        var ptScore = words.Count(w => PortugueseWords.Contains(w));
        var esScore = words.Count(w => SpanishWords.Contains(w));
        var enScore = words.Count(w => EnglishWords.Contains(w));

        return ptScore >= esScore && ptScore >= enScore ? "pt" : "en";
    }

    private static string BuildSystemPrompt(string language)
    {
        if (language == "pt")
        {
            return @"Eres um assistente de análise de riscos de incêndio de Portugal.
Analisa apenas os dados fornecidos no contexto.
Responde em português de forma clara e concisa.
Se não tens informação suficiente para responder, diz 'Não sei'.
Inclui referências aos dados quando relevante.
Sé breve mas completo.";
        }
        else
        {
            return @"You are a fire risk analysis assistant for Portugal.
Only analyze the data provided in the context.
Answer in English clearly and concisely.
If you don't have enough information, say 'I don't know'.
Include data references when relevant.
Be brief but complete.";
        }
    }

    private static string[] ExtractSources(string context)
    {
        var sources = new List<string>();

        // Extract event IDs from context
        var eventMatches = Regex.Matches(context, @"GeoEvent[:\-]?([a-fA-F0-9\-]{36})", RegexOptions.None, TimeSpan.FromSeconds(1));
        foreach (Match match in eventMatches)
        {
            var id = match.Groups[1].Value;
            if (!sources.Contains($"GeoEvent:{id}"))
                sources.Add($"GeoEvent:{id}");
        }

        // Extract hotspot IDs
        var hotspotMatches = Regex.Matches(context, @"Hotspot[:\-]?([a-fA-F0-9\-]{36})", RegexOptions.None, TimeSpan.FromSeconds(1));
        foreach (Match match in hotspotMatches)
        {
            var id = match.Groups[1].Value;
            if (!sources.Contains($"FireHotspot:{id}"))
                sources.Add($"FireHotspot:{id}");
        }

        return sources.ToArray();
    }

    private static double CalculateConfidence(string answer, string context)
    {
        // Simple confidence based on answer length and context availability
        if (string.IsNullOrEmpty(answer) || answer.Length < 20)
            return 0.3;

        if (string.IsNullOrEmpty(context))
            return 0.4;

        if (answer.Contains("não sei", StringComparison.OrdinalIgnoreCase) ||
            answer.Contains("i don't know", StringComparison.OrdinalIgnoreCase))
            return 0.5;

        return 0.8;
    }

    private static string DetermineTrend(WeatherRiskDataPoint? latest)
    {
        if (latest == null) return "desconhecido";

        // Simple trend based on FWI
        return DetermineFwiTrend(latest.FWI);
    }

    private static string DetermineFwiTrend(double fwi)
    {
        if (fwi >= 30) return "em alta";
        if (fwi >= 20) return "estável-alto";
        if (fwi >= 10) return "estável";
        return "em baixa";
    }
}
// DTOs
public class AiQueryRequest
{
    public string Question { get; set; } = string.Empty;
    public string? ContextType { get; set; }
    public Guid? EventId { get; set; }
    public Guid? HotspotId { get; set; }
}

public class AiQueryResponse
{
    public string Answer { get; set; } = string.Empty;
    public string[] Sources { get; set; } = Array.Empty<string>();
    public double Confidence { get; set; }
    public string Language { get; set; } = "pt";
}

public class CompareEventsResponse
{
    public Guid TargetEventId { get; set; }
    public SimilarEvent[] SimilarEvents { get; set; } = Array.Empty<SimilarEvent>();
    public string Analysis { get; set; } = string.Empty;
}

public class SimilarEvent
{
    public Guid EventId { get; set; }
    public double Score { get; set; }
    public string? Title { get; set; }
    public DateTime OccurredAt { get; set; }
    public RiskLevel Severity { get; set; }
}

public class SituationSummary
{
    public int TotalEvents { get; set; }
    public int FireEvents { get; set; }
    public int ActiveHotspots { get; set; }
    public RiskLevel CurrentRiskLevel { get; set; }
    public string WeatherTrend { get; set; } = string.Empty;
    public double FWI { get; set; }
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public string SummaryText { get; set; } = string.Empty;
}