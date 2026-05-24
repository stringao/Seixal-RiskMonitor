using FluentAssertions;
using FluentValidation.TestHelper;
using GeoRisk.API.Features.Auth.Dto;

namespace GeoRisk.API.Tests.Features.Auth.Dto;

public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    [Fact]
    public async Task Validate_ValidData_Passes()
    {
        // Arrange
        var request = new RegisterRequest("user@example.com", "SecurePass123!", "Analyst");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyEmail_Fails(string? email)
    {
        // Arrange
        var request = new RegisterRequest(email!, "SecurePass123!", "Analyst");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign")]
    [InlineData("@missing-local.com")]
    [InlineData("missing-domain@")]
    public async Task Validate_InvalidEmail_Fails(string email)
    {
        // Arrange
        var request = new RegisterRequest(email, "SecurePass123!", "Analyst");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task Validate_ShortOrEmptyPassword_Fails(string? password)
    {
        // Arrange
        var request = new RegisterRequest("user@example.com", password!, "Analyst");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_PasswordWithExactly8Characters_Passes()
    {
        // Arrange
        var request = new RegisterRequest("user@example.com", "12345678", "Analyst");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("SuperAdmin")]
    [InlineData("Manager")]
    [InlineData("admin")]
    [InlineData("analyst")]
    public async Task Validate_InvalidRole_Fails(string? role)
    {
        // Arrange
        var request = new RegisterRequest("user@example.com", "SecurePass123!", role!);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Analyst")]
    [InlineData("Viewer")]
    public async Task Validate_ValidRoles_Pass(string role)
    {
        // Arrange
        var request = new RegisterRequest("user@example.com", "SecurePass123!", role);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }
}

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public async Task Validate_ValidData_Passes()
    {
        // Arrange
        var request = new LoginRequest("user@example.com", "SecurePass123!");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyEmail_Fails(string? email)
    {
        // Arrange
        var request = new LoginRequest(email!, "SecurePass123!");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@domain.com")]
    public async Task Validate_InvalidEmail_Fails(string email)
    {
        // Arrange
        var request = new LoginRequest(email, "SecurePass123!");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyPassword_Fails(string? password)
    {
        // Arrange
        var request = new LoginRequest("user@example.com", password!);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

public sealed class RefreshRequestValidatorTests
{
    private readonly RefreshRequestValidator _sut = new();

    [Fact]
    public async Task Validate_ValidData_Passes()
    {
        // Arrange
        var request = new RefreshRequest("some.access.token", "some.refresh.token");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyAccessToken_Fails(string? accessToken)
    {
        // Arrange
        var request = new RefreshRequest(accessToken!, "some.refresh.token");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyRefreshToken_Fails(string? refreshToken)
    {
        // Arrange
        var request = new RefreshRequest("some.access.token", refreshToken!);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public async Task Validate_BothFieldsEmpty_FailsWithBothErrors()
    {
        // Arrange
        var request = new RefreshRequest(string.Empty, string.Empty);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
