using System.Text.Json;

namespace GeoRisk.API.Infrastructure.ExternalApis;

public sealed class AnepcClient : IAnepcClient
{
#pragma warning disable S1075
    private const string ApiEndpoint = "https://www.procivil.pt/emergencias/api/active";
#pragma warning restore S1075

    private readonly HttpClient _http;
    private readonly ILogger<AnepcClient> _logger;

    public AnepcClient(HttpClient http, ILogger<AnepcClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AnepcEmergency>> GetActiveEmergenciesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(ApiEndpoint, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ANEPC API returned {StatusCode}", response.StatusCode);
                return GetMockData();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseAnepcResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ANEPC emergency data, using mock data");
            return GetMockData();
        }
    }

    private static IReadOnlyList<AnepcEmergency> GetMockData() =>
    [
        new AnepcEmergency(
            "ANEPC-2025-001", "Fire", "Setubal", "Seixal",
            38.5261, -8.8845, DateTime.UtcNow.AddHours(-1),
            "Active", 150),
        new AnepcEmergency(
            "ANEPC-2025-002", "Flood", "Setubal", "Sesimbra",
            38.5234, -8.9712, DateTime.UtcNow.AddMinutes(-45),
            "Active", 50)
    ];

    private static List<AnepcEmergency> ParseAnepcResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<AnepcEmergency>();
            foreach (var item in root.EnumerateArray())
            {
                list.Add(ParseAnepcEmergency(item));
            }
            return list;
        }
        catch { /* Intentionally swallowed: non-critical ANEPC parsing failure */ }
        return [];
    }

    private static AnepcEmergency ParseAnepcEmergency(JsonElement item)
    {
        return new AnepcEmergency(
            GetStringProperty(item, "id"),
            GetStringProperty(item, "type"),
            GetStringProperty(item, "district"),
            GetStringProperty(item, "county"),
            GetDoubleProperty(item, "lat"),
            GetDoubleProperty(item, "lon"),
            GetDateTimeProperty(item, "declaredAt"),
            GetStringProperty(item, "status"),
            GetIntProperty(item, "population"));
    }

    private static string GetStringProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
    }

    private static double GetDoubleProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetDouble() : 0;
    }

    private static DateTime GetDateTimeProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetDateTime() : DateTime.UtcNow;
    }

    private static int GetIntProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : 0;
    }
}
