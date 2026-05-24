using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

public class RiskZone
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Polygon Geometry { get; set; } = null!;
    public RiskLevel RiskLevel { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string? Source { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
}
