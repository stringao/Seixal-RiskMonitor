namespace GeoRisk.API.Infrastructure.ExternalApis;

public interface IOpenMeteoClient
{
    Task<OpenMeteoWeather?> GetWeatherAsync(double latitude, double longitude, CancellationToken ct = default);
    Task<OpenMeteoForecast?> GetForecastAsync(double latitude, double longitude, CancellationToken ct = default);
    Task<OpenMeteoAirQuality?> GetAirQualityAsync(double latitude, double longitude, CancellationToken ct = default);
}

public sealed record OpenMeteoWeather(
    double Latitude,
    double Longitude,
    DateTime Timestamp,
    double TemperatureCelsius,
    double RelativeHumidityPercent,
    double WindSpeedKmh,
    double WindDirectionDegrees,
    double PrecipitationMm);

public sealed record OpenMeteoForecast(double Latitude, double Longitude, List<ForecastPoint> Points);

public sealed record ForecastPoint(
    DateTime Timestamp,
    double Temperature,
    double Humidity,
    double WindSpeed,
    double WindDirection,
    double Precipitation);

// ─── Air Quality Records ─────────────────────────────────────────────

public sealed record OpenMeteoAirQuality(
    double Latitude,
    double Longitude,
    AirQualityCurrent Current,
    List<AirQualityHourly> HourlyForecast);

public sealed record AirQualityCurrent(
    DateTime Timestamp,
    double? Pm10,
    double? Pm25,
    double? NitrogenDioxide,
    double? Ozone,
    double? SulphurDioxide,
    double? CarbonMonoxide,
    double? Dust,
    double? AerosolOpticalDepth,
    double? GrassPollen,
    double? OlivePollen,
    double? AlderPollen,
    double? BirchPollen,
    double? MugwortPollen,
    double? RagweedPollen);

public sealed record AirQualityHourly(
    DateTime Timestamp,
    double? Pm10,
    double? Pm25,
    double? NitrogenDioxide,
    double? Ozone,
    double? SulphurDioxide,
    double? CarbonMonoxide,
    double? Dust,
    double? AerosolOpticalDepth,
    double? GrassPollen,
    double? OlivePollen,
    double? AlderPollen,
    double? BirchPollen,
    double? MugwortPollen,
    double? RagweedPollen);