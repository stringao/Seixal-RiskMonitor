namespace GeoRisk.API.Infrastructure.AI;

public static class LlmPromptBuilder
{
    public static string ClassifyIncident(GeoEvent evt)
    {
        var json = string.Join("", new[]
        {
            "{",
            $"  \"classification\": \"brief classification label\",",
            "  \"confidence\": 0.0-1.0,",
            "  \"reasoning\": \"2-3 sentence explanation\",",
            "  \"relatedRisks\": [\"risk1\", \"risk2\"]",
            "}"
        });

        return string.Join("\n", new[]
        {
            "You are a risk analysis expert for municipal geographic monitoring systems.",
            "",
            "Analyze this incident and provide a classification with confidence score:",
            "",
            $"Event Type: {evt.EventType}",
            $"Title: {evt.Title}",
            $"Description: {evt.Description ?? "No description"}",
            $"Severity: {evt.Severity}",
            $"Location: ({evt.Geometry.Y}, {evt.Geometry.X})",
            $"Occurred At: {evt.OccurredAt:yyyy-MM-dd HH:mm}",
            "",
            "Return a JSON object with this structure:",
            json
        });
    }

    public static string GenerateReport(IEnumerable<GeoEvent> events, DateTime from, DateTime to) =>
        string.Join("\n", new[]
        {
            "You are a municipal risk analyst generating executive reports.",
            "",
            $"Analyze the following events from {from:yyyy-MM-dd} to {to:yyyy-MM-dd} and generate a comprehensive markdown report.",
            "",
            "Events:",
            string.Join("\n", events.Select(e => $"- {e.EventType}: {e.Title} ({e.Severity}) at {e.OccurredAt:yyyy-MM-dd HH:mm}")),
            "",
            "Generate a markdown report with sections:",
            "1. Executive Summary",
            "2. Event Distribution by Type",
            "3. Severity Analysis",
            "4. Geographic Hotspots",
            "5. Risk Trends",
            "6. Recommendations"
        });

    public static string DetectPatterns(IEnumerable<GeoEvent> events)
    {
        var jsonArray = string.Join("", new[]
        {
            "[",
            "  {",
            "    \"patternType\": \"temporal|spatial|sequential|severity\",",
            "    \"title\": \"pattern title\",",
            "    \"description\": \"detailed description\",",
            "    \"confidence\": 0.0-1.0,",
            "    \"affectedEventIds\": [\"guid1\", \"guid2\"],",
            "    \"recommendation\": \"recommended action\"",
            "  }",
            "]"
        });

        return string.Join("\n", new[]
        {
            "You are a pattern recognition specialist for geographic risk analysis.",
            "",
            "Analyze these events from the last 7 days and identify patterns:",
            "",
            string.Join("\n", events.Select(e => $"- [{e.OccurredAt:yyyy-MM-dd}] {e.EventType}: {e.Title} ({e.Severity}) at ({e.Geometry.Y}, {e.Geometry.X})")),
            "",
            "Return a JSON array of detected patterns:",
            jsonArray
        });
    }
}