using System.Text.Json.Serialization;

namespace GeoRisk.API.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlertSeverity
{
    Info,
    Warning,
    Danger,
    Critical
}
