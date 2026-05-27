using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GeoRisk.API.Domain.Entities;

namespace GeoRisk.API.Domain.Entities;

public class FireResource
{
    public Guid Id { get; set; }

    public Guid FireStationId { get; set; }

    [ForeignKey(nameof(FireStationId))]
    public FireStation? FireStation { get; set; }

    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty; // "Tanker", "Pump", "Helicopter", "Team", "CommandUnit"

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "Tanker 23", "Helicopter 7"

    public int PersonnelCount { get; set; }

    public double WaterCapacityLiters { get; set; }

    public bool IsAvailable { get; set; } = true;

    public Guid? DeployedToEventId { get; set; }

    [ForeignKey(nameof(DeployedToEventId))]
    public GeoEvent? DeployedToEvent { get; set; }

    public DateTime? DeployedAt { get; set; }

    public DateTime? ExpectedReturnAt { get; set; } // estimated

    [MaxLength(50)]
    public string Status { get; set; } = "Available"; // "Available", "Dispatched", "EnRoute", "OnScene", "Returning", "Maintenance"

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}