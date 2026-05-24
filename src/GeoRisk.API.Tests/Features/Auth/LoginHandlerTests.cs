using FluentAssertions;
using GeoRisk.API.Auth;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.Auth;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GeoRisk.API.Tests.Features.Auth;

public sealed class LoginHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    private static (Mock<IPasswordHasher> hasher, Mock<IJwtService> jwt) CreateMocks()
    {
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtService>();

        jwt.Setup(j => j.GenerateRefreshToken()).Returns("test_refresh_token");
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("test_access_token");

        return (hasher, jwt);
    }

    private static async Task<(GeoRiskDbContext db, User user)> SeedUser(
        string dbName, string email = "login@example.com", string passwordHash = "hashed_password")
    {
        var db = CreateDbContext(dbName);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            Role = UserRole.Analyst,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (db, user);
    }

    [Fact]
    public async Task HandleAsync_WithCorrectCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var dbName = $"login_success_{Guid.NewGuid()}";
        var (db, user) = await SeedUser(dbName);
        var (hasher, jwt) = CreateMocks();
        hasher.Setup(h => h.Verify("correct_password", "hashed_password")).Returns(true);

        var handler = new LoginHandler(db, hasher.Object, jwt.Object);
        var query = new LoginQuery("login@example.com", "correct_password");

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_access_token");
        result.RefreshToken.Should().Be("test_refresh_token");
        result.User.Email.Should().Be("login@example.com");
        result.User.Role.Should().Be("Analyst");
        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task HandleAsync_WithWrongEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dbName = $"login_wrong_email_{Guid.NewGuid()}";
        var (db, _) = await SeedUser(dbName);
        var (hasher, jwt) = CreateMocks();

        var handler = new LoginHandler(db, hasher.Object, jwt.Object);
        var query = new LoginQuery("nonexistent@example.com", "any_password");

        // Act
        var act = () => handler.HandleAsync(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials*");
    }

    [Fact]
    public async Task HandleAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dbName = $"login_wrong_password_{Guid.NewGuid()}";
        var (db, _) = await SeedUser(dbName);
        var (hasher, jwt) = CreateMocks();
        hasher.Setup(h => h.Verify("wrong_password", "hashed_password")).Returns(false);

        var handler = new LoginHandler(db, hasher.Object, jwt.Object);
        var query = new LoginQuery("login@example.com", "wrong_password");

        // Act
        var act = () => handler.HandleAsync(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials*");

        hasher.Verify(h => h.Verify("wrong_password", "hashed_password"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OnSuccessfulLogin_UpdatesLastLoginAt()
    {
        // Arrange
        var dbName = $"login_lastlogin_{Guid.NewGuid()}";
        var (db, user) = await SeedUser(dbName);
        user.LastLoginAt.Should().BeNull();

        var (hasher, jwt) = CreateMocks();
        hasher.Setup(h => h.Verify("password123", "hashed_password")).Returns(true);

        var handler = new LoginHandler(db, hasher.Object, jwt.Object);
        var query = new LoginQuery("login@example.com", "password123");

        // Act
        await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        var updatedUser = await db.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.LastLoginAt.Should().NotBeNull();
        updatedUser.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_OnSuccessfulLogin_CreatesRefreshToken()
    {
        // Arrange
        var dbName = $"login_refresh_{Guid.NewGuid()}";
        var (db, user) = await SeedUser(dbName);
        var (hasher, jwt) = CreateMocks();
        hasher.Setup(h => h.Verify("password123", "hashed_password")).Returns(true);

        var handler = new LoginHandler(db, hasher.Object, jwt.Object);
        var query = new LoginQuery("login@example.com", "password123");

        // Act
        await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        var refreshToken = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user.Id);
        refreshToken.Should().NotBeNull();
        refreshToken!.Token.Should().Be("test_refresh_token");
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        refreshToken.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task HandleAsync_DoesNotVerifyPassword_WhenUserNotFound()
    {
        // Arrange
        var dbName = $"login_no_verify_{Guid.NewGuid()}";
        var (db, _) = await SeedUser(dbName);
        var (hasher, jwt) = CreateMocks();

        var handler = new LoginHandler(db, hasher.Object, jwt.Object);
        var query = new LoginQuery("nonexistent@example.com", "any_password");

        // Act
        try
        {
            await handler.HandleAsync(query, CancellationToken.None);
        }
        catch (UnauthorizedAccessException)
        {
            // Expected
        }

        // Assert - Verify should never be called since user was not found
        hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
