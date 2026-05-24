using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using GeoRisk.API.Auth;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;

namespace GeoRisk.API.Tests.Auth;

public sealed class JwtServiceTests
{
    private static JwtOptions CreateTestOptions(int expirationMinutes = 15) => new()
    {
        Issuer = "GeoRisk.API",
        Audience = "GeoRisk.Web",
        SecretKey = "TestKey_MustBeAtLeast32CharsLong!!",
        AccessTokenExpirationMinutes = expirationMinutes,
        RefreshTokenExpirationDays = 7
    };

    private static JwtOptions CreateTestOptionsWithKey(string secretKey) => new()
    {
        Issuer = "GeoRisk.API",
        Audience = "GeoRisk.Web",
        SecretKey = secretKey,
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private static User CreateTestUser(UserRole role = UserRole.Analyst) => new()
    {
        Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        Email = "testuser@georisk.pt",
        Role = role
    };

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtWithCorrectClaims()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);
        var user = CreateTestUser();

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be(options.Issuer);
        jwt.Audiences.Should().Contain(options.Audience);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAtCorrectTime()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);
        var user = CreateTestUser();
        var beforeGenerating = DateTime.UtcNow;

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var expectedExpiry = beforeGenerating.AddMinutes(options.AccessTokenExpirationMinutes);
        var tolerance = TimeSpan.FromSeconds(5);

        jwt.ValidTo.Should().BeCloseTo(expectedExpiry, tolerance);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64StringOfCorrectLength()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);

        // Act
        var refreshToken = service.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();

        // 64 random bytes encoded as base64 produces 88 characters
        var decodedBytes = Convert.FromBase64String(refreshToken);
        decodedBytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);

        // Act
        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsClaimsFromExpiredToken()
    {
        // Arrange
        var expiredService = new JwtService(CreateTestOptions(-1));
        var user = CreateTestUser();

        // Generate a token that is already expired (expiration = -1 minutes from now)
        var expiredToken = expiredService.GenerateAccessToken(user);

        // Create a new service instance with normal options for validation
        var validationService = new JwtService(CreateTestOptions());

        // Act
        var principal = validationService.GetPrincipalFromExpiredToken(expiredToken);

        // Assert
        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Value == user.Id.ToString());
        principal.Claims.Should().Contain(c => c.Value == user.Email);
        principal.Claims.Should().Contain(c => c.Value == user.Role.ToString());
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsClaimsFromValidToken()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);
        var user = CreateTestUser();
        var token = service.GenerateAccessToken(user);

        // Act
        var principal = service.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Value == user.Id.ToString());
        principal.Claims.Should().Contain(c => c.Value == user.Email);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsNullForInvalidToken()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);

        // Act
        var principal = service.GetPrincipalFromExpiredToken("this.is.not.a.valid.jwt");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsNullForEmptyString()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);

        // Act
        var principal = service.GetPrincipalFromExpiredToken(string.Empty);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsNullForTokenWithWrongSignature()
    {
        // Arrange
        var correctOptions = CreateTestOptions();
        var correctService = new JwtService(correctOptions);
        var user = CreateTestUser();
        var token = correctService.GenerateAccessToken(user);

        var wrongService = new JwtService(CreateTestOptionsWithKey("DifferentKey_ThatIsAtLeast32CharsLong!!"));

        // Act
        var principal = wrongService.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GenerateAccessToken_IncludesJtiClaim()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);
        var user = CreateTestUser();

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        var jtiValue = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Guid.TryParse(jtiValue, out _).Should().BeTrue("JTI should be a valid GUID");
    }

    [Fact]
    public void GenerateAccessToken_ProducesDifferentJtisForSameUser()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);
        var user = CreateTestUser();

        // Act
        var token1 = service.GenerateAccessToken(user);
        var token2 = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt1 = handler.ReadJwtToken(token1);
        var jwt2 = handler.ReadJwtToken(token2);

        var jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    [Fact]
    public void GenerateAccessToken_WithAdminRole_ContainsAdminRoleClaim()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);
        var user = CreateTestUser(UserRole.Admin);

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateAccessToken_WithViewerRole_ContainsViewerRoleClaim()
    {
        // Arrange
        var options = CreateTestOptions();
        var service = new JwtService(options);
        var user = CreateTestUser(UserRole.Viewer);

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Viewer");
    }
}
