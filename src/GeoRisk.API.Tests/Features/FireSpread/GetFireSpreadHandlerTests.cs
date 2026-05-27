using FluentAssertions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.FireSpread;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Features.FireSpread;

public sealed class GetFireSpreadHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    private static GeoEvent CreateFireEvent(Guid id)
    {
        var fire = new GeoEvent
        {
            Id = id,
            EventType = EventType.Fire,
            Title = "Test fire",
            Severity = RiskLevel.High,
            Source = EventSource.Manual,
            OccurredAt = DateTime.UtcNow.AddHours(-1),
            Geometry = new Point(-8.9, 38.5) { SRID = 4326 }
        };
        return fire;
    }

    private static FireSpreadPrediction CreatePrediction(Guid fireId, int horizon, double areaKm2 = 0, double rosKmh = 0)
    {
        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        var polygon = factory.CreatePolygon(new[] {
            new Coordinate(-9.0, 38.4),
            new Coordinate(-8.8, 38.4),
            new Coordinate(-8.8, 38.6),
            new Coordinate(-9.0, 38.6),
            new Coordinate(-9.0, 38.4)
        });

        return new FireSpreadPrediction
        {
            Id = Guid.NewGuid(),
            FireEventId = fireId,
            HorizonHours = horizon,
            Polygon = polygon,
            RosKmh = rosKmh > 0 ? rosKmh : 2.5,
            AreaKm2 = areaKm2 > 0 ? areaKm2 : horizon * 1.5,
            AffectedMunicipalities = new List<string> { "Seixal" },
            Scenario = FireSpreadScenario.Moderate,
            Conclusion = $"Test conclusion for {horizon}h",
            CalculatedAt = DateTime.UtcNow,
            FWI = 68,
            ISI = 12,
            Temperature = 32,
            Humidity = 28,
            WindSpeed = 18,
            WindDirection = 315
        };
    }

    [Fact]
    public async Task Handle_FireNotFound_ReturnsEmptyResponse()
    {
        // Arrange
        var dbName = $"fire_not_found_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var fireId = Guid.NewGuid();
        var handler = new GetFireSpreadHandler(db);

        // Act
        var response = await handler.HandleAsync(new GetFireSpreadQuery(fireId), CancellationToken.None);

        // Assert
        response.FireEventId.Should().Be(fireId);
        response.Horizons.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FireExistsWithPredictions_ReturnsPrediction()
    {
        // Arrange
        var dbName = $"fire_with_preds_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var fireId = Guid.NewGuid();

        var fire = CreateFireEvent(fireId);
        db.GeoEvents.Add(fire);

        var horizons = new[] { 1, 2, 4, 8, 12 };
        foreach (var h in horizons)
        {
            db.FireSpreadPredictions.Add(CreatePrediction(fireId, h));
        }
        await db.SaveChangesAsync();

        var handler = new GetFireSpreadHandler(db);

        // Act
        var response = await handler.HandleAsync(new GetFireSpreadQuery(fireId), CancellationToken.None);

        // Assert
        response.FireEventId.Should().Be(fireId);
        response.Horizons.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_HorizonData_MappedCorrectly()
    {
        // Arrange
        var dbName = $"horizon_data_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var fireId = Guid.NewGuid();

        db.GeoEvents.Add(CreateFireEvent(fireId));
        db.FireSpreadPredictions.Add(CreatePrediction(fireId, horizon: 4, areaKm2: 12.5, rosKmh: 2.3));
        await db.SaveChangesAsync();

        var handler = new GetFireSpreadHandler(db);

        // Act
        var response = await handler.HandleAsync(new GetFireSpreadQuery(fireId), CancellationToken.None);

        // Assert
        response.Horizons.Should().HaveCount(1);
        var horizon = response.Horizons[0];
        horizon.Hours.Should().Be(4);
        horizon.AreaKm2.Should().Be(12.5);
        horizon.RosKmh.Should().Be(2.3);
    }

    [Fact]
    public async Task Handle_PolygonWkt_Preserved()
    {
        // Arrange
        var dbName = $"polygon_wkt_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var fireId = Guid.NewGuid();

        db.GeoEvents.Add(CreateFireEvent(fireId));
        db.FireSpreadPredictions.Add(CreatePrediction(fireId, horizon: 1));
        await db.SaveChangesAsync();

        var handler = new GetFireSpreadHandler(db);

        // Act
        var response = await handler.HandleAsync(new GetFireSpreadQuery(fireId), CancellationToken.None);

        // Assert
        response.Horizons[0].PolygonWkt.Should().Contain("POLYGON");
    }

    [Fact]
    public async Task Handle_AffectedMunicipalities_Listed()
    {
        // Arrange
        var dbName = $"municipalities_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var fireId = Guid.NewGuid();

        db.GeoEvents.Add(CreateFireEvent(fireId));
        var prediction = CreatePrediction(fireId, horizon: 1);
        prediction.AffectedMunicipalities = new List<string> { "Seixal", "Sesimbra" };
        db.FireSpreadPredictions.Add(prediction);
        await db.SaveChangesAsync();

        var handler = new GetFireSpreadHandler(db);

        // Act
        var response = await handler.HandleAsync(new GetFireSpreadQuery(fireId), CancellationToken.None);

        // Assert
        response.Horizons[0].AffectedMunicipalities.Should().Contain("Seixal");
        response.Horizons[0].AffectedMunicipalities.Should().Contain("Sesimbra");
    }

    [Fact]
    public async Task Handle_Scenario_ModerateByDefault()
    {
        // Arrange
        var dbName = $"scenario_moderate_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var fireId = Guid.NewGuid();

        db.GeoEvents.Add(CreateFireEvent(fireId));
        db.FireSpreadPredictions.Add(CreatePrediction(fireId, horizon: 1));
        await db.SaveChangesAsync();

        var handler = new GetFireSpreadHandler(db);

        // Act
        var response = await handler.HandleAsync(new GetFireSpreadQuery(fireId), CancellationToken.None);

        // Assert
        response.Scenario.Should().Be("Moderate");
    }

    [Fact]
    public async Task Handle_WeatherConditions_Provided()
    {
        // Arrange
        var dbName = $"weather_conditions_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var fireId = Guid.NewGuid();

        db.GeoEvents.Add(CreateFireEvent(fireId));
        db.FireSpreadPredictions.Add(CreatePrediction(fireId, horizon: 1, areaKm2: 1.5, rosKmh: 2.5));
        await db.SaveChangesAsync();

        var handler = new GetFireSpreadHandler(db);

        // Act
        var response = await handler.HandleAsync(new GetFireSpreadQuery(fireId), CancellationToken.None);

        // Assert
        response.CurrentWeather.Temperature.Should().Be(32);
        response.CurrentWeather.FWI.Should().Be(68);
    }

    [Fact]
    public async Task Handle_LastUpdated_SetFromPrediction()
    {
        // Arrange
        var dbName = $"last_updated_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var fireId = Guid.NewGuid();

        db.GeoEvents.Add(CreateFireEvent(fireId));
        var calculatedAt = new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);
        var prediction = CreatePrediction(fireId, horizon: 1);
        prediction.CalculatedAt = calculatedAt;
        db.FireSpreadPredictions.Add(prediction);
        await db.SaveChangesAsync();

        var handler = new GetFireSpreadHandler(db);

        // Act
        var response = await handler.HandleAsync(new GetFireSpreadQuery(fireId), CancellationToken.None);

        // Assert
        response.LastUpdated.Should().Be(calculatedAt);
    }
}