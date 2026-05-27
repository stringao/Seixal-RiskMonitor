using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GeoRisk.API.Domain.Enums;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Stores FWI (Fire Weather Index) forecasts for 1-7 days ahead.
/// Calculated every 6 hours based on Open-Meteo forecast data.
/// </summary>
public class FwiForecast
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Grid point identifier (matches WeatherRiskDataPoint.GridPointId).
    /// </summary>
    [MaxLength(50)]
    public string GridPointId { get; set; } = null!;

    /// <summary>
    /// Location of this forecast point.
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// The date of the forecast (day this prediction is for).
    /// </summary>
    public DateTime ForecastDate { get; set; }

    /// <summary>
    /// Forecast horizon in days (1-7).
    /// </summary>
    [Range(1, 7)]
    public int HorizonDays { get; set; }

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
    /// When this forecast was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}