namespace GeoRisk.API.Domain.Entities;

public class Alert
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? GeoEventId { get; set; }
    public GeoEvent? GeoEvent { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
