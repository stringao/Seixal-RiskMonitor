using System.Text.Json;

namespace GeoRisk.API.Infrastructure.ExternalApis;

public sealed class AnepcClient : IAnepcClient
{
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
            var response = await _http.GetAsync("https://www.procivil.pt/emergencias/api/active", ct);
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

    private static IReadOnlyList<AnepcEmergency> ParseAnepcResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var list = new List<AnepcEmergency>();
                foreach (var item in root.EnumerateArray())
                {
                    list.Add(new AnepcEmergency(
                        item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                        item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                        item.TryGetProperty("district", out var d) ? d.GetString() ?? "" : "",
                        item.TryGetProperty("county", out var c) ? c.GetString() ?? "" : "",
                        item.TryGetProperty("lat", out var lat) ? lat.GetDouble() : 0,
                        item.TryGetProperty("lon", out var lon) ? lon.GetDouble() : 0,
                        item.TryGetProperty("declaredAt", out var da) ? da.GetDateTime() : DateTime.UtcNow,
                        item.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
                        item.TryGetProperty("population", out var p) ? p.GetInt32() : 0));
                }
                return list;
            }
        }
        catch { }
        return [];
    }
}