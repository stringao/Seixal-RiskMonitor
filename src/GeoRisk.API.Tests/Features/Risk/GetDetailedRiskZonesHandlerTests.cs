using FluentAssertions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.Risk;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Features.Risk;

public sealed class GetDetailedRiskZonesHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new GeoRiskDbContext(options);
    }

    private static WeatherRiskDataPoint CreateDataPoint(
        double lon,
        double lat,
        RiskLevel riskLevel,
        double fwi,
        DateTime? timestamp = null,
        string? conclusion = null)
    {
        return new WeatherRiskDataPoint
        {
            Id = Guid.NewGuid(),
            Location = new Point(lon, lat) { SRID = 4326 },
            Timestamp = timestamp ?? DateTime.UtcNow,
            Temperature = 32,
            Humidity = 28,
            WindSpeed = 18,
            WindDirection = 315,
            FWI = fwi,
            ISI = 12,
            BUI = 45,
            RiskLevel = riskLevel,
            Conclusion = conclusion
        };
    }

    [Fact]
    public async Task Handle_NoDataPoints_ReturnsEmptyZones()
    {
        // Arrange
        var dbName = $"empty_zones_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var handler = new GetDetailedRiskZonesHandler(db);
        var query = new GetDetailedRiskZonesQuery(null);

        // Act
        var response = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        response.Zones.Should().BeEmpty();
        response.TotalPoints.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithDataPoints_ReturnsZones()
    {
        // Arrange
        var dbName = $"with_points_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var now = DateTime.UtcNow;

        var points = new List<WeatherRiskDataPoint>
        {
            CreateDataPoint(-9.10, 38.64, RiskLevel.Critical, 75, now.AddMinutes(-5)),
            CreateDataPoint(-9.11, 38.65, RiskLevel.High, 55, now.AddMinutes(-10)),
            CreateDataPoint(-9.12, 38.66, RiskLevel.Medium, 35, now.AddMinutes(-15))
        };
        db.WeatherRiskDataPoints.AddRange(points);
        await db.SaveChangesAsync();

        var handler = new GetDetailedRiskZonesHandler(db);
        var query = new GetDetailedRiskZonesQuery(null);

        // Act
        var response = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        response.Zones.Should().NotBeEmpty();
        response.TotalPoints.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithHighFWI_CriticalZone()
    {
        // Arrange
        var dbName = $"critical_zone_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var now = DateTime.UtcNow;

        var point = CreateDataPoint(-9.10, 38.64, RiskLevel.Critical, 75, now.AddMinutes(-5), "Critical fire risk");
        db.WeatherRiskDataPoints.Add(point);
        await db.SaveChangesAsync();

        var handler = new GetDetailedRiskZonesHandler(db);
        var query = new GetDetailedRiskZonesQuery(null);

        // Act
        var response = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        response.Zones.Should().Contain(z => z.RiskLevelName == "Critical");
    }

    [Fact]
    public async Task Handle_MinRiskLevelFilter_FiltersCorrectly()
    {
        // Arrange
        var dbName = $"min_level_filter_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var now = DateTime.UtcNow;

        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.10, 38.64, RiskLevel.Low, 10, now.AddMinutes(-5)));
        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.11, 38.65, RiskLevel.Critical, 80, now.AddMinutes(-10)));
        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.12, 38.66, RiskLevel.High, 55, now.AddMinutes(-15)));
        await db.SaveChangesAsync();

        var handler = new GetDetailedRiskZonesHandler(db);
        var query = new GetDetailedRiskZonesQuery("High");

        // Act
        var response = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        response.Zones.Should().OnlyContain(z => z.RiskLevelName == "High" || z.RiskLevelName == "Critical");
        response.Zones.Should().NotContain(z => z.RiskLevelName == "Low");
        response.Zones.Should().NotContain(z => z.RiskLevelName == "Medium");
    }

    [Fact]
    public async Task Handle_MinRiskLevelNull_ReturnsAll()
    {
        // Arrange
        var dbName = $"null_min_level_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var now = DateTime.UtcNow;

        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.10, 38.64, RiskLevel.Low, 10, now.AddMinutes(-5)));
        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.11, 38.65, RiskLevel.Medium, 25, now.AddMinutes(-10)));
        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.12, 38.66, RiskLevel.High, 50, now.AddMinutes(-15)));
        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.13, 38.67, RiskLevel.Critical, 75, now.AddMinutes(-20)));
        await db.SaveChangesAsync();

        var handler = new GetDetailedRiskZonesHandler(db);
        var query = new GetDetailedRiskZonesQuery(null);

        // Act
        var response = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        response.Zones.Should().HaveCount(4);
        response.TotalPoints.Should().Be(4);
    }

    [Fact]
    public async Task Handle_LatestTimestamp_ReflectedInResponse()
    {
        // Arrange
        var dbName = $"latest_timestamp_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var now = DateTime.UtcNow;
        var latestTime = now.AddMinutes(-2);

        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.10, 38.64, RiskLevel.High, 55, now.AddMinutes(-30)));
        db.WeatherRiskDataPoints.Add(CreateDataPoint(-9.11, 38.65, RiskLevel.Critical, 75, latestTime));
        await db.SaveChangesAsync();

        var handler = new GetDetailedRiskZonesHandler(db);
        var query = new GetDetailedRiskZonesQuery(null);

        // Act
        var response = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        response.LastUpdated.Should().BeCloseTo(latestTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ZonesHaveRequiredFields()
    {
        // Arrange
        var dbName = $"required_fields_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var now = DateTime.UtcNow;

        var point = CreateDataPoint(-9.10, 38.64, RiskLevel.Critical, 68, now.AddMinutes(-5), "High risk");
        db.WeatherRiskDataPoints.Add(point);
        await db.SaveChangesAsync();

        var handler = new GetDetailedRiskZonesHandler(db);
        var query = new GetDetailedRiskZonesQuery(null);

        // Act
        var response = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        response.Zones.Should().NotBeEmpty();
        foreach (var zone in response.Zones)
        {
            zone.Id.Should().NotBeEmpty();
            zone.RiskLevelName.Should().NotBeNullOrEmpty();
            zone.Wkt.Should().StartWith("POLYGON");
            zone.Conclusion.Should().NotBeNullOrEmpty();
        }
    }
}