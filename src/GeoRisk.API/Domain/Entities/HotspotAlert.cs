using System.ComponentModel.DataAnnotations;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Alert triggered when a fire hotspot shows unusual activity or exceeds thresholds.
/// </summary>
public class HotspotAlert
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The hotspot that triggered this alert.
    /// </summary>
    public Guid FireHotspotId { get; set; }

    /// <summary>
    /// Navigation property to the associated fire hotspot.
    /// </summary>
    public FireHotspot? FireHotspot { get; set; }

    /// <summary>
    /// The geo event that triggered this alert (if applicable).
    /// </summary>
    public Guid? GeoEventId { get; set; }

    /// <summary>
    /// Navigation property to the triggering geo event.
    /// </summary>
    public GeoEvent? GeoEvent { get; set; }

    /// <summary>
    /// Type of alert: "NewHotspot", "RecurrenceAboveThreshold", "UnusualActivity", "SeasonalPattern".
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AlertType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable alert message.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When this alert was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the alert has been read by a user.
    /// </summary>
    public bool IsRead { get; set; }
}