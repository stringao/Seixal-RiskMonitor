using System.Text.Json.Serialization;

namespace GeoRisk.API.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
