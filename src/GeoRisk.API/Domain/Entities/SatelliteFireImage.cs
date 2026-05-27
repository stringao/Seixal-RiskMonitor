using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

public class SatelliteFireImage
{
    public Guid Id { get; set; }

    public Guid? LinkedEventId { get; set; }

    [ForeignKey(nameof(LinkedEventId))]
    public GeoEvent? LinkedEvent { get; set; }

    public DateTime CaptureTime { get; set; }

    public string Source { get; set; } = "NASA_FIRMS";

    public string ImageUrl { get; set; } = string.Empty;

    public string FullResolutionUrl { get; set; } = string.Empty;

    public Polygon? Footprint { get; set; }

    public double? FireRadiativePower { get; set; }

    public double? BrightnessTemperature { get; set; }

    public bool IsActive { get; set; }

    public string? DetectionConfidence { get; set; }

    public DateTime CreatedAt { get; set; }
}
