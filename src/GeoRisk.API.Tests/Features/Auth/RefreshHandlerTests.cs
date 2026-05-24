using System.Security.Claims;
using FluentAssertions;
using GeoRisk.API.Auth;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Features.Auth;
using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GeoRisk.API.Tests.Features.Auth;

public sealed class RefreshHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    private static Mock<IJwtService> CreateJwtMock(out ClaimsPrincipal principal)
    {
        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh_token");
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("new_access_token");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
            new("sub", "00000000-0000-0000-0000-000000000001")
        };
        principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        jwt.Setup(j => j.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        return jwt;
    }

    private static async Task<(GeoRiskDbContext db, User user, RefreshToken refreshToken)> SeedUserAndToken(
        string dbName,
        string refreshTokenValue = "valid_refresh_token",
        bool isRevoked = false,
        DateTime? expiresAt = null)
    {
        var db = CreateDbContext(dbName);
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var user = new User
        {
            Id = userId,
            Email = "refresh@example.com",
            PasswordHash = "hashed",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenValue,
            UserId = userId,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        return (db, user, refreshToken);
    }

    [Fact]
    public async Task HandleAsync_WithValidRefreshToken_ReturnsNewAuthResponse()
    {
        // Arrange
        var dbName = $"refresh_valid_{Guid.NewGuid()}";
        var (db, user, _) = await SeedUserAndToken(dbName);
        var jwt = CreateJwtMock(out _);

        var handler = new RefreshHandler(db, jwt.Object);
        var cmd = new RefreshCommand("expired_access_token", "valid_refresh_token");

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
        result.User.Email.Should().Be("refresh@example.com");
        result.User.Role.Should().Be("Admin");
        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task HandleAsync_WithValidRefreshToken_RevokesOldTokenAndCreatesNew()
    {
        // Arrange
        var dbName = $"refresh_revoke_{Guid.NewGuid()}";
        var (db, _, originalToken) = await SeedUserAndToken(dbName);
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var jwt = CreateJwtMock(out _);

        var handler = new RefreshHandler(db, jwt.Object);
        var cmd = new RefreshCommand("expired_access_token", "valid_refresh_token");

        // Act
        await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        var oldToken = await db.RefreshTokens.FindAsync(originalToken.Id);
        oldToken.Should().NotBeNull();
        oldToken!.IsRevoked.Should().BeTrue();

        var newToken = await db.RefreshTokens.FirstOrDefaultAsync(
            rt => rt.UserId == userId && rt.Token == "new_refresh_token");
        newToken.Should().NotBeNull();
        newToken!.IsRevoked.Should().BeFalse();
        newToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        newToken.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_WithInvalidAccessToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dbName = $"refresh_invalid_access_{Guid.NewGuid()}";
        var (db, _, _) = await SeedUserAndToken(dbName);
        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.GetPrincipalFromExpiredToken("bad_token")).Returns((ClaimsPrincipal?)null);

        var handler = new RefreshHandler(db, jwt.Object);
        var cmd = new RefreshCommand("bad_token", "valid_refresh_token");

        // Act
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid access token*");
    }

    [Fact]
    public async Task HandleAsync_WithExpiredRefreshToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dbName = $"refresh_expired_{Guid.NewGuid()}";
        var (db, _, _) = await SeedUserAndToken(
            dbName, expiresAt: DateTime.UtcNow.AddDays(-1));
        var jwt = CreateJwtMock(out _);

        var handler = new RefreshHandler(db, jwt.Object);
        var cmd = new RefreshCommand("expired_access_token", "valid_refresh_token");

        // Act
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token expired or revoked*");
    }

    [Fact]
    public async Task HandleAsync_WithRevokedRefreshToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dbName = $"refresh_revoked_{Guid.NewGuid()}";
        var (db, _, _) = await SeedUserAndToken(
            dbName, isRevoked: true);
        var jwt = CreateJwtMock(out _);

        var handler = new RefreshHandler(db, jwt.Object);
        var cmd = new RefreshCommand("expired_access_token", "valid_refresh_token");

        // Act
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token expired or revoked*");
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentRefreshToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dbName = $"refresh_notfound_{Guid.NewGuid()}";
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var db = CreateDbContext(dbName);
        db.Users.Add(new User
        {
            Id = userId,
            Email = "orphan@example.com",
            PasswordHash = "hashed",
            Role = UserRole.Viewer,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var jwt = CreateJwtMock(out _);

        var handler = new RefreshHandler(db, jwt.Object);
        var cmd = new RefreshCommand("expired_access_token", "nonexistent_token");

        // Act
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token*");
    }

    [Fact]
    public async Task HandleAsync_WithTokenBelongingToDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dbName = $"refresh_wrong_user_{Guid.NewGuid()}";
        var (db, _, _) = await SeedUserAndToken(dbName);

        // Create a ClaimsPrincipal with a different user ID
        var differentUserId = Guid.NewGuid();
        var differentClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, differentUserId.ToString()),
            new("sub", differentUserId.ToString())
        };
        var differentPrincipal = new ClaimsPrincipal(new ClaimsIdentity(differentClaims, "TestAuth"));

        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(differentPrincipal);

        var handler = new RefreshHandler(db, jwt.Object);
        var cmd = new RefreshCommand("expired_access_token", "valid_refresh_token");

        // Act
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token*");
    }

    [Fact]
    public async Task HandleAsync_WithAccessTokenMissingUserIdClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dbName = $"refresh_no_userid_{Guid.NewGuid()}";
        var (db, _, _) = await SeedUserAndToken(dbName);

        var emptyClaims = new List<Claim>();
        var emptyPrincipal = new ClaimsPrincipal(new ClaimsIdentity(emptyClaims, "TestAuth"));

        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(emptyPrincipal);

        var handler = new RefreshHandler(db, jwt.Object);
        var cmd = new RefreshCommand("expired_access_token", "valid_refresh_token");

        // Act
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid token claims*");
    }
}
