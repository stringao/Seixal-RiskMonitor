using BCrypt.Net;
using FluentAssertions;
using GeoRisk.API.Auth;

namespace GeoRisk.API.Tests.Auth;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_ReturnsNonEmptyStringDifferentFromInput()
    {
        // Arrange
        const string password = "MySecurePassword123!";

        // Act
        var hash = _sut.Hash(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void Hash_ReturnsDifferentHashesForSamePassword()
    {
        // Arrange
        const string password = "MySecurePassword123!";

        // Act
        var hash1 = _sut.Hash(password);
        var hash2 = _sut.Hash(password);

        // Assert
        hash1.Should().NotBe(hash2, "BCrypt generates unique salts per hash");
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        // Arrange
        const string password = "MySecurePassword123!";
        var hash = _sut.Hash(password);

        // Act
        var result = _sut.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongPassword()
    {
        // Arrange
        const string correctPassword = "MySecurePassword123!";
        const string wrongPassword = "WrongPassword456!";
        var hash = _sut.Hash(correctPassword);

        // Act
        var result = _sut.Verify(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_ReturnsFalseForEmptyPassword()
    {
        // Arrange
        const string password = "MySecurePassword123!";
        var hash = _sut.Hash(password);

        // Act
        var result = _sut.Verify(string.Empty, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_ThrowsForInvalidHash()
    {
        // Arrange
        const string password = "MySecurePassword123!";
        const string invalidHash = "not-a-valid-bcrypt-hash";

        // Act
        var act = () => _sut.Verify(password, invalidHash);

        // Assert
        act.Should().Throw<SaltParseException>();
    }

    [Fact]
    public void Hash_ProducesBCryptFormattedOutput()
    {
        // Arrange
        const string password = "TestPassword";

        // Act
        var hash = _sut.Hash(password);

        // Assert
        // BCrypt hashes start with $2a$, $2b$, or $2y$
        hash.Should().StartWith("$2");
        hash.Should().Contain("$");
    }
}
