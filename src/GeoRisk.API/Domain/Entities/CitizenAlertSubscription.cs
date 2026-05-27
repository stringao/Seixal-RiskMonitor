using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

public class CitizenAlertSubscription
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Point Location { get; set; } = null!;
    public double RadiusKm { get; set; } = 10.0;
    public List<EventType> EventTypes { get; set; } = new();
    public RiskLevel SeverityThreshold { get; set; } = RiskLevel.Medium;
    public bool IsActive { get; set; } = true;
    public DateTime? ConfirmedAt { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime CreatedAt { get; set; }
}