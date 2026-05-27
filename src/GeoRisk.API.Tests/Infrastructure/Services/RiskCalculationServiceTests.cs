using FluentAssertions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Infrastructure.Services;

public sealed class RiskCalculationServiceTests : IDisposable
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    public void Dispose()
    {
    }

    #region ScoreToRiskLevel Tests

    [Theory]
    [InlineData(0, RiskLevel.Low)]
    [InlineData(5, RiskLevel.Low)]
    [InlineData(9.9, RiskLevel.Low)]
    [InlineData(10, RiskLevel.Medium)]
    [InlineData(20, RiskLevel.Medium)]
    [InlineData(29.9, RiskLevel.Medium)]
    [InlineData(30, RiskLevel.High)]
    [InlineData(50, RiskLevel.High)]
    [InlineData(59.9, RiskLevel.High)]
    [InlineData(60, RiskLevel.Critical)]
    [InlineData(80, RiskLevel.Critical)]
    [InlineData(100, RiskLevel.Critical)]
    public void ScoreToRiskLevel_ReturnsCorrectLevel(double score, RiskLevel expectedLevel)
    {
        // Arrange
        var service = new RiskCalculationService();

        // Act
        var result = service.ScoreToRiskLevel(score);

        // Assert
        result.Should().Be(expectedLevel);
    }

    #endregion

    #region CalculateZoneScoresAsync Tests

    [Fact]
    public async Task CalculateZoneScoresAsync_NoZones_ReturnsEmptyDictionary()
    {
        // Arrange
        var dbName = $"calc_no_zones_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var service = new RiskCalculationService();

        // Act
        var result = await service.CalculateZoneScoresAsync(db);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateZoneScoresAsync_ZoneWithNoEvents_ReturnsZeroScore()
    {
        // Arrange
        var dbName = $"calc_no_events_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var service = new RiskCalculationService();

        var zoneId = Guid.NewGuid();
        var zoneGeometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-9.2, 38.6),
            new Coordinate(-9.0, 38.6),
            new Coordinate(-9.0, 38.8),
            new Coordinate(-9.2, 38.8),
            new Coordinate(-9.2, 38.6)
        }));

        db.RiskZones.Add(new RiskZone
        {
            Id = zoneId,
            Name = "Empty Zone",
            Geometry = zoneGeometry,
            RiskLevel = RiskLevel.Low
        });
        await db.SaveChangesAsync();

        // Act
        var result = await service.CalculateZoneScoresAsync(db);

        // Assert
        result.Should().ContainKey(zoneId);
        result[zoneId].Should().Be(0);
    }

    [Fact]
    public async Task CalculateZoneScoresAsync_SingleHighSeverityEvent_ReturnsCorrectScore()
    {
        // Arrange
        var dbName = $"calc_single_high_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var service = new RiskCalculationService();

        var zoneGeometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-9.2, 38.6),
            new Coordinate(-9.0, 38.6),
            new Coordinate(-9.0, 38.8),
            new Coordinate(-9.2, 38.8),
            new Coordinate(-9.2, 38.6)
        }));

        var zoneId = Guid.NewGuid();
        db.RiskZones.Add(new RiskZone
        {
            Id = zoneId,
            Name = "Test Zone",
            Geometry = zoneGeometry,
            RiskLevel = RiskLevel.Medium
        });

        // Event inside the zone (coordinates: -9.15, 38.7 which is inside the polygon)
        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(),
            Title = "High Severity Fire",
            EventType = EventType.Fire,
            Severity = RiskLevel.High,
            Geometry = new Point(new Coordinate(-9.15, 38.7)),
            Source = EventSource.Manual,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var result = await service.CalculateZoneScoresAsync(db);

        // Assert
        result.Should().ContainKey(zoneId);
        // High severity has weight 7, so score should be 7 (before decay)
        result[zoneId].Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateZoneScoresAsync_MultipleEvents_AccumulatesScore()
    {
        // Arrange
        var dbName = $"calc_multiple_events_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var service = new RiskCalculationService();

        var zoneGeometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-9.2, 38.6),
            new Coordinate(-9.0, 38.6),
            new Coordinate(-9.0, 38.8),
            new Coordinate(-9.2, 38.8),
            new Coordinate(-9.2, 38.6)
        }));

        var zoneId = Guid.NewGuid();
        db.RiskZones.Add(new RiskZone
        {
            Id = zoneId,
            Name = "Multi Event Zone",
            Geometry = zoneGeometry,
            RiskLevel = RiskLevel.High
        });

        // Multiple events inside the zone
        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(),
            Title = "Fire 1",
            EventType = EventType.Fire,
            Severity = RiskLevel.High,
            Geometry = new Point(new Coordinate(-9.15, 38.7)),
            Source = EventSource.Manual,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(),
            Title = "Fire 2",
            EventType = EventType.Fire,
            Severity = RiskLevel.High,
            Geometry = new Point(new Coordinate(-9.16, 38.71)),
            Source = EventSource.Manual,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var result = await service.CalculateZoneScoresAsync(db);

        // Assert
        result.Should().ContainKey(zoneId);
        // Two High severity events: 7 + 7 = 14 (before decay)
        result[zoneId].Should().BeGreaterThan(13);
    }

    [Fact]
    public async Task CalculateZoneScoresAsync_ScoreIsCappedAt100()
    {
        // Arrange
        var dbName = $"calc_capped_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var service = new RiskCalculationService();

        var zoneGeometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-9.2, 38.6),
            new Coordinate(-9.0, 38.6),
            new Coordinate(-9.0, 38.8),
            new Coordinate(-9.2, 38.8),
            new Coordinate(-9.2, 38.6)
        }));

        var zoneId = Guid.NewGuid();
        db.RiskZones.Add(new RiskZone
        {
            Id = zoneId,
            Name = "High Risk Zone",
            Geometry = zoneGeometry,
            RiskLevel = RiskLevel.Critical
        });

        // Add many Critical severity events
        for (int i = 0; i < 20; i++)
        {
            db.GeoEvents.Add(new GeoEvent
            {
                Id = Guid.NewGuid(),
                Title = $"Critical Event {i}",
                EventType = EventType.Fire,
                Severity = RiskLevel.Critical,
                Geometry = new Point(new Coordinate(-9.15 + (i * 0.001), 38.7 + (i * 0.001))),
                Source = EventSource.Manual,
                OccurredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        // Act
        var result = await service.CalculateZoneScoresAsync(db);

        // Assert
        result.Should().ContainKey(zoneId);
        result[zoneId].Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task CalculateZoneScoresAsync_EventOutsideZone_NotIncludedInScore()
    {
        // Arrange
        var dbName = $"calc_outside_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var service = new RiskCalculationService();

        var zoneGeometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-9.2, 38.6),
            new Coordinate(-9.0, 38.6),
            new Coordinate(-9.0, 38.8),
            new Coordinate(-9.2, 38.8),
            new Coordinate(-9.2, 38.6)
        }));

        var zoneId = Guid.NewGuid();
        db.RiskZones.Add(new RiskZone
        {
            Id = zoneId,
            Name = "Test Zone",
            Geometry = zoneGeometry,
            RiskLevel = RiskLevel.Medium
        });

        // Event OUTSIDE the zone
        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(),
            Title = "Outside Event",
            EventType = EventType.Fire,
            Severity = RiskLevel.Critical,
            Geometry = new Point(new Coordinate(-8.5, 39.0)), // Clearly outside Seixal area
            Source = EventSource.Manual,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var result = await service.CalculateZoneScoresAsync(db);

        // Assert
        result.Should().ContainKey(zoneId);
        result[zoneId].Should().Be(0);
    }

    [Fact]
    public async Task CalculateZoneScoresAsync_MultipleZones_CalculatesEachSeparately()
    {
        // Arrange
        var dbName = $"calc_multiple_zones_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var service = new RiskCalculationService();

        var zone1Geometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-9.2, 38.6),
            new Coordinate(-9.1, 38.6),
            new Coordinate(-9.1, 38.7),
            new Coordinate(-9.2, 38.7),
            new Coordinate(-9.2, 38.6)
        }));

        var zone2Geometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-9.05, 38.6),
            new Coordinate(-8.95, 38.6),
            new Coordinate(-8.95, 38.7),
            new Coordinate(-9.05, 38.7),
            new Coordinate(-9.05, 38.6)
        }));

        var zone1Id = Guid.NewGuid();
        var zone2Id = Guid.NewGuid();

        db.RiskZones.Add(new RiskZone
        {
            Id = zone1Id,
            Name = "Zone 1",
            Geometry = zone1Geometry,
            RiskLevel = RiskLevel.Low
        });

        db.RiskZones.Add(new RiskZone
        {
            Id = zone2Id,
            Name = "Zone 2",
            Geometry = zone2Geometry,
            RiskLevel = RiskLevel.High
        });

        // Event only in Zone 1
        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(),
            Title = "Zone 1 Event",
            EventType = EventType.Fire,
            Severity = RiskLevel.High,
            Geometry = new Point(new Coordinate(-9.15, 38.65)),
            Source = EventSource.Manual,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var result = await service.CalculateZoneScoresAsync(db);

        // Assert
        result.Should().ContainKey(zone1Id);
        result.Should().ContainKey(zone2Id);
        result[zone1Id].Should().BeGreaterThan(0);
        result[zone2Id].Should().Be(0);
    }

    #endregion
}
