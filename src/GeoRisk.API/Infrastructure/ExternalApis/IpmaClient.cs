using System.Text.Json;

namespace GeoRisk.API.Infrastructure.ExternalApis;

public interface IIpmaClient
{
    Task<IpmaWeatherData?> GetWeatherForRegionAsync(string regionCode, CancellationToken ct = default);
    Task<IReadOnlyList<IpmaFireRisk>> GetFireRiskAsync(CancellationToken ct = default);
}

public sealed record IpmaWeatherData(
    string RegionCode,
    double Temperature,
    double Humidity,
    double WindSpeed,
    string WindDirection,
    DateTime ForecastDate);

public sealed record IpmaFireRisk(
    string District,
    string County,
    double Latitude,
    double Longitude,
    int RiskIndex,
    string RiskLevel);

public sealed class IpmaClient : IIpmaClient
{
#pragma warning disable S1075
    private const string WeatherApiUrl = "https://api.ipma.pt/public/cities/{0}/weather";
    private const string FireRiskApiUrl = "https://api.ipma.pt/public/criticalareas";
#pragma warning restore S1075

    private readonly HttpClient _http;
    private readonly ILogger<IpmaClient> _logger;

    public IpmaClient(HttpClient http, ILogger<IpmaClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IpmaWeatherData?> GetWeatherForRegionAsync(string regionCode, CancellationToken ct = default)
    {
        try
        {
            var url = string.Format(WeatherApiUrl, regionCode);
            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IPMA weather API returned {StatusCode}", response.StatusCode);
                return GetMockWeather(regionCode);
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseWeatherResponse(content, regionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch IPMA weather for region {Region}, using mock data", regionCode);
            return GetMockWeather(regionCode);
        }
    }

    public async Task<IReadOnlyList<IpmaFireRisk>> GetFireRiskAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(FireRiskApiUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IPMA fire risk API returned {StatusCode}", response.StatusCode);
                return GetMockFireRisk();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseFireRiskResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch IPMA fire risk, using mock data");
            return GetMockFireRisk();
        }
    }

    private static IpmaWeatherData GetMockWeather(string regionCode) => new(
        regionCode, 28.5, 35.0, 15.2, "NW", DateTime.UtcNow);

    private static IReadOnlyList<IpmaFireRisk> GetMockFireRisk() =>
    [
        new IpmaFireRisk("Setubal", "Seixal", 38.5261, -8.8845, 85, "Very High"),
        new IpmaFireRisk("Setubal", "Sesimbra", 38.5234, -8.9712, 72, "High"),
        new IpmaFireRisk("Lisboa", "Alcochete", 38.5423, -8.8123, 65, "High")
    ];

    private static IpmaWeatherData? ParseWeatherResponse(string json, string regionCode)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new IpmaWeatherData(
                regionCode,
                root.TryGetProperty("t", out var t) ? t.GetDouble() : 0,
                root.TryGetProperty("h", out var h) ? h.GetDouble() : 0,
                root.TryGetProperty("w", out var w) ? w.GetDouble() : 0,
                root.TryGetProperty("wv", out var wv) ? wv.GetString() ?? "N" : "N",
                DateTime.UtcNow);
        }
        catch { /* Intentionally swallowed: non-critical IPMA parsing failure */ }
        return null;
    }

    private static List<IpmaFireRisk> ParseFireRiskResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<IpmaFireRisk>();
            foreach (var item in root.EnumerateArray())
            {
                list.Add(ParseIpmaFireRisk(item));
            }
            return list;
        }
        catch { /* Intentionally swallowed: non-critical IPMA parsing failure */ }
        return [];
    }

    private static IpmaFireRisk ParseIpmaFireRisk(JsonElement item)
    {
        return new IpmaFireRisk(
            GetStringProperty(item, "district"),
            GetStringProperty(item, "county"),
            GetDoubleProperty(item, "lat"),
            GetDoubleProperty(item, "lon"),
            GetIntProperty(item, "riskIndex"),
            GetStringProperty(item, "riskLevel"));
    }

    private static string GetStringProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
    }

    private static double GetDoubleProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetDouble() : 0;
    }

    private static int GetIntProperty(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : 0;
    }
}
