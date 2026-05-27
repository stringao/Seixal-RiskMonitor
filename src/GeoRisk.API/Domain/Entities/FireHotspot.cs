using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GeoRisk.API.Domain.Enums;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Represents a historical fire hotspot identified through spatial-temporal clustering.
/// Hotspots are 1km x 1km grid cells with ≥3 historical fire events.
/// </summary>
public class FireHotspot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable name for the hotspot (e.g., "Serra da Arrábida North", "Campo Maior East")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Centroid location of the hotspot cell in WGS84 (SRID 4326).
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// 1km grid cell identifier following pattern "N{lat}E{wLon}" or "N{lat}W{absLon}".
    /// For example: "N38W009" represents cell at lat 38-39, lon -9 to -8.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string GridCellId { get; set; } = string.Empty;

    /// <summary>
    /// Total count of fire events historically recorded in this hotspot zone.
    /// </summary>
    public int FireCount { get; set; }

    /// <summary>
    /// Sum of all burned areas (in hectares) for fires in this hotspot.
    /// </summary>
    public double TotalAreaBurned { get; set; }

    /// <summary>
    /// Average severity level of fires in this hotspot.
    /// </summary>
    public RiskLevel AverageSeverity { get; set; }

    /// <summary>
    /// Most common month (1-12) for fire occurrence in this hotspot.
    /// </summary>
    public int PeakMonth { get; set; }

    /// <summary>
    /// Most common hour (0-23) for fire occurrence in this hotspot.
    /// </summary>
    public int PeakHour { get; set; }

    /// <summary>
    /// Most frequent wind direction at time of fires (e.g., "NW", "N", "NE").
    /// </summary>
    [MaxLength(10)]
    public string? CommonWindDirection { get; set; }

    /// <summary>
    /// Average Fire Weather Index when fires started in this hotspot.
    /// </summary>
    public double AverageFwi { get; set; }

    /// <summary>
    /// Calculated risk level based on frequency, area burned, and severity.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Bounding box geometry for the hotspot cell (1km x 1km polygon).
    /// </summary>
    public Polygon? CellGeometry { get; set; }

    /// <summary>
    /// Timestamp when this hotspot was last analyzed/updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Collection of alerts triggered for this hotspot.
    /// </summary>
    public ICollection<HotspotAlert> Alerts { get; set; } = new List<HotspotAlert>();
}