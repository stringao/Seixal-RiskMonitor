using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    // New fields for enhanced alert system
    public Guid? AlertRuleId { get; set; }
    [ForeignKey(nameof(AlertRuleId))]
    public AlertRule? AlertRule { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public double? FwiValue { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public double? WindSpeed { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public double? Temperature { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public double? AreaKm2 { get; set; }

    public bool IsEscalated { get; set; }

    public Guid? EscalatedFromAlertId { get; set; }
    [ForeignKey(nameof(EscalatedFromAlertId))]
    public Alert? EscalatedFromAlert { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
