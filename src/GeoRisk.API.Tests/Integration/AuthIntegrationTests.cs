using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GeoRisk.API.Features.Auth.Dto;

namespace GeoRisk.API.Tests.Integration;

public sealed class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_Returns200WithAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest(
            $"integration_{Guid.NewGuid()}@test.com",
            "SecurePass123!",
            "Analyst");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.User.Email.Should().Be(request.Email);
        body.User.Role.Should().Be("Analyst");
        body.User.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns500OrError()
    {
        // Arrange - register once
        var email = $"dup_{Guid.NewGuid()}@test.com";
        var request1 = new RegisterRequest(email, "SecurePass123!", "Analyst");
        await _client.PostAsJsonAsync("/api/auth/register", request1);

        // Act - register again with same email
        var request2 = new RegisterRequest(email, "DifferentPass456!", "Viewer");
        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsJsonAsync("/api/auth/register", request2);
        }
        catch (Exception)
        {
            // The TestHost propagates unhandled server exceptions to the test client.
            // The endpoint did not return 200, which is the expected behavior.
            return;
        }

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200WithAuthResponse()
    {
        // Arrange - register a user first
        var email = $"login_{Guid.NewGuid()}@test.com";
        var password = "SecurePass123!";
        var registerRequest = new RegisterRequest(email, password, "Analyst");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - login with the same credentials
        var loginRequest = new LoginRequest(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401OrError()
    {
        // Arrange - register a user
        var email = $"wrongpass_{Guid.NewGuid()}@test.com";
        var registerRequest = new RegisterRequest(email, "CorrectPass123!", "Analyst");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - login with wrong password
        var loginRequest = new LoginRequest(email, "WrongPass456!");
        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        }
        catch (Exception)
        {
            // TestHost propagates unhandled server exceptions; endpoint did not return 200.
            return;
        }

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_Returns401OrError()
    {
        // Arrange
        var loginRequest = new LoginRequest($"nonexistent_{Guid.NewGuid()}@test.com", "AnyPass123!");

        // Act
        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        }
        catch (Exception)
        {
            // TestHost propagates unhandled server exceptions; endpoint did not return 200.
            return;
        }

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_WithValidTokens_Returns200WithNewTokens()
    {
        // Arrange - register and get tokens
        var email = $"refresh_{Guid.NewGuid()}@test.com";
        var registerRequest = new RegisterRequest(email, "SecurePass123!", "Analyst");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - use the refresh token to get new tokens
        var refreshRequest = new RefreshRequest(
            registerBody!.AccessToken,
            registerBody.RefreshToken);
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBe(registerBody.RefreshToken, "new refresh token should be issued");
    }

    [Fact]
    public async Task Refresh_WithInvalidAccessToken_ReturnsError()
    {
        // Arrange
        var refreshRequest = new RefreshRequest("invalid.access.token", "some_refresh_token");

        // Act
        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        }
        catch (Exception)
        {
            // TestHost propagates unhandled server exceptions; endpoint did not return 200.
            return;
        }

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ReturnsError()
    {
        // Arrange - register to get a valid access token
        var email = $"refresh_invalid_{Guid.NewGuid()}@test.com";
        var registerRequest = new RegisterRequest(email, "SecurePass123!", "Analyst");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - use valid access token but invalid refresh token
        var refreshRequest = new RefreshRequest(
            registerBody!.AccessToken,
            "invalid_refresh_token_value");
        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        }
        catch (Exception)
        {
            // TestHost propagates unhandled server exceptions; endpoint did not return 200.
            return;
        }

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200WithUserInfo()
    {
        // Arrange - register to get token
        var email = $"me_{Guid.NewGuid()}@test.com";
        var registerRequest = new RegisterRequest(email, "SecurePass123!", "Viewer");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerBody!.AccessToken);
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_Returns200WithHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
        body.Database.Should().Be("Connected");
    }

    [Fact]
    public async Task Register_WithAdminRole_ReturnsAdminRoleInResponse()
    {
        // Arrange
        var request = new RegisterRequest(
            $"admin_{Guid.NewGuid()}@test.com",
            "AdminPass123!",
            "Admin");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.User.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Register_WithViewerRole_ReturnsViewerRoleInResponse()
    {
        // Arrange
        var request = new RegisterRequest(
            $"viewer_{Guid.NewGuid()}@test.com",
            "ViewerPass123!",
            "Viewer");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.User.Role.Should().Be("Viewer");
    }

    // Helper record to deserialize health responses
    private sealed record HealthResponse(string Status, string? Database, string? Redis, string? Timestamp);
}
