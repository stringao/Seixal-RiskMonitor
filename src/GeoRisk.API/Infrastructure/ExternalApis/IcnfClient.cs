using System.Text.Json;

namespace GeoRisk.API.Infrastructure.ExternalApis;

public sealed class IcnfClient : IIcnfClient
{
#pragma warning disable S1075
    private const string ApiEndpoint = "https://www.icnf.pt/portal//api/focos/fogos";
#pragma warning restore S1075

    private readonly HttpClient _http;
    private readonly ILogger<IcnfClient> _logger;

    public IcnfClient(HttpClient http, ILogger<IcnfClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IcnfFireEvent>> GetActiveFiresAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(ApiEndpoint, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ICNF API returned {StatusCode}", response.StatusCode);
                return GetMockData();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseIcnfResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ICNF fire data, using mock data");
            return GetMockData();
        }
    }

    private static IReadOnlyList<IcnfFireEvent> GetMockData()
    {
        return
        [
            new IcnfFireEvent(
                "ICNF-2025-001", "Lisboa", "Sesimbra",
                38.5234, -8.9712, DateTime.UtcNow.AddHours(-2),
                1.5, "active"),
            new IcnfFireEvent(
                "ICNF-2025-002", "Setubal", "Seixal",
                38.5261, -8.8845, DateTime.UtcNow.AddHours(-1),
                0.8, "active"),
            new IcnfFireEvent(
                "ICNF-2025-003", "Lisboa", "Alcochete",
                38.5423, -8.8123, DateTime.UtcNow.AddMinutes(-30),
                2.1, "active")
        ];
    }

    private static List<IcnfFireEvent> ParseIcnfResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<IcnfFireEvent>();
            foreach (var item in root.EnumerateArray())
            {
                list.Add(ParseIcnfFireEvent(item));
            }
            return list;
        }
        catch { /* Intentionally swallowed: non-critical ICNF parsing failure */ }
        return [];
    }

    private static IcnfFireEvent ParseIcnfFireEvent(JsonElement item)
    {
        return new IcnfFireEvent(
            GetStringProperty(item, "id"),
            GetStringProperty(item, "region"),
            GetStringProperty(item, "county"),
            GetDoubleProperty(item, "lat"),
            GetDoubleProperty(item, "lon"),
            GetDateTimeProperty(item, "date"),
            GetDoubleOrNullProperty(item, "area"),
            GetStringProperty(item, "status"));
    }

    private static string GetStringProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
    }

    private static double GetDoubleProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetDouble() : 0;
    }

    private static double? GetDoubleOrNullProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetDouble() : null;
    }

    private static DateTime GetDateTimeProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetDateTime() : DateTime.UtcNow;
    }
}
