using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

public class GeoEvent
{
    public Guid Id { get; set; }
    public EventType EventType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Point Geometry { get; set; } = null!;
    public RiskLevel Severity { get; set; }
    public EventSource Source { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? AIClassification { get; set; }

    [Column(TypeName = "jsonb")]
    public string? AIInsight { get; set; }

    public string? SourceId { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
