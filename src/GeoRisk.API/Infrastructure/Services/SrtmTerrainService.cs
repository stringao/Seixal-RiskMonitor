using System.Diagnostics;
using System.Text.Json;
using GeoRisk.API.Domain.Entities;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Service for reading pre-computed terrain data from SRTM processing pipeline.
/// Terrain data is pre-computed by scripts/process_srtm_terrain.py and stored as GeoTIFFs.
/// </summary>
public sealed class SrtmTerrainService
{
    private readonly GeometryFactory _geometryFactory;
    private readonly ILogger<SrtmTerrainService> _logger;
    private readonly string _terrainRiskFile;
    private readonly string _slopeFile;
    private readonly string _aspectFile;
    private readonly string _solarExposureFile;

    // Constants for sampling strategies (balances detail vs performance)
    private const string PythonCommand = "python3";
    private const double NoDataValue = -32768.0;
    private const int HeatmapSampleStep = 10;
    private const int SlopeVisualizationSampleStep = 20;
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(60);

    public SrtmTerrainService(
        GeometryFactory geometryFactory,
        ILogger<SrtmTerrainService> logger)
    {
        _geometryFactory = geometryFactory;
        _logger = logger;

        var processedDir = Path.Combine(AppContext.BaseDirectory, "srtm_processed");

        // Check environment variable override (for testing/custom deployments)
        var envPath = Environment.GetEnvironmentVariable("GEORISK_SRTM_DIR");
        if (!string.IsNullOrEmpty(envPath))
            processedDir = envPath;

        _terrainRiskFile = Path.Combine(processedDir, "terrain_risk_1km.tif");
        _slopeFile = Path.Combine(processedDir, "slope.tif");
        _aspectFile = Path.Combine(processedDir, "aspect.tif");
        _solarExposureFile = Path.Combine(processedDir, "solar_exposure.tif");
    }

    /// <summary>
    /// Check if terrain data is available.
    /// </summary>
    public bool IsAvailable => File.Exists(_terrainRiskFile);

    /// <summary>
    /// Get terrain analysis for a specific lat/lon point.
    /// </summary>
    public async Task<TerrainAnalysis?> GetTerrainAnalysisAsync(double latitude, double longitude, string? gridPointId = null)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Terrain data not available. Run scripts/process_srtm_terrain.py first");
            return null;
        }

        try
        {
            // Run Python script to extract values at point
            var result = await RunPythonExtractionAsync(latitude, longitude);
            if (result == null)
                return null;

            var slope = result.GetValueOrDefault("slope", 0);
            var aspect = result.GetValueOrDefault("aspect", -1);
            var risk = result.GetValueOrDefault("risk", 0);
            var solar = result.GetValueOrDefault("solar_exposure", 0.7);
            var elevation = result.GetValueOrDefault("elevation", 0);

            // Determine terrain complexity from slope variance proxy (using slope directly)
            var complexity = slope switch
            {
                < 10 => "Low",
                < 25 => "Medium",
                _ => "High"
            };

            // North-facing percentage (aspect 315-45 degrees)
            var northFacing = (aspect >= 315 || aspect < 45) ? 1.0 : 0.0;

            // Terrain fire risk contribution
            var fireRisk = ClassifyTerrainFireRisk(slope, aspect, elevation);

            return new TerrainAnalysis
            {
                Id = Guid.NewGuid(),
                GridPointId = gridPointId ?? $"{latitude:F4}_{longitude:F4}",
                Location = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude)),
                ElevationMeters = elevation,
                SlopeDegrees = slope,
                AspectDegrees = aspect,
                SolarExposureIndex = solar,
                TerrainComplexity = complexity,
                NorthFacingPercent = northFacing,
                TerrainRiskScore = risk,
                TerrainFireRiskContribution = fireRisk.ToString(),
                CalculatedAt = DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get terrain analysis for {Lat}, {Lon}", latitude, longitude);
            return null;
        }
    }

    /// <summary>
    /// Get terrain risk score for a specific point.
    /// </summary>
    public async Task<double> GetTerrainRiskScoreAsync(double latitude, double longitude)
    {
        var analysis = await GetTerrainAnalysisAsync(latitude, longitude);
        return analysis?.TerrainRiskScore ?? 50; // Default moderate risk if unavailable
    }

    /// <summary>
    /// Get terrain risk heatmap data as GeoJSON for the Setubal region.
    /// Returns a grid of points with terrain risk scores.
    /// </summary>
    public async Task<string> GetTerrainRiskHeatmapAsync()
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Terrain data not available");
            return "{}";
        }

        try
        {
            var result = await RunPythonHeatmapAsync();
            return result ?? "{}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate terrain risk heatmap");
            return "{}";
        }
    }

    /// <summary>
    /// Get combined terrain + weather risk score.
    /// </summary>
    public static double CalculateCombinedRisk(double terrainRisk, double fwi)
    {
        // Terrain contributes 30%, weather (FWI) contributes 70%
        // High terrain risk amplifies weather risk
        var terrainFactor = 1.0 + (terrainRisk / 100) * 0.3;
        var combined = fwi * terrainFactor * 0.7 + terrainRisk * 0.3;

        return Math.Min(100, Math.Max(0, combined));
    }

    /// <summary>
    /// Adjust fire spread rate based on terrain (slope).
    /// Fire spreads faster uphill.
    /// </summary>
    public static double AdjustSpreadRateForTerrain(double baseRosKmh, double slopeDegrees, double aspectDegrees, double windDirectionDegrees)
    {
        // Slope effect: fire goes faster uphill
        // 10 degree slope ≈ 10% increase in ROS
        // 20 degree slope ≈ 25% increase
        // 30+ degree slope ≈ 40% increase (capped)
        var slopeFactor = slopeDegrees switch
        {
            < 5 => 1.0,
            < 15 => 1.0 + (slopeDegrees - 5) * 0.01,
            < 30 => 1.1 + (slopeDegrees - 15) * 0.015,
            _ => 1.0 + 0.4  // Cap at 40% increase
        };

        // Aspect effect: south-facing (135-225) = drier = faster spread in Portugal
        // North-facing (315-45) = slower spread due to moisture
        var aspectFactor = aspectDegrees switch
        {
            < 0 => 1.0, // Flat
            >= 315 or < 45 => 0.85,  // North - moisture retention
            >= 45 and < 135 => 1.0,  // East
            >= 135 and < 225 => 1.15, // South - maximum solar
            _ => 1.0  // West
        };

        return baseRosKmh * slopeFactor * aspectFactor;
    }

    private static TerrainFireRiskLevel ClassifyTerrainFireRisk(double slope, double aspect, double elevation)
    {
        // Extreme: steep (30+) AND south-facing AND high elevation
        if (slope >= 30 && (aspect >= 135 && aspect < 225) && elevation > 400)
            return TerrainFireRiskLevel.Extreme;

        // High: steep OR south-facing + some elevation
        if (slope >= 25 || (slope >= 15 && (aspect >= 135 && aspect < 225) && elevation > 300))
            return TerrainFireRiskLevel.High;

        // Medium: moderate slope or unfavorable aspect
        if (slope >= 10 || (aspect >= 135 && aspect < 225))
            return TerrainFireRiskLevel.Medium;

        return TerrainFireRiskLevel.Low;
    }

    private async Task<Dictionary<string, double>?> RunPythonExtractionAsync(double lat, double lon)
    {
        var script = $@"
import rasterio
from rasterio.transform import rowcol

files = {{
    'slope': r'{_slopeFile}',
    'aspect': r'{_aspectFile}',
    'risk': r'{_terrainRiskFile}',
    'solar_exposure': r'{_solarExposureFile}'
}}

# Use Portugal SRTM for elevation
with rasterio.open(r'{Path.Combine(Path.GetDirectoryName(_terrainRiskFile)!, "..", "SRTM_Portugal", "Portugal_SRTM_30m.tif").Replace("\\", "\\\\")}') as src:
    try:
        # Get elevation at point
        col, row = src.index({lon}, {lat})
        if 0 <= row < src.height and 0 <= col < src.width:
            elevation = src.read(1)[0, 0]
        else:
            elevation = 0
    except:
        elevation = 0

result = {{'elevation': elevation}}

for name, path in files.items():
    try:
        with rasterio.open(path) as src:
            col, row = src.index({lon}, {lat})
            if 0 <= row < src.height and 0 <= col < src.width:
                val = src.read(1)[0, 0]
result[name] = val if val != {NoDataValue} else 0
            else:
                result[name] = 0
    except Exception as e:
        result[name] = 0

print(str(result))
";
        return await RunPythonDictAsync(script);
    }

    private async Task<string> RunPythonHeatmapAsync()
    {
        var script = $@"
import rasterio
import json

path = r'{_terrainRiskFile}'
output = {{'type': 'FeatureCollection', 'features': []}}

try:
    with rasterio.open(path) as src:
        data = src.read(1)
        for i in range(0, src.height, {HeatmapSampleStep}):  # Sample every Nth pixel
            for j in range(0, src.width, {HeatmapSampleStep}):
                val = data[i, j]
                if val != {NoDataValue}:
                    x, y = src.xy(i, j)
                    output['features'].append({{
                        'type': 'Feature',
                        'geometry': {{'type': 'Point', 'coordinates': [x, y]}},
                        'properties': {{'risk': round(float(val), 1)}}
                    }})
    print(json.dumps(output))
except Exception as e:
    print('{{}}')
";
        return await RunPythonScriptAsync(script);
    }

    /// <summary>
    /// Get slope data as GeoJSON for visualization.
    /// Returns points colored by slope category for overlay on map.
    /// </summary>
    public async Task<string> GetSlopeVisualizationAsync()
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Terrain data not available");
            return "{}";
        }

        try
        {
            var script = $@"
import rasterio
import json

path = r'{_slopeFile}'
features = []

try:
    with rasterio.open(path) as src:
        data = src.read(1)
        for i in range(0, src.height, {SlopeVisualizationSampleStep}):
            for j in range(0, src.width, {SlopeVisualizationSampleStep}):
                val = data[i, j]
                if val != {NoDataValue} and val > 0:
                    x, y = src.xy(i, j)
                    slope = min(max(float(val), 0), 90)
                    features.append({{
                        'type': 'Feature',
                        'geometry': {{'type': 'Point', 'coordinates': [round(x, 4), round(y, 4)]}},
                        'properties': {{
                            'slope': round(slope, 1),
                            'category': 'flat' if slope < 10 else 'moderate' if slope < 25 else 'steep' if slope < 40 else 'very_steep'
                        }}
                    }})
    print(json.dumps({{'type': 'FeatureCollection', 'features': features}}))
except Exception as e:
    print('{{}}')
";
            return await RunPythonScriptAsync(script);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate slope visualization for {SlopeFile}", _slopeFile);
            return "{}";
        }
    }

    private async Task<string> RunPythonScriptAsync(string script)
    {
        // Create temp script file with guaranteed unique name (GetRandomFileName doesn't create the file)
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) + ".py";

        try
        {
            await File.WriteAllTextAsync(tempFile, script);

            // SECURITY: PythonCommand is a controlled constant ("python3").
            // The script content is constructed from controlled data (embedded constants and numeric values).
            // No user input is passed directly to the command line.
            // NOTE: UseShellExecute=false means shell operators (>, 2>&1) are NOT interpreted.
            // We use RedirectStandardOutput/RedirectStandardError to capture output directly.
            var psi = new ProcessStartInfo
            {
                FileName = PythonCommand,
                Arguments = $"\"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogError("Failed to start Python process for script execution");
                return "{}";
            }

            // Read stdout and stderr concurrently to avoid deadlocks when buffers fill up
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(ProcessTimeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Python script timed out after {Timeout}s, killing process", ProcessTimeout.TotalSeconds);
                process.Kill(true);
                return "{}";
            }

            var output = await stdoutTask;
            var stderr = await stderrTask;

            if (!string.IsNullOrEmpty(stderr))
            {
                _logger.LogWarning("Python stderr: {Stderr}", stderr);
            }

            if (!string.IsNullOrEmpty(output) && output.Contains('{'))
            {
                var start = output.IndexOf('{');
                var end = output.LastIndexOf('}') + 1;
                if (start >= 0 && end > start)
                {
                    return output.Substring(start, end - start);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Python script execution failed");
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch { /* Best effort cleanup */ }
        }

        return "{}";
    }

    private async Task<Dictionary<string, double>?> RunPythonDictAsync(string script)
    {
        var result = await RunPythonScriptAsync(script);
        if (result == "{}") return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(result);
        }
        catch
        {
            return null;
        }
    }
}