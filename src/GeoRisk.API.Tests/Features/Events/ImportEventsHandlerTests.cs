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

public sealed class ImportEventsHandlerTests
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
    public async Task HandleAsync_ImportsNewEvents()
    {
        // Arrange
        var dbName = $"import_new_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = CreateCacheMock();
        var handler = new ImportEventsHandler(db, cache.Object);
        var items = new List<ImportEventItem>
        {
            new("IMP-001", EventType.Fire, "Imported Fire", 38.64, -9.10, RiskLevel.High, EventSource.ICNF, DateTime.UtcNow, null),
            new("IMP-002", EventType.Flood, "Imported Flood", 38.65, -9.11, RiskLevel.Medium, EventSource.IPMA, DateTime.UtcNow, "desc")
        };
        var cmd = new ImportEventsCommand(items);

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Imported.Should().Be(2);
        result.Skipped.Should().Be(0);
        var all = await db.GeoEvents.ToListAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_SkipsDuplicatesBySourceId()
    {
        // Arrange
        var dbName = $"import_dup_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(), EventType = EventType.Fire, Title = "Existing", Description = null,
            Geometry = new Point(-9.10, 38.64) { SRID = 4326 }, Severity = RiskLevel.High,
            Source = EventSource.ICNF, SourceId = "IMP-001", OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var cache = CreateCacheMock();
        var handler = new ImportEventsHandler(db, cache.Object);
        var items = new List<ImportEventItem>
        {
            new("IMP-001", EventType.Fire, "Duplicate", 38.64, -9.10, RiskLevel.High, EventSource.ICNF, DateTime.UtcNow, null),
            new("IMP-002", EventType.Flood, "New Event", 38.65, -9.11, RiskLevel.Medium, EventSource.IPMA, DateTime.UtcNow, null)
        };
        var cmd = new ImportEventsCommand(items);

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Imported.Should().Be(1);
        result.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_InvalidatesCache()
    {
        // Arrange
        var dbName = $"import_cache_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = CreateCacheMock();
        var handler = new ImportEventsHandler(db, cache.Object);
        var items = new List<ImportEventItem>
        {
            new("IMP-CACHE-001", EventType.Storm, "Storm", 38.64, -9.10, RiskLevel.Critical, EventSource.IPMA, DateTime.UtcNow, null)
        };
        var cmd = new ImportEventsCommand(items);

        // Act
        await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        cache.Verify(c => c.RemoveByPrefixAsync("events:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyList_ReturnsZeros()
    {
        // Arrange
        var dbName = $"import_empty_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = CreateCacheMock();
        var handler = new ImportEventsHandler(db, cache.Object);
        var cmd = new ImportEventsCommand([]);

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Imported.Should().Be(0);
        result.Skipped.Should().Be(0);
        cache.Verify(c => c.RemoveByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
