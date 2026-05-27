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

    // New fields for enhanced rule conditions
    public double? MinFwi { get; set; }
    public double? MaxFwi { get; set; }
    public double? MinWindSpeed { get; set; }
    public double? MinTemperature { get; set; }
    public int? SeasonStartMonth { get; set; }
    public int? SeasonEndMonth { get; set; }
    public double? AreaKm2Threshold { get; set; }
    public int? ConsecutiveCount { get; set; }
    public int? EscalationMinutes { get; set; }
    public List<string>? NotifyRoles { get; set; }
}
