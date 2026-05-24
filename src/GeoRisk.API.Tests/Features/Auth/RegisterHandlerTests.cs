using FluentAssertions;
using GeoRisk.API.Auth;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.Auth;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GeoRisk.API.Tests.Features.Auth;

public sealed class RegisterHandlerTests
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

        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_password");
        jwt.Setup(j => j.GenerateRefreshToken()).Returns("test_refresh_token");
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("test_access_token");

        return (hasher, jwt);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var dbName = $"register_valid_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var (hasher, jwt) = CreateMocks();
        var handler = new RegisterHandler(db, hasher.Object, jwt.Object);
        var cmd = new RegisterCommand("test@example.com", "P@ssw0rd!", "Analyst");

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_access_token");
        result.RefreshToken.Should().Be("test_refresh_token");
        result.User.Email.Should().Be("test@example.com");
        result.User.Role.Should().Be("Analyst");
        result.User.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_PersistsUserInDatabase()
    {
        // Arrange
        var dbName = $"register_persist_user_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var (hasher, jwt) = CreateMocks();
        var handler = new RegisterHandler(db, hasher.Object, jwt.Object);
        var cmd = new RegisterCommand("persist@example.com", "P@ssw0rd!", "Analyst");

        // Act
        await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "persist@example.com");
        user.Should().NotBeNull();
        user!.Email.Should().Be("persist@example.com");
        user.Role.Should().Be(UserRole.Analyst);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbName = $"register_duplicate_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var (hasher, jwt) = CreateMocks();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            PasswordHash = "existing_hash",
            Role = UserRole.Viewer,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new RegisterHandler(db, hasher.Object, jwt.Object);
        var cmd = new RegisterCommand("existing@example.com", "P@ssw0rd!", "Analyst");

        // Act
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already registered*");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidRole_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbName = $"register_invalid_role_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var (hasher, jwt) = CreateMocks();
        var handler = new RegisterHandler(db, hasher.Object, jwt.Object);
        var cmd = new RegisterCommand("new@example.com", "P@ssw0rd!", "SuperAdmin");

        // Act
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid role:*");
    }

    [Fact]
    public async Task HandleAsync_HashesPassword_DoesNotStorePlainText()
    {
        // Arrange
        var dbName = $"register_hash_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var (hasher, jwt) = CreateMocks();
        var handler = new RegisterHandler(db, hasher.Object, jwt.Object);
        var plainPassword = "MySecretP@ss123";
        var cmd = new RegisterCommand("hash@example.com", plainPassword, "Viewer");

        // Act
        await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        hasher.Verify(h => h.Hash(plainPassword), Times.Once);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "hash@example.com");
        user.Should().NotBeNull();
        user!.PasswordHash.Should().Be("hashed_password");
        user.PasswordHash.Should().NotBe(plainPassword);
    }

    [Fact]
    public async Task HandleAsync_CreatesAndStoresRefreshToken()
    {
        // Arrange
        var dbName = $"register_refresh_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var (hasher, jwt) = CreateMocks();
        var handler = new RegisterHandler(db, hasher.Object, jwt.Object);
        var cmd = new RegisterCommand("refresh@example.com", "P@ssw0rd!", "Admin");

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        jwt.Verify(j => j.GenerateRefreshToken(), Times.Once);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "refresh@example.com");
        user.Should().NotBeNull();

        var refreshToken = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user!.Id);
        refreshToken.Should().NotBeNull();
        refreshToken!.Token.Should().Be("test_refresh_token");
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        refreshToken.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ReturnsMatchingUserIdsAcrossTokensAndResponse()
    {
        // Arrange
        var dbName = $"register_consistency_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var (hasher, jwt) = CreateMocks();
        var handler = new RegisterHandler(db, hasher.Object, jwt.Object);
        var cmd = new RegisterCommand("consistency@example.com", "P@ssw0rd!", "Analyst");

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "consistency@example.com");
        var refreshToken = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user!.Id);

        result.User.Id.Should().Be(user!.Id);
        refreshToken!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyEmail_StillProcessesBecauseValidationIsNotInHandler()
    {
        // Arrange - The handler does not validate email format; that is a concern for the endpoint layer.
        // This test confirms the handler will attempt to create a user regardless.
        var dbName = $"register_empty_email_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var (hasher, jwt) = CreateMocks();
        var handler = new RegisterHandler(db, hasher.Object, jwt.Object);
        var cmd = new RegisterCommand("", "P@ssw0rd!", "Analyst");

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.User.Email.Should().BeEmpty();
    }
}
