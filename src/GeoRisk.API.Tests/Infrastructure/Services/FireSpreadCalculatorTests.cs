using FluentAssertions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Services;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Infrastructure.Services;

public sealed class FireSpreadCalculatorTests
{
    private readonly FireSpreadCalculator _calc;
    private readonly GeometryFactory _geometryFactory;
    private readonly FwiCalculator _fwiCalc;

    public FireSpreadCalculatorTests()
    {
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        _fwiCalc = new FwiCalculator();
        _calc = new FireSpreadCalculator(_geometryFactory, _fwiCalc);
    }

    #region CalculateROS Tests

    [Fact]
    public void CalculateROS_BasicISIAndWind_ReturnsPositiveROS()
    {
        // Arrange
        var fwi = new FwiResult(15.0, 10.0, 8.0, 5.0, 10.0, 80.0);
        double windSpeedKmh = 15;

        // Act
        var ros = _calc.CalculateROS(fwi, windSpeedKmh, windDirectionDegrees: 0);

        // Assert
        ros.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateROS_HigherWindSpeed_ProducesHigherROS()
    {
        // Arrange
        var fwi = new FwiResult(15.0, 10.0, 8.0, 5.0, 10.0, 80.0);

        // Act
        var rosLowWind = _calc.CalculateROS(fwi, windSpeedKmh: 15, windDirectionDegrees: 0);
        var rosHighWind = _calc.CalculateROS(fwi, windSpeedKmh: 30, windDirectionDegrees: 0);

        // Assert
        rosHighWind.Should().BeGreaterThan(rosLowWind);
    }

    [Fact]
    public void CalculateROS_ZeroISI_ReturnsMinimumFloor()
    {
        // Arrange
        var fwi = new FwiResult(0, 0, 0, 0, 0, 0);
        double windSpeedKmh = 15;

        // Act
        var ros = _calc.CalculateROS(fwi, windSpeedKmh, windDirectionDegrees: 0);

        // Assert
        ros.Should().BeGreaterThanOrEqualTo(0.1);
    }

    #endregion

    #region BuildEllipse Tests

    [Fact]
    public void BuildEllipse_ValidInputs_ReturnsPolygonWithPositiveArea()
    {
        // Arrange
        double centerLat = 38.5;
        double centerLon = -8.9;
        double rosKmh = 5;
        int hours = 2;
        double windDirRad = 0; // North

        // Act
        var ellipse = _calc.BuildEllipse(centerLat, centerLon, rosKmh, hours, windDirRad);

        // Assert
        ellipse.Should().NotBeNull();
        ellipse.Area.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BuildEllipse_DifferentHours_ProducesDifferentSizes()
    {
        // Arrange
        double centerLat = 38.5;
        double centerLon = -8.9;
        double rosKmh = 5;
        double windDirRad = 0;

        // Act
        var ellipse1h = _calc.BuildEllipse(centerLat, centerLon, rosKmh, hours: 1, windDirRad);
        var ellipse4h = _calc.BuildEllipse(centerLat, centerLon, rosKmh, hours: 4, windDirRad);

        // Assert
        ellipse4h.Area.Should().BeGreaterThan(ellipse1h.Area);
    }

    [Fact]
    public void BuildEllipse_DifferentWindDirections_ProducesDifferentOrientations()
    {
        // Arrange
        double centerLat = 38.5;
        double centerLon = -8.9;
        double rosKmh = 5;
        int hours = 2;

        // Act
        var northEllipse = _calc.BuildEllipse(centerLat, centerLon, rosKmh, hours, windDirectionRadians: 0); // North
        var eastEllipse = _calc.BuildEllipse(centerLat, centerLon, rosKmh, hours, windDirectionRadians: Math.PI / 2); // East

        // Assert
        northEllipse.Centroid.X.Should().BeApproximately(eastEllipse.Centroid.X, 0.001);
        northEllipse.Centroid.Y.Should().BeApproximately(eastEllipse.Centroid.Y, 0.001);

        // Ellipses should have different orientations (not equal)
        northEllipse.Coordinates.Should().NotBeEquivalentTo(eastEllipse.Coordinates);
    }

    #endregion

    #region CalculateSpread Tests

    [Fact]
    public void CalculateSpread_ReturnsFivePredictions()
    {
        // Arrange
        var fireEvent = CreateTestFireEvent();
        var weather = CreateTestWeather();
        var fwi = _fwiCalc.Calculate(weather);

        // Act
        var predictions = _calc.CalculateSpread(
            fireEvent,
            latitude: 38.5,
            longitude: -8.9,
            weather,
            fwi,
            FireSpreadScenario.Moderate);

        // Assert
        predictions.Should().HaveCount(5);
        predictions.Select(p => p.HorizonHours).Should().BeEquivalentTo(new[] { 1, 2, 4, 8, 12 });
    }

    [Fact]
    public void CalculateSpread_DifferentScenarios_AffectROS()
    {
        // Arrange
        var fireEvent = CreateTestFireEvent();
        var weather = CreateTestWeather();
        var fwi = _fwiCalc.Calculate(weather);

        // Act
        var optimistPredictions = _calc.CalculateSpread(
            fireEvent, 38.5, -8.9, weather, fwi, FireSpreadScenario.Optimist);
        var moderatePredictions = _calc.CalculateSpread(
            fireEvent, 38.5, -8.9, weather, fwi, FireSpreadScenario.Moderate);
        var pessimistPredictions = _calc.CalculateSpread(
            fireEvent, 38.5, -8.9, weather, fwi, FireSpreadScenario.Pessimist);

        // Assert
        var optimistRos = optimistPredictions.First().RosKmh;
        var moderateRos = moderatePredictions.First().RosKmh;
        var pessimistRos = pessimistPredictions.First().RosKmh;

        optimistRos.Should().BeLessThan(moderateRos);
        moderateRos.Should().BeLessThan(pessimistRos);
    }

    [Fact]
    public void CalculateSpread_PolygonsHaveValidWKT()
    {
        // Arrange
        var fireEvent = CreateTestFireEvent();
        var weather = CreateTestWeather();
        var fwi = _fwiCalc.Calculate(weather);

        // Act
        var predictions = _calc.CalculateSpread(
            fireEvent,
            latitude: 38.5,
            longitude: -8.9,
            weather,
            fwi,
            FireSpreadScenario.Moderate);

        // Assert
        foreach (var prediction in predictions)
        {
            var wkt = prediction.Polygon.AsText();
            wkt.Should().StartWith("POLYGON");
            wkt.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void CalculateSpread_ConclusionIsGenerated()
    {
        // Arrange
        var fireEvent = CreateTestFireEvent();
        var weather = CreateTestWeather();
        var fwi = _fwiCalc.Calculate(weather);

        // Act
        var predictions = _calc.CalculateSpread(
            fireEvent,
            latitude: 38.5,
            longitude: -8.9,
            weather,
            fwi,
            FireSpreadScenario.Moderate);

        // Assert
        foreach (var prediction in predictions)
        {
            prediction.Conclusion.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region Helper Methods

    private static GeoEvent CreateTestFireEvent()
    {
        return new GeoEvent
        {
            Id = Guid.NewGuid(),
            EventType = EventType.Fire,
            Title = "Test fire",
            Geometry = new Point(-8.9, 38.5) { SRID = 4326 },
            Severity = RiskLevel.High
        };
    }

    private static OpenMeteoWeather CreateTestWeather()
    {
        return new OpenMeteoWeather(
            Latitude: 38.5,
            Longitude: -8.9,
            Timestamp: DateTime.UtcNow,
            TemperatureCelsius: 25.0,
            RelativeHumidityPercent: 50.0,
            WindSpeedKmh: 15.0,
            WindDirectionDegrees: 0.0,
            PrecipitationMm: 0.0);
    }

    #endregion
}