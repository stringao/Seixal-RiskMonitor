using FluentAssertions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.Alerts;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Cache;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GeoRisk.API.Tests.Features.Alerts;

public sealed class GetAlertsHandlerTests
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
        cache.Setup(c => c.GetAsync<AlertListResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertListResponse?)null);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<AlertListResponse>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return cache;
    }

    private static async Task SeedAlerts(GeoRiskDbContext db)
    {
        var alerts = new List<Alert>
        {
            new() { Id = Guid.NewGuid(), Title = "Alert 1", Severity = AlertSeverity.Warning, Message = "m", IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Title = "Alert 2", Severity = AlertSeverity.Critical, Message = "m", IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), Title = "Alert 3", Severity = AlertSeverity.Info, Message = "m", IsRead = true, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new() { Id = Guid.NewGuid(), Title = "Alert 4", Severity = AlertSeverity.Danger, Message = "m", IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-4) },
        };
        db.Alerts.AddRange(alerts);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_NoFilters_ReturnsAllPaginated()
    {
        var dbName = $"get_alerts_all_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var cache = CreateCacheMock();
        var handler = new GetAlertsHandler(db, cache.Object);
        var query = new GetAlertsQuery(1, 20, null, null, "cache:key");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task HandleAsync_FilterBySeverity_ReturnsMatchingAlerts()
    {
        var dbName = $"get_alerts_severity_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var cache = CreateCacheMock();
        var handler = new GetAlertsHandler(db, cache.Object);
        var query = new GetAlertsQuery(1, 20, AlertSeverity.Critical, null, "cache:key");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.Should().OnlyContain(a => a.Severity == AlertSeverity.Critical);
    }

    [Fact]
    public async Task HandleAsync_FilterByIsRead_ReturnsMatchingAlerts()
    {
        var dbName = $"get_alerts_isread_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var cache = CreateCacheMock();
        var handler = new GetAlertsHandler(db, cache.Object);
        var query = new GetAlertsQuery(1, 20, null, false, "cache:key");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(a => !a.IsRead);
    }

    [Fact]
    public async Task HandleAsync_PaginationPage2_ReturnsCorrectSlice()
    {
        var dbName = $"get_alerts_page2_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var cache = CreateCacheMock();
        var handler = new GetAlertsHandler(db, cache.Object);
        var query = new GetAlertsQuery(2, 2, null, null, "cache:key");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(4);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_CachedResponse_ReturnsCachedData()
    {
        var dbName = $"get_alerts_cached_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cachedResult = new AlertListResponse(
            new List<AlertResponse> { new(Guid.NewGuid(), "Cached", AlertSeverity.Info, "m", null, false, DateTime.UtcNow) },
            1, 1, 20);
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<AlertListResponse>("cache:hit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);
        var handler = new GetAlertsHandler(db, cache.Object);
        var query = new GetAlertsQuery(1, 20, null, null, "cache:hit");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Cached");
    }

    [Fact]
    public async Task HandleAsync_CombinedFilters_ReturnsMatchingAlerts()
    {
        var dbName = $"get_alerts_combined_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var cache = CreateCacheMock();
        var handler = new GetAlertsHandler(db, cache.Object);
        var query = new GetAlertsQuery(1, 20, AlertSeverity.Warning, false, "cache:key");

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Severity.Should().Be(AlertSeverity.Warning);
        result.Items[0].IsRead.Should().BeFalse();
    }
}
