using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Stores terrain analysis derived from SRTM DEM data.
/// Linked to weather grid points for combined risk calculation.
/// </summary>
public class TerrainAnalysis
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Grid point identifier (links to WeatherRiskDataPoint.GridPointId).
    /// </summary>
    [MaxLength(50)]
    public string GridPointId { get; set; } = string.Empty;

    /// <summary>
    /// Location of this terrain analysis point.
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// Elevation in meters from SRTM DEM.
    /// </summary>
    public double ElevationMeters { get; set; }

    /// <summary>
    /// Slope angle in degrees (0-90).
    /// Calculated as maximum gradient in any direction.
    /// </summary>
    public double SlopeDegrees { get; set; }

    /// <summary>
    /// Aspect/direction of steepest descent (0-360 degrees).
    /// 0=North, 90=East, 180=South, 270=West.
    /// -1 indicates flat terrain.
    /// </summary>
    public double AspectDegrees { get; set; }

    /// <summary>
    /// Estimated solar exposure index (0-1 scale).
    /// Based on aspect + latitude. South-facing = higher exposure.
    /// </summary>
    public double SolarExposureIndex { get; set; }

    /// <summary>
    /// Terrain complexity classification derived from slope variance.
    /// </summary>
    [MaxLength(20)]
    public string TerrainComplexity { get; set; } = "Low";

    /// <summary>
    /// Proportion of north-facing area within the analysis buffer (0-1).
    /// North-facing slopes retain more moisture, lower fire risk.
    /// </summary>
    public double NorthFacingPercent { get; set; }

    /// <summary>
    /// Terrain risk score (0-100) based on slope, aspect, elevation.
    /// Combined with weather FWI for final fire risk assessment.
    /// </summary>
    public double TerrainRiskScore { get; set; }

    /// <summary>
    /// Fire risk contribution from terrain alone.
    /// </summary>
    [MaxLength(20)]
    public string TerrainFireRiskContribution { get; set; } = "Low";

    /// <summary>
    /// When this analysis was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Grid latitude for this point.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Grid longitude for this point.
    /// </summary>
    public double Longitude { get; set; }
}

/// <summary>
/// Terrain complexity levels derived from slope variance.
/// </summary>
public enum TerrainComplexityLevel
{
    Low,      // variance < 5
    Medium,   // variance 5-15
    High      // variance > 15
}

/// <summary>
/// Fire risk contribution from terrain factors.
/// </summary>
public enum TerrainFireRiskLevel
{
    Low,      // Gentle slope, favorable aspect
    Medium,   // Moderate slope or unfavorable aspect
    High,     // Steep slope and unfavorable aspect
    Extreme   // Very steep, south-facing, high elevation
}