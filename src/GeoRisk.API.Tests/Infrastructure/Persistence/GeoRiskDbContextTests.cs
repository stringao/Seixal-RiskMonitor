using FluentAssertions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Infrastructure.Persistence;

public sealed class GeoRiskDbContextTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    private static GeoEvent CreateTestGeoEvent(
        Guid? id = null,
        EventType eventType = EventType.Flood,
        RiskLevel severity = RiskLevel.High,
        EventSource source = EventSource.Manual)
    {
        return new GeoEvent
        {
            Id = id ?? Guid.NewGuid(),
            EventType = eventType,
            Title = "Test Event",
            Severity = severity,
            Source = source,
            OccurredAt = DateTime.UtcNow,
            Geometry = new Point(0, 0)
        };
    }

    [Fact]
    public async Task SaveChangesAsync_SetsCreatedAt_OnGeoEventAdd()
    {
        // Arrange
        var dbName = $"db_createdat_add_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var geoEvent = CreateTestGeoEvent();

        // Act
        db.GeoEvents.Add(geoEvent);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.GeoEvents.FindAsync(geoEvent.Id);
        saved.Should().NotBeNull();
        saved!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChangesAsync_SetsUpdatedAt_OnGeoEventAdd()
    {
        // Arrange
        var dbName = $"db_updatedat_add_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var geoEvent = CreateTestGeoEvent();

        // Act
        db.GeoEvents.Add(geoEvent);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.GeoEvents.FindAsync(geoEvent.Id);
        saved.Should().NotBeNull();
        saved!.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChangesAsync_SetsUpdatedAt_OnGeoEventModify()
    {
        // Arrange
        var dbName = $"db_updatedat_modify_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var geoEvent = CreateTestGeoEvent();
        db.GeoEvents.Add(geoEvent);
        await db.SaveChangesAsync();

        var originalCreatedAt = geoEvent.CreatedAt;

        // Act
        geoEvent.Title = "Updated Title";
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.GeoEvents.FindAsync(geoEvent.Id);
        saved.Should().NotBeNull();
        saved!.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        saved.CreatedAt.Should().Be(originalCreatedAt, "CreatedAt should not change on modify");
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotOverwriteCreatedAt_OnGeoEventModify()
    {
        // Arrange
        var dbName = $"db_createdat_preserve_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var geoEvent = CreateTestGeoEvent();
        db.GeoEvents.Add(geoEvent);
        await db.SaveChangesAsync();

        var originalCreatedAt = geoEvent.CreatedAt;
        geoEvent.Title = "Modified";
        geoEvent.Severity = RiskLevel.Critical;

        // Act
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.GeoEvents.FindAsync(geoEvent.Id);
        saved.Should().NotBeNull();
        saved!.CreatedAt.Should().Be(originalCreatedAt);
        saved.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChangesAsync_HandlesMultipleGeoEventsAdded()
    {
        // Arrange
        var dbName = $"db_multi_add_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var event1 = CreateTestGeoEvent(id: Guid.NewGuid(), eventType: EventType.Fire);
        var event2 = CreateTestGeoEvent(id: Guid.NewGuid(), eventType: EventType.Flood);

        // Act
        db.GeoEvents.AddRange(event1, event2);
        await db.SaveChangesAsync();

        // Assert
        var allEvents = await db.GeoEvents.ToListAsync();
        allEvents.Should().HaveCount(2);
        allEvents.Should().AllSatisfy(e =>
        {
            e.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            e.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        });
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotAffectNonGeoEventEntities()
    {
        // Arrange
        var dbName = $"db_non_geoevent_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            Title = "Test Alert",
            Severity = AlertSeverity.Warning,
            Message = "Test message",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.Alerts.FindAsync(alert.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Test Alert");
    }

    [Fact]
    public async Task GeoEventsDbSet_CanQueryEntities()
    {
        // Arrange
        var dbName = $"db_query_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var geoEvent = CreateTestGeoEvent(eventType: EventType.Storm, severity: RiskLevel.Critical);
        db.GeoEvents.Add(geoEvent);
        await db.SaveChangesAsync();

        // Act
        var result = await db.GeoEvents
            .Where(e => e.EventType == EventType.Storm)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Severity.Should().Be(RiskLevel.Critical);
    }

    [Fact]
    public async Task UsersDbSet_CanAddAndQueryUsers()
    {
        // Arrange
        var dbName = $"db_users_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "dbtest@example.com",
            PasswordHash = "hash",
            Role = UserRole.Analyst,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.Users.FirstOrDefaultAsync(u => u.Email == "dbtest@example.com");
        saved.Should().NotBeNull();
        saved!.Role.Should().Be(UserRole.Analyst);
    }

    [Fact]
    public async Task RefreshTokensDbSet_CanAddAndQueryTokens()
    {
        // Arrange
        var dbName = $"db_refreshtokens_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "tokenuser@example.com",
            PasswordHash = "hash",
            Role = UserRole.Viewer,
            CreatedAt = DateTime.UtcNow
        };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test_token_123",
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Users.Add(user);
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == "test_token_123");
        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(userId);
        saved.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RiskZonesDbSet_CanAddAndQuery()
    {
        // Arrange
        var dbName = $"db_riskzones_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var zone = new RiskZone
        {
            Id = Guid.NewGuid(),
            Name = "Test Zone",
            Geometry = new Polygon(new LinearRing(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(1, 0),
                new Coordinate(1, 1),
                new Coordinate(0, 1),
                new Coordinate(0, 0)
            })),
            RiskLevel = RiskLevel.High,
            CalculatedAt = DateTime.UtcNow,
            Source = "Test"
        };

        // Act
        db.RiskZones.Add(zone);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.RiskZones.FindAsync(zone.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Zone");
        saved.RiskLevel.Should().Be(RiskLevel.High);
    }

    [Fact]
    public async Task AlertRulesDbSet_CanAddAndQuery()
    {
        // Arrange
        var dbName = $"db_alertrules_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            Name = "Fire Alert Rule",
            EventType = EventType.Fire,
            SeverityThreshold = RiskLevel.High,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.AlertRules.Add(rule);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.AlertRules.FindAsync(rule.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Fire Alert Rule");
        saved.IsActive.Should().BeTrue();
        saved.EventType.Should().Be(EventType.Fire);
    }

    [Fact]
    public async Task AlertsDbSet_CanAddAndQueryWithGeoEventReference()
    {
        // Arrange
        var dbName = $"db_alerts_geoevent_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);

        var geoEvent = CreateTestGeoEvent();
        db.GeoEvents.Add(geoEvent);
        await db.SaveChangesAsync();

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            Title = "Linked Alert",
            Severity = AlertSeverity.Danger,
            Message = "GeoEvent alert",
            GeoEventId = geoEvent.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.Alerts.FindAsync(alert.Id);
        saved.Should().NotBeNull();
        saved!.GeoEventId.Should().Be(geoEvent.Id);
        saved.IsRead.Should().BeFalse();
        saved.Severity.Should().Be(AlertSeverity.Danger);
    }
}
