using FluentAssertions;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Features.Alerts;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Persistence;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Features.Alerts;

public sealed class ConfigureAlertRulesTests : IDisposable
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    private static Mock<AlertRuleEvaluationEngine> CreateEvaluationEngineMock()
    {
        var dbMock = new Mock<GeoRiskDbContext>(MockBehavior.Loose, new DbContextOptionsBuilder<GeoRiskDbContext>().Options);
        var loggerMock = new Mock<ILogger<AlertRuleEvaluationEngine>>();
        return new Mock<AlertRuleEvaluationEngine>(dbMock.Object, loggerMock.Object);
    }

    private static Mock<ILogger<ConfigureAlertRulesHandler>> CreateLoggerMock()
    {
        return new Mock<ILogger<ConfigureAlertRulesHandler>>();
    }

    public void Dispose()
    {
    }

    #region CreateRule Tests

    [Fact]
    public async Task CreateRule_WithValidData_CreatesAndReturnsAlertRule()
    {
        // Arrange
        var dbName = $"create_rule_valid_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);
        var request = new CreateAlertRuleRequest(
            Name: "Fire Alert Rule",
            EventType: "Fire",
            SeverityThreshold: "High",
            AreaWkt: null,
            IsActive: true,
            MinFwi: null,
            MaxFwi: null,
            MinWindSpeed: null,
            MinTemperature: null,
            SeasonStartMonth: null,
            SeasonEndMonth: null,
            AreaKm2Threshold: null,
            ConsecutiveCount: null,
            EscalationMinutes: null,
            NotifyRoles: null);
        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.Create, null, request, null);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Fire Alert Rule");
        result.EventType.Should().Be("Fire");
        result.SeverityThreshold.Should().Be("High");
        result.IsActive.Should().BeTrue();
        result.Id.Should().NotBe(Guid.Empty);

        // Verify persisted
        var saved = await db.AlertRules.FirstOrDefaultAsync(r => r.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Fire Alert Rule");
    }

    [Fact]
    public async Task CreateRule_WithPolygonArea_CreatesRuleWithGeometry()
    {
        // Arrange
        var dbName = $"create_rule_geometry_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);
        var wkt = "POLYGON ((-9.1 38.6, -9.0 38.6, -9.0 38.7, -9.1 38.7, -9.1 38.6))";
        var request = new CreateAlertRuleRequest(
            Name: "Area Rule",
            EventType: "Flood",
            SeverityThreshold: "Medium",
            AreaWkt: wkt,
            IsActive: true,
            MinFwi: null,
            MaxFwi: null,
            MinWindSpeed: null,
            MinTemperature: null,
            SeasonStartMonth: null,
            SeasonEndMonth: null,
            AreaKm2Threshold: null,
            ConsecutiveCount: null,
            EscalationMinutes: null,
            NotifyRoles: null);
        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.Create, null, request, null);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AreaWkt.Should().Contain("POLYGON");
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateRule_WithInvalidWkt_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbName = $"create_rule_invalid_wkt_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);
        var request = new CreateAlertRuleRequest(
            Name: "Invalid WKT Rule",
            EventType: "Fire",
            SeverityThreshold: "High",
            AreaWkt: "INVALID_WKT",
            IsActive: true,
            MinFwi: null,
            MaxFwi: null,
            MinWindSpeed: null,
            MinTemperature: null,
            SeasonStartMonth: null,
            SeasonEndMonth: null,
            AreaKm2Threshold: null,
            ConsecutiveCount: null,
            EscalationMinutes: null,
            NotifyRoles: null);
        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.Create, null, request, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRule_WithAllEventTypes_CreatesSuccessfully()
    {
        // Arrange
        var eventTypes = new[] { "Fire", "Flood", "Storm", "Landslide", "Industrial", "Heatwave", "Other" };

        foreach (var eventType in eventTypes)
        {
            var dbName = $"create_rule_{eventType.ToLower()}_{Guid.NewGuid()}";
            using var db = CreateDbContext(dbName);
            var evaluationEngine = CreateEvaluationEngineMock();
            var logger = CreateLoggerMock();
            var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);
            var request = new CreateAlertRuleRequest(
                Name: $"{eventType} Rule",
                EventType: eventType,
                SeverityThreshold: "Medium",
                AreaWkt: null,
                IsActive: true,
                MinFwi: null,
                MaxFwi: null,
                MinWindSpeed: null,
                MinTemperature: null,
                SeasonStartMonth: null,
                SeasonEndMonth: null,
                AreaKm2Threshold: null,
                ConsecutiveCount: null,
                EscalationMinutes: null,
                NotifyRoles: null);
            var command = new ConfigureAlertRulesCommand(
                ConfigureAlertRulesAction.Create, null, request, null);

            // Act
            var result = await handler.HandleAsync(command, CancellationToken.None);

            // Assert
            result.EventType.Should().Be(eventType);
        }
    }

    [Fact]
    public async Task CreateRule_WithAllRiskLevels_CreatesSuccessfully()
    {
        // Arrange
        var riskLevels = new[] { "Low", "Medium", "High", "Critical" };

        foreach (var riskLevel in riskLevels)
        {
            var dbName = $"create_rule_{riskLevel.ToLower()}_{Guid.NewGuid()}";
            using var db = CreateDbContext(dbName);
            var evaluationEngine = CreateEvaluationEngineMock();
            var logger = CreateLoggerMock();
            var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);
            var request = new CreateAlertRuleRequest(
                Name: $"{riskLevel} Risk Rule",
                EventType: "Fire",
                SeverityThreshold: riskLevel,
                AreaWkt: null,
                IsActive: true,
                MinFwi: null,
                MaxFwi: null,
                MinWindSpeed: null,
                MinTemperature: null,
                SeasonStartMonth: null,
                SeasonEndMonth: null,
                AreaKm2Threshold: null,
                ConsecutiveCount: null,
                EscalationMinutes: null,
                NotifyRoles: null);
            var command = new ConfigureAlertRulesCommand(
                ConfigureAlertRulesAction.Create, null, request, null);

            // Act
            var result = await handler.HandleAsync(command, CancellationToken.None);

            // Assert
            result.SeverityThreshold.Should().Be(riskLevel);
        }
    }

    #endregion

    #region UpdateRule Tests

    [Fact]
    public async Task UpdateRule_WithValidData_UpdatesAndReturnsAlertRule()
    {
        // Arrange
        var dbName = $"update_rule_valid_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);

        // Create initial rule
        var ruleId = Guid.NewGuid();
        db.AlertRules.Add(new AlertRule
        {
            Id = ruleId,
            Name = "Original Name",
            EventType = EventType.Fire,
            SeverityThreshold = RiskLevel.Medium,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var updateRequest = new UpdateAlertRuleRequest(
            Name: "Updated Name",
            EventType: "Flood",
            SeverityThreshold: "High",
            AreaWkt: null,
            IsActive: false,
            MinFwi: null,
            MaxFwi: null,
            MinWindSpeed: null,
            MinTemperature: null,
            SeasonStartMonth: null,
            SeasonEndMonth: null,
            AreaKm2Threshold: null,
            ConsecutiveCount: null,
            EscalationMinutes: null,
            NotifyRoles: null);
        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.Update, ruleId, null, updateRequest);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.EventType.Should().Be("Flood");
        result.SeverityThreshold.Should().Be("High");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRule_NonExistent_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbName = $"update_rule_notfound_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateAlertRuleRequest(
            Name: "Updated", EventType: null, SeverityThreshold: null, AreaWkt: null, IsActive: null,
            MinFwi: null, MaxFwi: null, MinWindSpeed: null, MinTemperature: null,
            SeasonStartMonth: null, SeasonEndMonth: null, AreaKm2Threshold: null,
            ConsecutiveCount: null, EscalationMinutes: null, NotifyRoles: null);
        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.Update, nonExistentId, null, updateRequest);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command, CancellationToken.None));
    }

    #endregion

    #region DeleteRule Tests

    [Fact]
    public async Task DeleteRule_ExistingRule_DeletesSuccessfully()
    {
        // Arrange
        var dbName = $"delete_rule_valid_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);

        var ruleId = Guid.NewGuid();
        db.AlertRules.Add(new AlertRule
        {
            Id = ruleId,
            Name = "Rule to Delete",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.Delete, ruleId, null, null);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ruleId);

        var deleted = await db.AlertRules.FirstOrDefaultAsync(r => r.Id == ruleId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRule_NonExistent_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbName = $"delete_rule_notfound_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);
        var nonExistentId = Guid.NewGuid();
        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.Delete, nonExistentId, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command, CancellationToken.None));
    }

    #endregion

    #region ToggleActive Tests

    [Fact]
    public async Task ToggleActive_FromActive_ToInactive()
    {
        // Arrange
        var dbName = $"toggle_active_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);

        var ruleId = Guid.NewGuid();
        db.AlertRules.Add(new AlertRule
        {
            Id = ruleId,
            Name = "Toggle Rule",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.ToggleActive, ruleId, null, null);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActive_FromInactive_ToActive()
    {
        // Arrange
        var dbName = $"toggle_inactive_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);

        var ruleId = Guid.NewGuid();
        db.AlertRules.Add(new AlertRule
        {
            Id = ruleId,
            Name = "Toggle Rule 2",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var command = new ConfigureAlertRulesCommand(
            ConfigureAlertRulesAction.ToggleActive, ruleId, null, null);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActive_MultipleTimes_TogglesCorrectly()
    {
        // Arrange
        var dbName = $"toggle_multiple_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var evaluationEngine = CreateEvaluationEngineMock();
        var logger = CreateLoggerMock();
        var handler = new ConfigureAlertRulesHandler(db, evaluationEngine.Object, logger.Object);

        var ruleId = Guid.NewGuid();
        db.AlertRules.Add(new AlertRule
        {
            Id = ruleId,
            Name = "Multiple Toggle",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act - Toggle 3 times
        var result1 = await handler.HandleAsync(
            new ConfigureAlertRulesCommand(ConfigureAlertRulesAction.ToggleActive, ruleId, null, null),
            CancellationToken.None);
        var result2 = await handler.HandleAsync(
            new ConfigureAlertRulesCommand(ConfigureAlertRulesAction.ToggleActive, ruleId, null, null),
            CancellationToken.None);
        var result3 = await handler.HandleAsync(
            new ConfigureAlertRulesCommand(ConfigureAlertRulesAction.ToggleActive, ruleId, null, null),
            CancellationToken.None);

        // Assert
        result1.IsActive.Should().BeFalse();
        result2.IsActive.Should().BeTrue();
        result3.IsActive.Should().BeFalse();
    }

    #endregion
}
