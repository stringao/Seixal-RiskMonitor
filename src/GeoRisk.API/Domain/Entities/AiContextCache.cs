using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Caches AI context data for RAG queries.
/// Stores pre-processed context content with deduplication hash.
/// </summary>
public class AiContextCache
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Optional reference to a specific event this context relates to.
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Type of context stored: event_summary, hotspot, seasonal, weather, full, regional.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ContextType { get; set; } = string.Empty;

    /// <summary>
    /// The embedded/text content representing this context.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Hash of content for deduplication.
    /// </summary>
    [MaxLength(64)]
    public string? VectorHash { get; set; }

    /// <summary>
    /// When this cache entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional expiration time for cache invalidation.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    [ForeignKey(nameof(EventId))]
    public GeoEvent? Event { get; set; }
}