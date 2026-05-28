using System.Globalization;
using System.Text.Json;

namespace GeoRisk.API.Infrastructure.ExternalApis;

public sealed class OpenMeteoClient : IOpenMeteoClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenMeteoClient> _logger;
#pragma warning disable S1075
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";
#pragma warning restore S1075

    public OpenMeteoClient(HttpClient http, ILogger<OpenMeteoClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<OpenMeteoWeather?> GetWeatherAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,wind_speed_10m,wind_direction_10m,precipitation&wind_speed_unit=kmh&timezone=Europe/Lisbon";
            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo current weather API returned {StatusCode}", response.StatusCode);
                return GetMockWeather(latitude, longitude);
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseWeatherResponse(content, latitude, longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Open-Meteo weather for ({Lat}, {Lon}), using mock data", latitude, longitude);
            return GetMockWeather(latitude, longitude);
        }
    }

    public async Task<OpenMeteoForecast?> GetForecastAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            // Request 16-day forecast for FWI prediction (we use 7 days, but API provides 16)
            var url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&hourly=temperature_2m,relative_humidity_2m,wind_speed_10m,wind_direction_10m,precipitation&wind_speed_unit=kmh&forecast_days=16&timezone=Europe/Lisbon";
            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo forecast API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseForecastResponse(content, latitude, longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Open-Meteo forecast for ({Lat}, {Lon})", latitude, longitude);
            return null;
        }
    }

    private static OpenMeteoWeather? ParseWeatherResponse(string json, double lat, double lon)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var current = root.GetProperty("current");
            return new OpenMeteoWeather(
                lat, lon,
                DateTime.UtcNow,
                current.GetProperty("temperature_2m").GetDouble(),
                current.GetProperty("relative_humidity_2m").GetDouble(),
                current.GetProperty("wind_speed_10m").GetDouble(),
                current.GetProperty("wind_direction_10m").GetDouble(),
                current.GetProperty("precipitation").GetDouble());
        }
        catch
        {
            return null;
        }
    }

    private static OpenMeteoForecast? ParseForecastResponse(string json, double lat, double lon)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var hourly = root.GetProperty("hourly");
            var times = hourly.GetProperty("time");
            var temps = hourly.GetProperty("temperature_2m");
            var humids = hourly.GetProperty("relative_humidity_2m");
            var winds = hourly.GetProperty("wind_speed_10m");
            var windDirs = hourly.GetProperty("wind_direction_10m");
            var precips = hourly.GetProperty("precipitation");

            var points = new List<ForecastPoint>();
            for (int i = 0; i < times.GetArrayLength(); i++)
            {
                points.Add(new ForecastPoint(
                    DateTime.Parse(times[i].GetString()!, CultureInfo.InvariantCulture),
                    temps[i].GetDouble(),
                    humids[i].GetDouble(),
                    winds[i].GetDouble(),
                    windDirs[i].GetDouble(),
                    precips[i].GetDouble()));
            }

            return new OpenMeteoForecast(lat, lon, points);
        }
        catch
        {
            return null;
        }
    }

    private static OpenMeteoWeather GetMockWeather(double lat, double lon) => new(
        lat, lon, DateTime.UtcNow,
        28.5, 35.0, 15.2, 315.0, 0.0);

    public async Task<OpenMeteoAirQuality?> GetAirQualityAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            var url = $"https://air-quality-api.open-meteo.com/v1/air-quality" +
                $"?latitude={latitude}&longitude={longitude}" +
                $"&hourly=pm10,pm2_5,nitrogen_dioxide,ozone,sulphur_dioxide,carbon_monoxide,dust,aerosol_optical_depth,grass_pollen,olive_pollen,alder_pollen,birch_pollen,mugwort_pollen,ragweed_pollen" +
                $"&current=pm10,pm2_5,nitrogen_dioxide,ozone,sulphur_dioxide,carbon_monoxide,dust,aerosol_optical_depth,grass_pollen,olive_pollen,alder_pollen,birch_pollen,mugwort_pollen,ragweed_pollen" +
                $"&timezone=Europe/Lisbon&forecast_days=5";

            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo air quality API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseAirQualityResponse(content, latitude, longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Open-Meteo air quality for ({Lat}, {Lon})", latitude, longitude);
            return null;
        }
    }

    private static OpenMeteoAirQuality? ParseAirQualityResponse(string json, double lat, double lon)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Parse current values
            var current = root.GetProperty("current");
            var currentTime = DateTime.Parse(current.GetProperty("time").GetString()!, CultureInfo.InvariantCulture);

            var currentData = new AirQualityCurrent(
                currentTime,
                GetNullableDouble(current, "pm10"),
                GetNullableDouble(current, "pm2_5"),
                GetNullableDouble(current, "nitrogen_dioxide"),
                GetNullableDouble(current, "ozone"),
                GetNullableDouble(current, "sulphur_dioxide"),
                GetNullableDouble(current, "carbon_monoxide"),
                GetNullableDouble(current, "dust"),
                GetNullableDouble(current, "aerosol_optical_depth"),
                GetNullableDouble(current, "grass_pollen"),
                GetNullableDouble(current, "olive_pollen"),
                GetNullableDouble(current, "alder_pollen"),
                GetNullableDouble(current, "birch_pollen"),
                GetNullableDouble(current, "mugwort_pollen"),
                GetNullableDouble(current, "ragweed_pollen"));

            // Parse hourly forecast
            var hourly = root.GetProperty("hourly");
            var times = hourly.GetProperty("time");
            var hourlyList = new List<AirQualityHourly>();

            for (int i = 0; i < times.GetArrayLength(); i++)
            {
                var timestamp = DateTime.Parse(times[i].GetString()!, CultureInfo.InvariantCulture);
                hourlyList.Add(new AirQualityHourly(
                    timestamp,
                    GetNullableDouble(hourly, "pm10", i),
                    GetNullableDouble(hourly, "pm2_5", i),
                    GetNullableDouble(hourly, "nitrogen_dioxide", i),
                    GetNullableDouble(hourly, "ozone", i),
                    GetNullableDouble(hourly, "sulphur_dioxide", i),
                    GetNullableDouble(hourly, "carbon_monoxide", i),
                    GetNullableDouble(hourly, "dust", i),
                    GetNullableDouble(hourly, "aerosol_optical_depth", i),
                    GetNullableDouble(hourly, "grass_pollen", i),
                    GetNullableDouble(hourly, "olive_pollen", i),
                    GetNullableDouble(hourly, "alder_pollen", i),
                    GetNullableDouble(hourly, "birch_pollen", i),
                    GetNullableDouble(hourly, "mugwort_pollen", i),
                    GetNullableDouble(hourly, "ragweed_pollen", i)));
            }

            return new OpenMeteoAirQuality(lat, lon, currentData, hourlyList);
        }
        catch
        {
            return null;
        }
    }

    private static double? GetNullableDouble(JsonElement obj, string property, int index = -1)
    {
        try
        {
            if (index >= 0)
            {
                var arr = obj.GetProperty(property);
                if (arr.ValueKind == JsonValueKind.Null) return null;
                return arr[index].GetDouble();
            }
            else
            {
                if (obj.TryGetProperty(property, out var elem))
                {
                    if (elem.ValueKind == JsonValueKind.Null) return null;
                    return elem.GetDouble();
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}