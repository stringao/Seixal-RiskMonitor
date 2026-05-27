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

public sealed class MarkAlertReadHandlerTests
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

    private static async Task SeedAlerts(GeoRiskDbContext db)
    {
        var alerts = new List<Alert>
        {
            new() { Id = Guid.NewGuid(), Title = "Alert 1", Severity = AlertSeverity.Warning, Message = "m", IsRead = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Title = "Alert 2", Severity = AlertSeverity.Critical, Message = "m", IsRead = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Title = "Alert 3", Severity = AlertSeverity.Info, Message = "m", IsRead = true, CreatedAt = DateTime.UtcNow },
        };
        db.Alerts.AddRange(alerts);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_MarkAllRead_MarksAllUnreadAsRead()
    {
        var dbName = $"mark_all_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var handler = new MarkAlertReadHandler(db, CreateCacheMock().Object);
        var command = new MarkAlertReadCommand(null, true);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.MarkedCount.Should().Be(2);
        var unreadCount = await db.Alerts.CountAsync(a => !a.IsRead);
        unreadCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_MarkSpecificIds_MarksOnlyThose()
    {
        var dbName = $"mark_ids_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var alertToMark = db.Alerts.First(a => !a.IsRead);
        var handler = new MarkAlertReadHandler(db, CreateCacheMock().Object);
        var command = new MarkAlertReadCommand(new List<Guid> { alertToMark.Id }, false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.MarkedCount.Should().Be(1);
        var stillUnread = db.Alerts.Count(a => !a.IsRead);
        stillUnread.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_EmptyIds_ReturnsZero()
    {
        var dbName = $"mark_empty_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var handler = new MarkAlertReadHandler(db, CreateCacheMock().Object);
        var command = new MarkAlertReadCommand(new List<Guid>(), false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.MarkedCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_NonExistentIds_ReturnsZero()
    {
        var dbName = $"mark_nonexistent_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedAlerts(db);
        var handler = new MarkAlertReadHandler(db, CreateCacheMock().Object);
        var command = new MarkAlertReadCommand(new List<Guid> { Guid.NewGuid() }, false);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.MarkedCount.Should().Be(0);
    }
}
