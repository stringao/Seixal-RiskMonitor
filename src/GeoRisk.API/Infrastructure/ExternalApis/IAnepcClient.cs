namespace GeoRisk.API.Infrastructure.ExternalApis;

public interface IAnepcClient
{
    Task<IReadOnlyList<AnepcEmergency>> GetActiveEmergenciesAsync(CancellationToken ct = default);
}

public sealed record AnepcEmergency(
    string Id,
    string Type,
    string District,
    string County,
    double Latitude,
    double Longitude,
    DateTime DeclaredAt,
    string Status,
    int? AffectedPopulation);
