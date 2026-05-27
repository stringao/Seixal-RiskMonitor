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

public sealed class GetEventsHandlerTests
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
        cache.Setup(c => c.GetAsync<EventListResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventListResponse?)null);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<EventListResponse>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return cache;
    }

    private static async Task SeedEvents(GeoRiskDbContext db)
    {
        var events = new List<GeoEvent>
        {
            new() { Id = Guid.NewGuid(), EventType = EventType.Fire, Title = "Fire 1", Description = "d", Geometry = new Point(-9.10, 38.64) { SRID = 4326 }, Severity = RiskLevel.High, Source = EventSource.ICNF, OccurredAt = DateTime.UtcNow.AddDays(-1), SourceId = "S1" },
            new() { Id = Guid.NewGuid(), EventType = EventType.Fire, Title = "Fire 2", Description = "d", Geometry = new Point(-9.11, 38.65) { SRID = 4326 }, Severity = RiskLevel.Medium, Source = EventSource.ANEPC, OccurredAt = DateTime.UtcNow.AddDays(-2), SourceId = "S2" },
            new() { Id = Guid.NewGuid(), EventType = EventType.Flood, Title = "Flood 1", Description = "d", Geometry = new Point(-9.12, 38.66) { SRID = 4326 }, Severity = RiskLevel.Critical, Source = EventSource.IPMA, OccurredAt = DateTime.UtcNow.AddDays(-3), SourceId = "S3" },
            new() { Id = Guid.NewGuid(), EventType = EventType.Flood, Title = "Flood 2", Description = "d", Geometry = new Point(-9.13, 38.67) { SRID = 4326 }, Severity = RiskLevel.Low, Source = EventSource.IPMA, OccurredAt = DateTime.UtcNow.AddDays(-4), SourceId = "S4" },
            new() { Id = Guid.NewGuid(), EventType = EventType.Storm, Title = "Storm 1", Description = "d", Geometry = new Point(-9.14, 38.68) { SRID = 4326 }, Severity = RiskLevel.High, Source = EventSource.IPMA, OccurredAt = DateTime.UtcNow.AddDays(-5), SourceId = "S5" }
        };
        db.GeoEvents.AddRange(events);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_NoFilters_ReturnsAllPaginated()
    {
        // Arrange
        var dbName = $"get_events_all_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db);
        var cache = CreateCacheMock();
        var handler = new GetEventsHandler(db, cache.Object);
        var query = new GetEventsQuery(1, 10, null, null, null, null, null, null, null, null, "cache:key", null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_FilterByType_ReturnsMatchingEvents()
    {
        // Arrange
        var dbName = $"get_events_type_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db);
        var cache = CreateCacheMock();
        var handler = new GetEventsHandler(db, cache.Object);
        var query = new GetEventsQuery(1, 10, EventType.Fire, null, null, null, null, null, null, null, "cache:key", null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(e => e.EventType == "Fire");
    }

    [Fact]
    public async Task HandleAsync_FilterBySeverity_ReturnsMatchingEvents()
    {
        // Arrange
        var dbName = $"get_events_severity_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db);
        var cache = CreateCacheMock();
        var handler = new GetEventsHandler(db, cache.Object);
        var query = new GetEventsQuery(1, 10, null, RiskLevel.High, null, null, null, null, null, null, "cache:key", null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(e => e.Severity == "High");
    }

    [Fact]
    public async Task HandleAsync_PaginationPage2_ReturnsCorrectSlice()
    {
        // Arrange
        var dbName = $"get_events_page2_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db);
        var cache = CreateCacheMock();
        var handler = new GetEventsHandler(db, cache.Object);
        var query = new GetEventsQuery(2, 2, null, null, null, null, null, null, null, null, "cache:key", null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_CachedResponse_ReturnsCachedData()
    {
        // Arrange
        var dbName = $"get_events_cached_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cachedResult = new EventListResponse(
            new List<EventResponse> { new(Guid.NewGuid(), "Fire", "Cached", null, 0, 0, "Low", "Manual", DateTime.UtcNow, null, null, DateTime.UtcNow) },
            1, 1, 10);
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<EventListResponse>("cache:hit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);
        var handler = new GetEventsHandler(db, cache.Object);
        var query = new GetEventsQuery(1, 10, null, null, null, null, null, null, null, null, "cache:hit", null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Cached");
    }

    [Fact]
    public async Task HandleAsync_DateRangeFilter_ReturnsMatchingEvents()
    {
        // Arrange
        var dbName = $"get_events_dates_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db);
        var cache = CreateCacheMock();
        var handler = new GetEventsHandler(db, cache.Object);
        var from = DateTime.UtcNow.AddDays(-3).AddHours(-1);
        var to = DateTime.UtcNow;
        var query = new GetEventsQuery(1, 10, null, null, from, to, null, null, null, null, "cache:key", null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
    }
}
