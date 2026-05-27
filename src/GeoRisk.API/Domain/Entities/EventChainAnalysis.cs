using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Represents an analysis of correlated fire events that form a chain pattern.
/// Chains can be simultaneous fires, sequential ignitions, resource contention situations,
/// or ember cast events where one fire drives another.
/// </summary>
public class EventChainAnalysis
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of chain analysis: "Simultaneous", "Sequential", "ResourceContention", "EmberCast"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>
    /// Main or driver event in the chain (the fire that likely caused or primarily drives the chain).
    /// </summary>
    public Guid? PrimaryEventId { get; set; }

    /// <summary>
    /// Human-readable description of the chain relationship.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Confidence score (0-1) that this chain relationship is accurate.
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Detailed findings in JSON format with evidence and analysis details.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Findings { get; set; }

    /// <summary>
    /// Contributing factors such as weather, terrain, timing, etc.
    /// </summary>
    public List<string> ContributingFactors { get; set; } = new();

    /// <summary>
    /// Recommended action to respond to this chain pattern.
    /// </summary>
    [MaxLength(500)]
    public string? RecommendedAction { get; set; }

    /// <summary>
    /// When this analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the primary event if set.
    /// </summary>
    [ForeignKey(nameof(PrimaryEventId))]
    public GeoEvent? PrimaryEvent { get; set; }
}
