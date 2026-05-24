namespace GeoRisk.API.Infrastructure.ExternalApis;

public interface IIcnfClient
{
    Task<IReadOnlyList<IcnfFireEvent>> GetActiveFiresAsync(CancellationToken ct = default);
}

public sealed record IcnfFireEvent(
    string Id,
    string Region,
    string County,
    double Latitude,
    double Longitude,
    DateTime DetectedAt,
    double? AreaHa,
    string Status);
