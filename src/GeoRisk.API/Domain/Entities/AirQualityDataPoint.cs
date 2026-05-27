using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Stores air quality and pollen data points for environmental monitoring.
/// Data is aggregated daily from Open-Meteo Air Quality API.
/// </summary>
public class AirQualityDataPoint
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Location of this data point (grid point in Setubal area).
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// When this measurement was made (aggregated daily timestamp).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ─── Air Quality - Pollutants (µg/m³ unless noted) ───────────────

    /// <summary>
    /// Particulate matter 10µm (µg/m³).
    /// </summary>
    public double? Pm10 { get; set; }

    /// <summary>
    /// Particulate matter 2.5µm (µg/m³).
    /// </summary>
    public double? Pm25 { get; set; }

    /// <summary>
    /// Nitrogen dioxide (µg/m³).
    /// </summary>
    public double? NitrogenDioxide { get; set; }

    /// <summary>
    /// Ozone (µg/m³).
    /// </summary>
    public double? Ozone { get; set; }

    /// <summary>
    /// Sulphur dioxide (µg/m³).
    /// </summary>
    public double? SulphurDioxide { get; set; }

    /// <summary>
    /// Carbon monoxide (µg/m³).
    /// </summary>
    public double? CarbonMonoxide { get; set; }

    /// <summary>
    /// Dust particles (µg/m³).
    /// </summary>
    public double? Dust { get; set; }

    /// <summary>
    /// Aerosol optical depth (unitless, 0-1 scale).
    /// </summary>
    public double? AerosolOpticalDepth { get; set; }

    // ─── Air Quality Index ────────────────────────────────────────────

    /// <summary>
    /// Air Quality Index value (1-5 based on European standards).
    /// 1=VeryGood, 2=Good, 3=Medium, 4=Poor, 5=Bad
    /// </summary>
    public int? AqiValue { get; set; }

    /// <summary>
    /// Air Quality category text.
    /// </summary>
    public string? AqiCategory { get; set; }

    /// <summary>
    /// Dominant pollutant determining the AQI (e.g., "PM10", "O3", "NO2").
    /// </summary>
    public string? DominantPollutant { get; set; }

    // ─── Pollen - grains/m³ ───────────────────────────────────────────

    /// <summary>
    /// Grass pollen concentration (grains/m³).
    /// </summary>
    public double? GrassPollen { get; set; }

    /// <summary>
    /// Olive pollen concentration (grains/m³).
    /// </summary>
    public double? OlivePollen { get; set; }

    /// <summary>
    /// Alder pollen concentration (grains/m³).
    /// </summary>
    public double? AlderPollen { get; set; }

    /// <summary>
    /// Birch pollen concentration (grains/m³).
    /// </summary>
    public double? BirchPollen { get; set; }

    /// <summary>
    /// Mugwort pollen concentration (grains/m³).
    /// </summary>
    public double? MugwortPollen { get; set; }

    /// <summary>
    /// Ragweed pollen concentration (grains/m³).
    /// </summary>
    public double? RagweedPollen { get; set; }

    // ─── Pollen Index ─────────────────────────────────────────────────

    /// <summary>
    /// Pollen index value (1-5 scale).
    /// 1=None, 2=Low, 3=Moderate, 4=High, 5=Very High
    /// </summary>
    public int? PollenIndex { get; set; }

    /// <summary>
    /// Pollen category text.
    /// </summary>
    public string? PollenCategory { get; set; }

    /// <summary>
    /// Dominant pollen type.
    /// </summary>
    public string? DominantPollen { get; set; }

    // ─── Health Risk ──────────────────────────────────────────────────

    /// <summary>
    /// Computed health risk level (combined air quality + pollen).
    /// "Low", "Moderate", "High", "Very High"
    /// </summary>
    public string? HealthRiskLevel { get; set; }

    // ─── Metadata ────────────────────────────────────────────────────

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Grid point identifier (for deduplication and grid system).
    /// </summary>
    public string? GridPointId { get; set; }

    /// <summary>
    /// Municipality name if within a known municipality.
    /// </summary>
    public string? Municipality { get; set; }
}