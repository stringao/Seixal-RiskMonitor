using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GeoRisk.API.Domain.Entities;

namespace GeoRisk.API.Domain.Entities;

public class ResourceDispatch
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public GeoEvent? Event { get; set; }

    public Guid ResourceId { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public FireResource? Resource { get; set; }

    public DateTime DispatchedAt { get; set; }

    public DateTime? ArrivedAt { get; set; }

    public double? DistanceKm { get; set; }

    public double? TravelTimeMinutes { get; set; }

    public string RoutePolyline { get; set; } = string.Empty; // encoded polyline

    [MaxLength(50)]
    public string Status { get; set; } = "Dispatched"; // "Dispatched", "EnRoute", "OnScene", "Returning", "Completed"
}