using FluentAssertions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.Events;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Features.Events;

public sealed class CreateEventHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    private static Mock<ICacheService> CreateCacheMock()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return cache;
    }

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesEventAndReturnsResponse()
    {
        // Arrange
        var dbName = $"create_event_valid_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = CreateCacheMock();
        var handler = new CreateEventHandler(db, cache.Object);
        var cmd = new CreateEventCommand(
            EventType.Fire, "Test Fire", "A fire event",
            38.64, -9.10, RiskLevel.High,
            EventSource.Manual, DateTime.UtcNow, null);

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EventType.Should().Be("Fire");
        result.Title.Should().Be("Test Fire");
        result.Description.Should().Be("A fire event");
        result.Severity.Should().Be("High");
        result.Source.Should().Be("Manual");
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_PersistsGeometryToDatabase()
    {
        // Arrange
        var dbName = $"create_event_geometry_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = CreateCacheMock();
        var handler = new CreateEventHandler(db, cache.Object);
        var cmd = new CreateEventCommand(
            EventType.Flood, "Flood Event", null,
            38.64, -9.10, RiskLevel.Medium,
            EventSource.IPMA, DateTime.UtcNow, null);

        // Act
        await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        var saved = await db.GeoEvents.FirstOrDefaultAsync(e => e.Title == "Flood Event");
        saved.Should().NotBeNull();
        saved!.Geometry.Should().NotBeNull();
        saved.Geometry.X.Should().Be(-9.10);
        saved.Geometry.Y.Should().Be(38.64);
    }

    [Fact]
    public async Task HandleAsync_InvalidatesCache()
    {
        // Arrange
        var dbName = $"create_event_cache_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = CreateCacheMock();
        var handler = new CreateEventHandler(db, cache.Object);
        var cmd = new CreateEventCommand(
            EventType.Storm, "Storm Event", null,
            38.64, -9.10, RiskLevel.Critical,
            EventSource.IPMA, DateTime.UtcNow, null);

        // Act
        await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        cache.Verify(c => c.RemoveByPrefixAsync("events:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SetsMetadataWhenProvided()
    {
        // Arrange
        var dbName = $"create_event_metadata_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = CreateCacheMock();
        var handler = new CreateEventHandler(db, cache.Object);
        var metadata = "{\"source\":\"test\"}";
        var cmd = new CreateEventCommand(
            EventType.Industrial, "Industrial Event", null,
            38.64, -9.10, RiskLevel.High,
            EventSource.ANEPC, DateTime.UtcNow, metadata);

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        var saved = await db.GeoEvents.FirstOrDefaultAsync(e => e.Title == "Industrial Event");
        saved.Should().NotBeNull();
        saved!.Metadata.Should().Be(metadata);
    }
}
