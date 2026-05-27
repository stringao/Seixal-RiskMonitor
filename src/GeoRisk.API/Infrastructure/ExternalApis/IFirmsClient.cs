namespace GeoRisk.API.Infrastructure.ExternalApis;

public interface IFirmsClient
{
    Task<IReadOnlyList<FirmsFireDetection>> GetFireDetectionsAsync(CancellationToken ct = default);
}

public sealed record FirmsFireDetection(
    double Latitude,
    double Longitude,
    double BrightnessTi4,
    double BrightnessTi5,
    double Scan,
    double Track,
    DateOnly AcqDate,
    int AcqTime,
    string Satellite,
    string Version,
    double BrightTi31,
    double Frp);
