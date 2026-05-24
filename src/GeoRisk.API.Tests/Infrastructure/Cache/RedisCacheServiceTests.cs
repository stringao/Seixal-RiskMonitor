using System.Text.Json;
using FluentAssertions;
using GeoRisk.API.Infrastructure.Cache;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace GeoRisk.API.Tests.Infrastructure.Cache;

public sealed class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _db = new();

    public RedisCacheServiceTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_db.Object);
    }

    private RedisCacheService CreateSut() => new(_redis.Object);

    [Fact]
    public async Task GetAsync_ReturnsDeserializedValue_WhenKeyExists()
    {
        // Arrange
        var expected = new TestData { Name = "test" };
        var json = JsonSerializer.Serialize(expected);
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)json);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAsync<TestData>("key");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenKeyIsMissing()
    {
        // Arrange
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAsync<TestData>("missing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_CallsStringSetWithSerializedValueAndTtl()
    {
        // Arrange
        var value = new TestData { Name = "cached" };
        var ttl = TimeSpan.FromMinutes(5);
        _db.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        await sut.SetAsync("key", value, ttl);

        // Assert
        _db.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.Is<RedisValue>(v => v == JsonSerializer.Serialize(value)),
            ttl,
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_CallsKeyDelete()
    {
        // Arrange
        _db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        await sut.RemoveAsync("key");

        // Assert
        _db.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k == "key"),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    private sealed class TestData
    {
        public string Name { get; set; } = string.Empty;
    }
}
