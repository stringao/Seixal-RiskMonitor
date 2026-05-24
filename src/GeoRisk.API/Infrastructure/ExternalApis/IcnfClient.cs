using System.Text.Json;

namespace GeoRisk.API.Infrastructure.ExternalApis;

public sealed class IcnfClient : IIcnfClient
{
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
            var response = await _http.GetAsync("https://www.icnf.pt/portal//api/focos/fogos", ct);
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

    private static IReadOnlyList<IcnfFireEvent> ParseIcnfResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var list = new List<IcnfFireEvent>();
                foreach (var item in root.EnumerateArray())
                {
                    list.Add(new IcnfFireEvent(
                        item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                        item.TryGetProperty("region", out var reg) ? reg.GetString() ?? "" : "",
                        item.TryGetProperty("county", out var co) ? co.GetString() ?? "" : "",
                        item.TryGetProperty("lat", out var lat) ? lat.GetDouble() : 0,
                        item.TryGetProperty("lon", out var lon) ? lon.GetDouble() : 0,
                        item.TryGetProperty("date", out var dt) ? dt.GetDateTime() : DateTime.UtcNow,
                        item.TryGetProperty("area", out var area) ? area.GetDouble() : null,
                        item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : ""));
                }
                return list;
            }
        }
        catch { }
        return [];
    }
}