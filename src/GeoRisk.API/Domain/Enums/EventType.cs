using System.Text.Json.Serialization;

namespace GeoRisk.API.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventType
{
    Fire,
    Flood,
    Storm,
    Landslide,
    Industrial,
    Heatwave,
    Other
}
