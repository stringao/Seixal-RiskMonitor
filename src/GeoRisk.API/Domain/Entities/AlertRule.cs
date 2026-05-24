using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

public class AlertRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EventType? EventType { get; set; }
    public RiskLevel? SeverityThreshold { get; set; }
    public Polygon? Area { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
