using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Stores aggregated seasonal fire statistics for analysis and trends.
/// Calculated monthly and annually for Portugal, Setubal region, or individual municipalities.
/// </summary>
public class SeasonalStatistics
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Year for these statistics.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month (1-12), or 0 for annual summary.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Region: "Portugal", "Setubal", or specific municipality name.
    /// </summary>
    public string Region { get; set; } = "Portugal";

    /// <summary>
    /// Total number of fires in this period/region.
    /// </summary>
    public int TotalFires { get; set; }

    /// <summary>
    /// Total area burned in hectares.
    /// </summary>
    public double TotalAreaHa { get; set; }

    /// <summary>
    /// Area of the largest fire in hectares.
    /// </summary>
    public double LargestFireHa { get; set; }

    /// <summary>
    /// Average FWI value during the period.
    /// </summary>
    public double AverageFwi { get; set; }

    /// <summary>
    /// Average temperature in Celsius.
    /// </summary>
    public double AverageTemperature { get; set; }

    /// <summary>
    /// Total precipitation in mm.
    /// </summary>
    public double TotalPrecipitationMm { get; set; }

    /// <summary>
    /// Date of the peak fire day.
    /// </summary>
    public DateTime? PeakFireDay { get; set; }

    /// <summary>
    /// JSON object with fire cause breakdown: {"natural": 5, "accidental": 12, "intentional": 3, "unknown": 1}.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? FireCauseBreakdown { get; set; }

    /// <summary>
    /// JSON array of daily fire counts: [0, 2, 1, 0, 3, ...] for each day of the month/year.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? DailyFireCounts { get; set; }

    /// <summary>
    /// When these statistics were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
