using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GeoRisk.API.Domain.Enums;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Status of a fire report analysis.
/// </summary>
public enum ReportStatus
{
    InProgress,
    Completed,
    Failed
}

/// <summary>
/// Represents a post-incident analysis report for a fire event.
/// Generated automatically when a fire event ends or manually requested.
/// </summary>
public class FireReport
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the originating GeoEvent.
    /// </summary>
    public Guid GeoEventId { get; set; }

    [ForeignKey(nameof(GeoEventId))]
    public GeoEvent? GeoEvent { get; set; }

    /// <summary>
    /// Current status of the report generation.
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.InProgress;

    /// <summary>
    /// When the analysis started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the analysis completed (null if not yet completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// How the report was generated: AI or Manual.
    /// </summary>
    public string GeneratedBy { get; set; } = "AI";

    /// <summary>
    /// Executive summary of the fire event.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Probable cause of the fire: natural, accidental, intentional, unknown.
    /// </summary>
    public string? ProbableCause { get; set; }

    /// <summary>
    /// Confidence score for the cause analysis (0-1).
    /// </summary>
    public double? CauseConfidence { get; set; }

    /// <summary>
    /// When the fire reached its peak size.
    /// </summary>
    public DateTime? PeakFireTime { get; set; }

    /// <summary>
    /// Peak fire area in hectares.
    /// </summary>
    public double? PeakFireAreaHa { get; set; }

    /// <summary>
    /// Total area burned in hectares.
    /// </summary>
    public double TotalAreaHa { get; set; }

    /// <summary>
    /// List of affected municipality names.
    /// </summary>
    public List<string> AffectedAreas { get; set; } = new();

    /// <summary>
    /// Number of people evacuated.
    /// </summary>
    public int? EvacuationCount { get; set; }

    /// <summary>
    /// Number of structures destroyed.
    /// </summary>
    public int? StructuresDestroyed { get; set; }

    /// <summary>
    /// Number of firefighters deployed.
    /// </summary>
    public int? FirefightersDeployed { get; set; }

    /// <summary>
    /// Fire duration in hours.
    /// </summary>
    public double? DurationHours { get; set; }

    /// <summary>
    /// FWI conditions during the fire.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? FwiConditionsJson { get; set; }

    /// <summary>
    /// Weather conditions during the fire.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? WeatherConditionsJson { get; set; }

    /// <summary>
    /// Chronological timeline of fire events.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? TimelineJson { get; set; }

    /// <summary>
    /// Full AI-generated report in markdown format.
    /// </summary>
    public string? GeneratedContent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// FWI conditions during a fire event.
/// </summary>
public class FwiCondition
{
    /// <summary>
    /// Initial FWI at fire start.
    /// </summary>
    public double StartFwi { get; set; }

    /// <summary>
    /// Peak FWI during fire.
    /// </summary>
    public double PeakFwi { get; set; }

    /// <summary>
    /// Average FWI during fire.
    /// </summary>
    public double AverageFwi { get; set; }

    /// <summary>
    /// Dominant wind direction during fire.
    /// </summary>
    public string? WindDirection { get; set; }
}

/// <summary>
/// Weather conditions during a fire event.
/// </summary>
public class WeatherCondition
{
    /// <summary>
    /// Average temperature in Celsius.
    /// </summary>
    public double AvgTemp { get; set; }

    /// <summary>
    /// Maximum temperature in Celsius.
    /// </summary>
    public double MaxTemp { get; set; }

    /// <summary>
    /// Minimum humidity percentage.
    /// </summary>
    public double MinHumidity { get; set; }

    /// <summary>
    /// Total precipitation in mm.
    /// </summary>
    public double TotalPrecipitation { get; set; }

    /// <summary>
    /// Dominant wind direction.
    /// </summary>
    public string? DominantWindDir { get; set; }
}

/// <summary>
/// A single entry in the fire evolution timeline.
/// </summary>
public class TimelineEntry
{
    /// <summary>
    /// Time of this event.
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// Type of event (e.g., Ignition, Spread, Peak, Contained, Extinguished).
    /// </summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// Description of what happened.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Area affected at this time in hectares.
    /// </summary>
    public double? AreaHa { get; set; }

    /// <summary>
    /// FWI value at this time.
    /// </summary>
    public double? Fwi { get; set; }
}
