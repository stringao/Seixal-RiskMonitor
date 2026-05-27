using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Service for fetching and processing land use data from DGT COS (Carta de Uso e Ocupação do Solo).
/// Provides fuel load and fire risk multipliers based on land use classification.
/// </summary>
public sealed class LandUseService(
    IHttpClientFactory httpClientFactory,
    GeometryFactory _geometryFactory,
    ILogger<LandUseService> logger,
    GeoRiskDbContext dbContext)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("DGT");
#pragma warning disable S1075
    private const string DgtBaseUrl = "https://ogcapi.dgterritorio.gov.pt/collections/carta_do_solo/items";
#pragma warning restore S1075

    // Setubal area bounding box
    private const double MinLat = 38.2;
    private const double MaxLat = 39.0;
    private const double MinLon = -9.5;
    private const double MaxLon = -8.2;

    // Cache duration for land use data (7 days)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions CachedJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Get land use data for a specific point.
    /// First checks cache, then fetches from DGT API if needed.
    /// </summary>
    public async Task<LandUseDataPoint?> GetLandUseAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        // Check cache first
        var cached = await dbContext.LandUseDataPoints
            .AsNoTracking()
            .Where(p => p.Location.Distance(new Point(longitude, latitude) { SRID = 4326 }) < 0.0001)
            .Where(p => p.Timestamp > DateTime.UtcNow - CacheDuration)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync(ct);

        if (cached != null)
        {
            logger.LogDebug("Returning cached land use data for ({Lat}, {Lon})", latitude, longitude);
            return cached;
        }

        // Fetch from DGT API
        try
        {
            var landUse = await FetchFromDgtApiAsync(latitude, longitude, ct);
            if (landUse == null) return null;

            // Check if this is a WUI zone
            landUse.IsWildlandUrbanInterface = await CheckWuiZoneAsync(latitude, longitude, ct);

            // Save to cache
            await dbContext.LandUseDataPoints.AddAsync(landUse, ct);
            await dbContext.SaveChangesAsync(ct);

            return landUse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch land use data for ({Lat}, {Lon})", latitude, longitude);
            return null;
        }
    }

    /// <summary>
    /// Get fuel load at a point (1-5 scale).
    /// </summary>
    public async Task<int> CalculateFuelLoad(double latitude, double longitude, CancellationToken ct = default)
    {
        var landUse = await GetLandUseAsync(latitude, longitude, ct);
        return landUse?.FuelLoad ?? 2; // Default to medium if unavailable
    }

    /// <summary>
    /// Get fire risk multiplier at a point (0.0 - 2.0).
    /// </summary>
    public async Task<double> GetFireRiskMultiplier(double latitude, double longitude, CancellationToken ct = default)
    {
        var landUse = await GetLandUseAsync(latitude, longitude, ct);
        return landUse?.FireRiskMultiplier ?? 0.5; // Default to low risk if unavailable
    }

    /// <summary>
    /// Get fuel category at a point.
    /// </summary>
    public async Task<string> GetFuelCategory(double latitude, double longitude, CancellationToken ct = default)
    {
        var landUse = await GetLandUseAsync(latitude, longitude, ct);
        return landUse?.FuelCategory ?? FuelCategory.Bare;
    }

    /// <summary>
    /// Generate a heatmap of fuel risk values across the region.
    /// Returns a GeoJSON FeatureCollection.
    /// </summary>
    public async Task<string> GetLandUseHeatmapAsync(CancellationToken ct = default)
    {
        var features = new List<Dictionary<string, object>>();
        var gridStep = 0.009; // ~1km grid

        for (var lat = MinLat; lat <= MaxLat; lat += gridStep)
        {
            for (var lon = MinLon; lon <= MaxLon; lon += gridStep)
            {
                try
                {
                    var landUse = await GetLandUseAsync(lat, lon, ct);
                    if (landUse == null) continue;

                    features.Add(new Dictionary<string, object>
                    {
                        ["type"] = "Feature",
                        ["geometry"] = new Dictionary<string, object>
                        {
                            ["type"] = "Point",
                            ["coordinates"] = new[] { lon, lat }
                        },
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["fuelLoad"] = landUse.FuelLoad,
                            ["fireRiskMultiplier"] = landUse.FireRiskMultiplier,
                            ["fuelCategory"] = landUse.FuelCategory ?? "Unknown",
                            ["cosCode"] = landUse.CosCode ?? "",
                            ["cosDescription"] = landUse.CosDescription ?? "",
                            ["isWui"] = landUse.IsWildlandUrbanInterface
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to get land use for heatmap point ({Lat}, {Lon})", lat, lon);
                }
            }
        }

        var result = new Dictionary<string, object>
        {
            ["type"] = "FeatureCollection",
            ["features"] = features
        };

        return JsonSerializer.Serialize(result, CachedJsonOptions);
    }

    /// <summary>
    /// Update all land use data for the region.
    /// </summary>
    public async Task<int> UpdateLandUseDataAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting land use data update for Setubal region");
        var pointsUpdated = 0;
        var gridStep = 0.009; // ~1km grid

        for (var lat = MinLat; lat <= MaxLat; lat += gridStep)
        {
            for (var lon = MinLon; lon <= MaxLon; lon += gridStep)
            {
                ct.ThrowIfCancellationRequested();

                if (await TryUpdatePointAsync(lat, lon, ct))
                    pointsUpdated++;

                if (pointsUpdated > 0 && pointsUpdated % 100 == 0)
                {
                    await dbContext.SaveChangesAsync(ct);
                    logger.LogDebug("Updated {Count} land use data points", pointsUpdated);
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Completed land use data update. Total points: {Count}", pointsUpdated);

        return pointsUpdated;
    }

    private async Task<bool> TryUpdatePointAsync(double lat, double lon, CancellationToken ct)
    {
        try
        {
            var existing = await dbContext.LandUseDataPoints
                .Where(p => Math.Abs(p.Location.Y - lat) < 0.0001 && Math.Abs(p.Location.X - lon) < 0.0001)
                .Where(p => p.Timestamp > DateTime.UtcNow - CacheDuration)
                .FirstOrDefaultAsync(ct);

            if (existing != null) return false;

            var landUse = await FetchFromDgtApiAsync(lat, lon, ct);
            if (landUse == null) return false;

            landUse.IsWildlandUrbanInterface = await CheckWuiZoneAsync(lat, lon, ct);
            landUse.GridPointId = $"{lat:F4}_{lon:F4}";

            await dbContext.LandUseDataPoints.AddAsync(landUse, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update land use for ({Lat}, {Lon})", lat, lon);
            return false;
        }
    }

    private async Task<LandUseDataPoint?> FetchFromDgtApiAsync(double latitude, double longitude, CancellationToken ct)
    {
        try
        {
            // Query DGT OGC API for land use at point
            var bbox = $"{longitude - 0.001},{latitude - 0.001},{longitude + 0.001},{latitude + 0.001}";
            var url = $"{DgtBaseUrl}?bbox={bbox}&limit=10&crs=EPSG:4326";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("DGT API returned {StatusCode} for ({Lat}, {Lon})",
                    response.StatusCode, latitude, longitude);
                return CreateDefaultLandUse(latitude, longitude);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (!json.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
            {
                // No land use data available - likely urban/water, return minimal data
                return CreateDefaultLandUse(latitude, longitude);
            }

            var feature = features[0];
            if (!feature.TryGetProperty("properties", out var props))
            {
                return CreateDefaultLandUse(latitude, longitude);
            }

            var cosCode = props.TryGetProperty("cos", out var cos) ? cos.GetString() : null;
            var cosDesc = props.TryGetProperty("cos_descritivo", out var desc) ? desc.GetString() : null;

            // Parse COS code to determine fuel properties
            var fuelData = ParseCosCode(cosCode);

            return new LandUseDataPoint
            {
                Id = Guid.NewGuid(),
                Location = new Point(longitude, latitude) { SRID = 4326 },
                Timestamp = DateTime.UtcNow,
                CosCode = cosCode,
                CosDescription = cosDesc,
                FuelCategory = fuelData.Category,
                FuelLoad = fuelData.FuelLoad,
                FireRiskMultiplier = fuelData.RiskMultiplier,
                DominantSpecies = fuelData.DominantSpecies,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error fetching from DGT API for ({Lat}, {Lon})", latitude, longitude);
            return CreateDefaultLandUse(latitude, longitude);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching from DGT API for ({Lat}, {Lon})", latitude, longitude);
            return CreateDefaultLandUse(latitude, longitude);
        }
    }

    /// <summary>
    /// Parse COS code to extract fuel properties.
    /// </summary>
    private static (string Category, int FuelLoad, double RiskMultiplier, string? DominantSpecies) ParseCosCode(
        string? cosCode)
    {
        if (string.IsNullOrEmpty(cosCode))
            return (FuelCategory.Bare, 0, 0.0, null);

        // Parse the first digit of COS code
        var firstDigit = cosCode.Split('.')[0];

        return firstDigit switch
        {
            // 1 - Urban/constructed: no fuel
            "1" => (FuelCategory.Urban, 0, 0.0, null),

            // 2 - Agriculture: medium fuel load
            "2" => cosCode switch
            {
                // 2.1 - Annual crops: low fuel
                "2.1" or string when cosCode.StartsWith("2.1") =>
                    (FuelCategory.Agriculture, 1, 0.3, null),
                // 2.2 - Permanent crops (olival): higher fuel
                "2.2" or string when cosCode.StartsWith("2.2") =>
                    (FuelCategory.Agriculture, 3, 0.5, "Olive"),
                // 2.3 - Pastures: medium fuel
                "2.3" or string when cosCode.StartsWith("2.3") =>
                    (FuelCategory.Agriculture, 2, 0.4, null),
                _ => (FuelCategory.Agriculture, 2, 0.5, null)
            },

            // 3 - Forest: high fuel load
            "3" => cosCode switch
            {
                // 3.1 - Eucalyptus: highest risk
                "3.1" or string when cosCode.StartsWith("3.1") =>
                    (FuelCategory.Forest, 5, 2.0, "Eucalyptus"),
                // 3.2 - Maritime Pine
                "3.2" or string when cosCode.StartsWith("3.2") =>
                    (FuelCategory.Forest, 4, 1.5, "Maritime Pine"),
                // 3.3 - Oak/Cork
                "3.3" or string when cosCode.StartsWith("3.3") =>
                    (FuelCategory.Forest, 3, 1.0, "Cork Oak"),
                // Other forest types
                _ => (FuelCategory.Forest, 4, 1.5, "Mixed Forest")
            },

            // 4 - Shrubland/Herbaceous: highest risk!
            "4" => (FuelCategory.Shrubland, 5, 2.0, null),

            // 5 - Open spaces/Bare: no fuel
            "5" => (FuelCategory.Bare, 0, 0.0, null),

            // 6 - Water: no fuel
            "6" => (FuelCategory.Water, 0, 0.0, null),

            _ => (FuelCategory.Bare, 0, 0.0, null)
        };
    }

    private static LandUseDataPoint CreateDefaultLandUse(double latitude, double longitude)
    {
        return new LandUseDataPoint
        {
            Id = Guid.NewGuid(),
            Location = new Point(longitude, latitude) { SRID = 4326 },
            Timestamp = DateTime.UtcNow,
            CosCode = null,
            CosDescription = "Unknown",
            FuelCategory = FuelCategory.Bare,
            FuelLoad = 0,
            FireRiskMultiplier = 0.0,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Check if a point is in a Wildland-Urban Interface zone.
    /// Simplified check: if nearby area has urban land use.
    /// </summary>
    private async Task<bool> CheckWuiZoneAsync(double latitude, double longitude, CancellationToken ct)
    {
        // Check a 500m radius for urban areas
        var searchRadius = 0.005; // ~500m in degrees

        var nearbyUrban = await dbContext.LandUseDataPoints
            .AsNoTracking()
            .Where(p => p.Location.Distance(new Point(longitude, latitude) { SRID = 4326 }) < searchRadius)
            .Where(p => p.FuelCategory == FuelCategory.Urban)
            .AnyAsync(ct);

        return nearbyUrban;
    }

    /// <summary>
    /// Get the spread rate multiplier based on fuel category.
    /// Used by FireSpreadCalculator to adjust ROS.
    /// </summary>
    public static double GetSpreadRateMultiplier(string fuelCategory)
    {
        return fuelCategory switch
        {
            FuelCategory.Shrubland => 2.0,  // Highest spread rate
            FuelCategory.Forest => 1.5,      // High spread rate
            FuelCategory.Agriculture => 0.5,  // Medium spread rate
            FuelCategory.Urban => 0.0,        // Fire stops
            FuelCategory.Water => 0.0,        // No spread
            FuelCategory.Bare => 0.0,         // No fuel
            _ => 1.0                          // Default
        };
    }
}