using FluentAssertions;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.Events;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Features.Events;

public sealed class GetEventByIdHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ExistingId_ReturnsEvent()
    {
        // Arrange
        var dbName = $"get_by_id_exists_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var eventId = Guid.NewGuid();
        db.GeoEvents.Add(new GeoEvent
        {
            Id = eventId, EventType = EventType.Fire, Title = "Test Fire", Description = "desc",
            Geometry = new Point(-9.10, 38.64) { SRID = 4326 }, Severity = RiskLevel.High,
            Source = EventSource.Manual, OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetEventByIdHandler(db);
        var query = new GetEventByIdQuery(eventId);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventId);
        result.Title.Should().Be("Test Fire");
        result.EventType.Should().Be(EventType.Fire);
        result.Latitude.Should().Be(38.64);
        result.Longitude.Should().Be(-9.10);
    }

    [Fact]
    public async Task HandleAsync_NonexistentId_ReturnsNull()
    {
        // Arrange
        var dbName = $"get_by_id_missing_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var handler = new GetEventByIdHandler(db);
        var query = new GetEventByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
