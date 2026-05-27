using System.Globalization;

namespace GeoRisk.API.Infrastructure.ExternalApis;

public sealed class FirmsClient : IFirmsClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FirmsClient> _logger;
    private readonly string _apiKey;

    // Portugal continental bounding box
    private const double MinLon = -10.5;
    private const double MinLat = 36.5;
    private const double MaxLon = -6.0;
    private const double MaxLat = 43.0;

    public FirmsClient(HttpClient http, ILogger<FirmsClient> logger, IConfiguration configuration)
    {
        _http = http;
        _logger = logger;
        _apiKey = configuration["NasaFirms:ApiKey"] ?? "";
    }

    public async Task<IReadOnlyList<FirmsFireDetection>> GetFireDetectionsAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("NASA FIRMS API key not configured, returning mock data");
            return GetMockData();
        }

        try
        {
            var bbox = $"{MinLon},{MinLat},{MaxLon},{MaxLat}";
            var url = $"https://firms.modaps.eosdis.nasa.gov/api/area/csv/{_apiKey}/VIIRS_SNPP_NRT/{bbox}/1";

            _logger.LogInformation("Fetching VIIRS fire data from NASA FIRMS for bbox: {Bbox}", bbox);

            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("NASA FIRMS API returned {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                return GetMockData();
            }

            var csv = await response.Content.ReadAsStringAsync(ct);
            return ParseCsv(csv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch NASA FIRMS fire data");
            return GetMockData();
        }
    }

    private static List<FirmsFireDetection> ParseCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return [];

        // Skip header row
        var detections = new List<FirmsFireDetection>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            try
            {
                var detection = ParseCsvLine(line);
                if (detection != null)
                    detections.Add(detection);
            }
            catch
            {
                // Skip malformed rows
            }
        }

        return detections;
    }

    private static FirmsFireDetection? ParseCsvLine(string line)
    {
        // NASA FIRMS VIIRS CSV format:
        // latitude,longitude,brightness_ti4,brightness_ti5,scan,track,acq_date,acq_time,satellite,version,bright_ti31,FRP
        var fields = line.Split(',');

        if (fields.Length < 12)
            return null;

        return new FirmsFireDetection(
            Latitude: double.Parse(fields[0], CultureInfo.InvariantCulture),
            Longitude: double.Parse(fields[1], CultureInfo.InvariantCulture),
            BrightnessTi4: double.Parse(fields[2], CultureInfo.InvariantCulture),
            BrightnessTi5: double.Parse(fields[3], CultureInfo.InvariantCulture),
            Scan: double.Parse(fields[4], CultureInfo.InvariantCulture),
            Track: double.Parse(fields[5], CultureInfo.InvariantCulture),
            AcqDate: DateOnly.Parse(fields[6], CultureInfo.InvariantCulture),
            AcqTime: int.Parse(fields[7], CultureInfo.InvariantCulture),
            Satellite: fields[8],
            Version: fields[9],
            BrightTi31: double.Parse(fields[10], CultureInfo.InvariantCulture),
            Frp: double.Parse(fields[11], CultureInfo.InvariantCulture));
    }

    private static IReadOnlyList<FirmsFireDetection> GetMockData()
    {
        // Return mock fire detections for testing
        return
        [
            new FirmsFireDetection(
                Latitude: 38.5197,
                Longitude: -8.8881,
                BrightnessTi4: 360.5,
                BrightnessTi5: 285.2,
                Scan: 0.64,
                Track: 0.64,
                AcqDate: DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6)),
                AcqTime: 1530,
                Satellite: "VIIRS_SNPP",
                Version: "2.0",
                BrightTi31: 295.0,
                Frp: 45.5),
            new FirmsFireDetection(
                Latitude: 38.7234,
                Longitude: -9.1234,
                BrightnessTi4: 342.1,
                BrightnessTi5: 271.5,
                Scan: 0.64,
                Track: 0.64,
                AcqDate: DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3)),
                AcqTime: 1030,
                Satellite: "VIIRS_SNPP",
                Version: "2.0",
                BrightTi31: 288.5,
                Frp: 32.1),
            new FirmsFireDetection(
                Latitude: 39.5234,
                Longitude: -8.4521,
                BrightnessTi4: 378.9,
                BrightnessTi5: 290.8,
                Scan: 0.64,
                Track: 0.64,
                AcqDate: DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-1)),
                AcqTime: 1430,
                Satellite: "VIIRS_SNPP",
                Version: "2.0",
                BrightTi31: 302.3,
                Frp: 67.8),
        ];
    }
}
