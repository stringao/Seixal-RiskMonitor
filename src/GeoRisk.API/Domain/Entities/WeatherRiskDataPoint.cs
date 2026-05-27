using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GeoRisk.API.Domain.Enums;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Stores historical weather risk data points for analysis and prediction.
/// Calculated every 30 minutes from Open-Meteo + FWI system.
/// </summary>
public class WeatherRiskDataPoint
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Location of this data point (grid point in Setubal area).
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// When this measurement/calculation was made.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Temperature in Celsius.
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Relative humidity in percent.
    /// </summary>
    public double Humidity { get; set; }

    /// <summary>
    /// Wind speed in km/h.
    /// </summary>
    public double WindSpeed { get; set; }

    /// <summary>
    /// Wind direction in degrees (0-360).
    /// </summary>
    public double WindDirection { get; set; }

    /// <summary>
    /// Precipitation in mm.
    /// </summary>
    public double Precipitation { get; set; }

    // ─── FWI System Values ───────────────────────────────────────

    /// <summary>
    /// Fine Fuel Moisture Code (0-101).
    /// </summary>
    public double FFMC { get; set; }

    /// <summary>
    /// Duff Moisture Code (0+).
    /// </summary>
    public double DMC { get; set; }

    /// <summary>
    /// Drought Code (0+).
    /// </summary>
    public double DC { get; set; }

    /// <summary>
    /// Initial Spread Index (0+).
    /// </summary>
    public double ISI { get; set; }

    /// <summary>
    /// Build Up Index (0+).
    /// </summary>
    public double BUI { get; set; }

    /// <summary>
    /// Fire Weather Index (0+) - main risk indicator.
    /// </summary>
    public double FWI { get; set; }

    /// <summary>
    /// Computed risk level from FWI.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Auto-generated conclusion based on conditions.
    /// </summary>
    public string? Conclusion { get; set; }

    /// <summary>
    /// Municipality name if within a known municipality.
    /// </summary>
    public string? Municipality { get; set; }

    /// <summary>
    /// Grid point identifier (for deduplication).
    /// </summary>
    public string? GridPointId { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
}