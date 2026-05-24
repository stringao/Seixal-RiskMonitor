using FluentAssertions;
using FluentValidation.TestHelper;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.Events.Dto;

namespace GeoRisk.API.Tests.Features.Events.Dto;

public sealed class CreateEventRequestValidatorTests
{
    private readonly CreateEventRequestValidator _sut = new();

    [Fact]
    public async Task Validate_ValidData_Passes()
    {
        // Arrange
        var request = new CreateEventRequest(
            EventType.Fire, "Test Event", "Description",
            38.64, -9.10, RiskLevel.High,
            EventSource.Manual, DateTime.UtcNow, null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyTitle_Fails()
    {
        // Arrange
        var request = new CreateEventRequest(
            EventType.Fire, "", "Description",
            38.64, -9.10, RiskLevel.High,
            EventSource.Manual, DateTime.UtcNow, null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Validate_InvalidLatitude_Fails()
    {
        // Arrange
        var request = new CreateEventRequest(
            EventType.Fire, "Title", "Description",
            91, -9.10, RiskLevel.High,
            EventSource.Manual, DateTime.UtcNow, null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Fact]
    public async Task Validate_InvalidLongitude_Fails()
    {
        // Arrange
        var request = new CreateEventRequest(
            EventType.Fire, "Title", "Description",
            38.64, 200, RiskLevel.High,
            EventSource.Manual, DateTime.UtcNow, null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Longitude);
    }
}

public sealed class ImportEventItemValidatorTests
{
    private readonly ImportEventItemValidator _sut = new();

    [Fact]
    public async Task Validate_ValidData_Passes()
    {
        // Arrange
        var item = new ImportEventItem(
            "SRC-001", EventType.Flood, "Imported Event",
            38.64, -9.10, RiskLevel.Medium,
            EventSource.IPMA, DateTime.UtcNow, "Description");

        // Act
        var result = await _sut.TestValidateAsync(item);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptySourceId_Fails()
    {
        // Arrange
        var item = new ImportEventItem(
            "", EventType.Flood, "Imported Event",
            38.64, -9.10, RiskLevel.Medium,
            EventSource.IPMA, DateTime.UtcNow, "Description");

        // Act
        var result = await _sut.TestValidateAsync(item);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SourceId);
    }
}
