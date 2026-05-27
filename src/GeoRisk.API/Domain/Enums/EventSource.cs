using System.Text.Json.Serialization;

namespace GeoRisk.API.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventSource
{
    ICNF,
    IPMA,
    ANEPC,
    Manual,
    AI_Detected
}
