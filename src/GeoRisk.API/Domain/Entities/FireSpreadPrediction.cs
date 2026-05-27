using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GeoRisk.API.Domain.Enums;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Stores fire spread predictions for each active fire event.
/// Calculated for multiple time horizons (1h, 2h, 4h, 8h, 12h).
/// Updated every 30 minutes by the WeatherRiskUpdateJob.
/// </summary>
public class FireSpreadPrediction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the fire event.
    /// </summary>
    public Guid FireEventId { get; set; }

    [ForeignKey(nameof(FireEventId))]
    public GeoEvent? FireEvent { get; set; }

    /// <summary>
    /// Time horizon of this prediction (1, 2, 4, 8, or 12 hours).
    /// </summary>
    public int HorizonHours { get; set; }

    /// <summary>
    /// Polygon representing the predicted fire spread area.
    /// </summary>
    public Polygon Polygon { get; set; } = null!;

    /// <summary>
    /// Rate of Spread in km/h.
    /// </summary>
    public double RosKmh { get; set; }

    /// <summary>
    /// Predicted affected area in km².
    /// </summary>
    public double AreaKm2 { get; set; }

    /// <summary>
    /// List of municipality names affected.
    /// </summary>
    public List<string> AffectedMunicipalities { get; set; } = new();

    /// <summary>
    /// Scenario for this prediction.
    /// </summary>
    public FireSpreadScenario Scenario { get; set; } = FireSpreadScenario.Moderate;

    /// <summary>
    /// Auto-generated conclusion based on spread analysis.
    /// </summary>
    public string Conclusion { get; set; } = string.Empty;

    /// <summary>
    /// When this prediction was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// FWI value at time of calculation.
    /// </summary>
    public double FWI { get; set; }

    /// <summary>
    /// ISI value at time of calculation.
    /// </summary>
    public double ISI { get; set; }

    /// <summary>
    /// Temperature at time of calculation.
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Humidity at time of calculation.
    /// </summary>
    public double Humidity { get; set; }

    /// <summary>
    /// Wind speed at time of calculation.
    /// </summary>
    public double WindSpeed { get; set; }

    /// <summary>
    /// Wind direction at time of calculation.
    /// </summary>
    public double WindDirection { get; set; }
}

public enum FireSpreadScenario
{
    Optimist,
    Moderate,
    Pessimist
}