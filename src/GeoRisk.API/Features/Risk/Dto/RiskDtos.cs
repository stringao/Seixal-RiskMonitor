namespace GeoRisk.API.Features.Risk.Dto;

public sealed record RiskZoneResponse(
    Guid Id,
    string Name,
    string Wkt,
    RiskLevel RiskLevel,
    double Score,
    DateTime CalculatedAt);

public sealed record RiskDashboardResponse(
    int TotalZones,
    int CriticalZones,
    int HighRiskZones,
    List<RiskZoneResponse> Zones);

public sealed record CalculateRiskRequest(Guid? ZoneId);

public sealed record CalculateRiskResponse(
    List<RiskZoneResponse> Zones,
    DateTime CalculatedAt);
