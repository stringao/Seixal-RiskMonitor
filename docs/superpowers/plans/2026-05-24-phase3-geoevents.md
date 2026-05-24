# Phase 3: Core Domain - GeoEvents Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement GeoEvents CRUD, spatial queries via PostGIS, Redis caching, and seed data.

**Architecture:** Vertical Slice with hand-rolled CQRS. Each feature is a self-contained file with Command/Query record + Handler + Endpoint extension method. Handlers resolved via Scrutor assembly scanning. Spatial filters are optional query params on a single GET /api/events endpoint.

**Tech Stack:** .NET 10, EF Core with Npgsql + NetTopologySuite, StackExchange.Redis, FluentValidation, xUnit + FluentAssertions + Moq

---

## File Structure

| File | Responsibility |
|------|---------------|
| `src/GeoRisk.API/Infrastructure/Cache/ICacheService.cs` | Cache abstraction interface |
| `src/GeoRisk.API/Infrastructure/Cache/RedisCacheService.cs` | Redis implementation |
| `src/GeoRisk.API/Features/Events/Dto/EventDtos.cs` | All Event DTOs |
| `src/GeoRisk.API/Features/Events/Dto/EventDtoValidators.cs` | FluentValidation for DTOs |
| `src/GeoRisk.API/Features/Events/CreateEvent.cs` | POST /api/events |
| `src/GeoRisk.API/Features/Events/GetEvents.cs` | GET /api/events with all filters |
| `src/GeoRisk.API/Features/Events/GetEventById.cs` | GET /api/events/{id} |
| `src/GeoRisk.API/Features/Events/ImportEvents.cs` | POST /api/events/import |
| `src/GeoRisk.API/Infrastructure/Persistence/SeedData.cs` | ~20 sample events |
| `src/GeoRisk.API/Program.cs` | Register cache, map event endpoints |
| `src/GeoRisk.API.Tests/Features/Events/CreateEventHandlerTests.cs` | Create handler unit tests |
| `src/GeoRisk.API.Tests/Features/Events/GetEventsHandlerTests.cs` | GetEvents handler unit tests |
| `src/GeoRisk.API.Tests/Features/Events/GetEventByIdHandlerTests.cs` | GetById handler unit tests |
| `src/GeoRisk.API.Tests/Features/Events/ImportEventsHandlerTests.cs` | Import handler unit tests |
| `src/GeoRisk.API.Tests/Infrastructure/Cache/RedisCacheServiceTests.cs` | Cache service unit tests |

---

### Task 1: Add Redis NuGet Package

**Files:**
- Modify: `src/GeoRisk.API/GeoRisk.API.csproj`

- [ ] **Step 1: Add StackExchange.Redis package**

Run:
```bash
cd src/GeoRisk.API && dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/GeoRisk.API/GeoRisk.API.csproj`
Expected: BUILD SUCCEEDS

- [ ] **Step 3: Commit**

```bash
git add src/GeoRisk.API/GeoRisk.API.csproj
git commit -m "chore: add StackExchange.Redis cache package for Phase 3"
```

---

### Task 2: ICacheService Interface

**Files:**
- Create: `src/GeoRisk.API/Infrastructure/Cache/ICacheService.cs`

- [ ] **Step 1: Create the interface**

```csharp
namespace GeoRisk.API.Infrastructure.Cache;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/GeoRisk.API/GeoRisk.API.csproj`
Expected: BUILD SUCCEEDS

- [ ] **Step 3: Commit**

```bash
git add src/GeoRisk.API/Infrastructure/Cache/ICacheService.cs
git commit -m "feat: add ICacheService abstraction for caching"
```

---

### Task 3: RedisCacheService Implementation + Tests

**Files:**
- Create: `src/GeoRisk.API/Infrastructure/Cache/RedisCacheService.cs`
- Create: `src/GeoRisk.API.Tests/Infrastructure/Cache/RedisCacheServiceTests.cs`

- [ ] **Step 1: Write tests for RedisCacheService**

Create: `src/GeoRisk.API.Tests/Infrastructure/Cache/RedisCacheServiceTests.cs`

```csharp
using FluentAssertions;
using GeoRisk.API.Infrastructure.Cache;
using Moq;
using StackExchange.Redis;

namespace GeoRisk.API.Tests.Infrastructure.Cache;

public sealed class RedisCacheServiceTests
{
    private static (Mock<IConnectionMultiplexer> mux, Mock<IDatabase> db) CreateMocks()
    {
        var mux = new Mock<IConnectionMultiplexer>();
        var db = new Mock<IDatabase>();
        mux.Setup(m => m.GetDatabase(-1, null)).Returns(db.Object);
        return (mux, db);
    }

    [Fact]
    public async Task GetAsync_WithExistingKey_ReturnsDeserializedValue()
    {
        var (mux, db) = CreateMocks();
        var json = """{"Name":"test","Value":42}""";
        db.Setup(d => d.StringGetAsync("test:key", CommandFlags.None))
            .ReturnsAsync(RedisValue.Unbox(json));
        var service = new RedisCacheService(mux.Object);

        var result = await service.GetAsync<TestPayload>("test:key");

        result.Should().NotBeNull();
        result!.Name.Should().Be("test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetAsync_WithMissingKey_ReturnsNull()
    {
        var (mux, db) = CreateMocks();
        db.Setup(d => d.StringGetAsync("missing", CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);
        var service = new RedisCacheService(mux.Object);

        var result = await service.GetAsync<TestPayload>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithTtl_SetsValueWithExpiry()
    {
        var (mux, db) = CreateMocks();
        var service = new RedisCacheService(mux.Object);
        var payload = new TestPayload("hello", 99);
        var ttl = TimeSpan.FromMinutes(5);

        await service.SetAsync("key", payload, ttl);

        db.Verify(d => d.StringSetAsync(
            "key",
            It.IsAny<RedisValue>(),
            ttl,
            false,
            When.Always,
            CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_CallsKeyDelete()
    {
        var (mux, db) = CreateMocks();
        var service = new RedisCacheService(mux.Object);

        await service.RemoveAsync("key");

        db.Verify(d => d.KeyDeleteAsync("key", CommandFlags.None), Times.Once);
    }

    private sealed record TestPayload(string Name, int Value);
}
```

- [ ] **Step 2: Write RedisCacheService implementation**

Create: `src/GeoRisk.API/Infrastructure/Cache/RedisCacheService.cs`

```csharp
using System.Text.Json;
using StackExchange.Redis;

namespace GeoRisk.API.Infrastructure.Cache;

public sealed class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var endpoints = redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = redis.GetServer(endpoint);
            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test src/GeoRisk.API.Tests/ --filter "FullyQualifiedName~RedisCacheServiceTests" -v n`
Expected: ALL PASS

- [ ] **Step 4: Verify full build**

Run: `dotnet build src/GeoRisk.API/GeoRisk.API.csproj`
Expected: BUILD SUCCEEDS

- [ ] **Step 5: Commit**

```bash
git add src/GeoRisk.API/Infrastructure/Cache/RedisCacheService.cs src/GeoRisk.API.Tests/Infrastructure/Cache/RedisCacheServiceTests.cs
git commit -m "feat: add RedisCacheService with ICacheService implementation and tests"
```

---

### Task 4: Event DTOs and Validators

**Files:**
- Create: `src/GeoRisk.API/Features/Events/Dto/EventDtos.cs`
- Create: `src/GeoRisk.API/Features/Events/Dto/EventDtoValidators.cs`
- Create: `src/GeoRisk.API.Tests/Features/Events/Dto/EventDtoValidatorsTests.cs`

- [ ] **Step 1: Create DTOs**

Create: `src/GeoRisk.API/Features/Events/Dto/EventDtos.cs`

```csharp
namespace GeoRisk.API.Features.Events.Dto;

public sealed record CreateEventRequest(
    EventType EventType,
    string Title,
    string? Description,
    double Latitude,
    double Longitude,
    RiskLevel Severity,
    EventSource Source,
    DateTime OccurredAt,
    string? Metadata);

public sealed record EventResponse(
    Guid Id,
    EventType EventType,
    string Title,
    string? Description,
    double Latitude,
    double Longitude,
    RiskLevel Severity,
    EventSource Source,
    DateTime OccurredAt,
    string? AIClassification,
    string? AIInsight,
    DateTime CreatedAt);

public sealed record EventListResponse(
    List<EventResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ImportEventItem(
    string SourceId,
    EventType EventType,
    string Title,
    double Latitude,
    double Longitude,
    RiskLevel Severity,
    EventSource Source,
    DateTime OccurredAt,
    string? Description);

public sealed record ImportEventsResponse(
    int Imported,
    int Skipped);
```

- [ ] **Step 2: Create validators**

Create: `src/GeoRisk.API/Features/Events/Dto/EventDtoValidators.cs`

```csharp
using FluentValidation;

namespace GeoRisk.API.Features.Events.Dto;

public sealed class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}

public sealed class ImportEventItemValidator : AbstractValidator<ImportEventItem>
{
    public ImportEventItemValidator()
    {
        RuleFor(x => x.SourceId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
```

- [ ] **Step 3: Write DTO validation tests**

Create: `src/GeoRisk.API.Tests/Features/Events/Dto/EventDtoValidatorsTests.cs`

```csharp
using FluentAssertions;
using GeoRisk.API.Features.Events.Dto;

namespace GeoRisk.API.Tests.Features.Events.Dto;

public sealed class EventDtoValidatorsTests
{
    [Fact]
    public void CreateEventRequestValidator_ValidData_Passes()
    {
        var validator = new CreateEventRequestValidator();
        var dto = new CreateEventRequest(
            EventType.Fire, "Incendio", "Desc", 38.64, -9.10, RiskLevel.High, EventSource.Manual, DateTime.UtcNow, null);

        var result = validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateEventRequestValidator_EmptyTitle_Fails()
    {
        var validator = new CreateEventRequestValidator();
        var dto = new CreateEventRequest(
            EventType.Fire, "", "Desc", 38.64, -9.10, RiskLevel.High, EventSource.Manual, DateTime.UtcNow, null);

        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    [Fact]
    public void CreateEventRequestValidator_InvalidLatitude_Fails()
    {
        var validator = new CreateEventRequestValidator();
        var dto = new CreateEventRequest(
            EventType.Fire, "Title", "Desc", 91.0, -9.10, RiskLevel.High, EventSource.Manual, DateTime.UtcNow, null);

        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Latitude");
    }

    [Fact]
    public void CreateEventRequestValidator_InvalidLongitude_Fails()
    {
        var validator = new CreateEventRequestValidator();
        var dto = new CreateEventRequest(
            EventType.Fire, "Title", "Desc", 38.64, 200.0, RiskLevel.High, EventSource.Manual, DateTime.UtcNow, null);

        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Longitude");
    }

    [Fact]
    public void ImportEventItemValidator_NoSourceId_Fails()
    {
        var validator = new ImportEventItemValidator();
        var dto = new ImportEventItem(
            "", EventType.Fire, "Title", 38.64, -9.10, RiskLevel.High, EventSource.ICNF, DateTime.UtcNow, null);

        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "SourceId");
    }

    [Fact]
    public void ImportEventItemValidator_ValidData_Passes()
    {
        var validator = new ImportEventItemValidator();
        var dto = new ImportEventItem(
            "ICNF-001", EventType.Fire, "Title", 38.64, -9.10, RiskLevel.High, EventSource.ICNF, DateTime.UtcNow, null);

        var result = validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test src/GeoRisk.API.Tests/ --filter "FullyQualifiedName~EventDtoValidatorsTests" -v n`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/GeoRisk.API/Features/Events/Dto/ src/GeoRisk.API.Tests/Features/Events/Dto/
git commit -m "feat: add Event DTOs and FluentValidation validators with tests"
```

---

### Task 5: CreateEvent (Handler + Endpoint + Tests)

**Files:**
- Create: `src/GeoRisk.API/Features/Events/CreateEvent.cs`
- Create: `src/GeoRisk.API.Tests/Features/Events/CreateEventHandlerTests.cs`

- [ ] **Step 1: Write handler tests**

Create: `src/GeoRisk.API.Tests/Features/Events/CreateEventHandlerTests.cs`

```csharp
using FluentAssertions;
using GeoRisk.API.Features.Events;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GeoRisk.API.Tests.Features.Events;

public sealed class CreateEventHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesEventAndReturnsResponse()
    {
        var dbName = $"create_event_valid_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = new Mock<ICacheService>();
        var handler = new CreateEventHandler(db, cache.Object);
        var cmd = new CreateEventCommand(
            EventType.Fire, "Incendio Florestal", "Grande incendio", 38.64, -9.10,
            RiskLevel.High, EventSource.Manual, DateTime.UtcNow, null);

        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.EventType.Should().Be(EventType.Fire);
        result.Title.Should().Be("Incendio Florestal");
        result.Latitude.Should().Be(38.64);
        result.Longitude.Should().Be(-9.10);
        result.Severity.Should().Be(RiskLevel.High);
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_PersistsEventInDatabase()
    {
        var dbName = $"create_event_persist_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = new Mock<ICacheService>();
        var handler = new CreateEventHandler(db, cache.Object);
        var cmd = new CreateEventCommand(
            EventType.Flood, "Inundacao", null, 38.63, -9.09,
            RiskLevel.Medium, EventSource.IPMA, DateTime.UtcNow, null);

        await handler.HandleAsync(cmd, CancellationToken.None);

        var saved = await db.GeoEvents.FirstOrDefaultAsync(e => e.Title == "Inundacao");
        saved.Should().NotBeNull();
        saved!.EventType.Should().Be(EventType.Flood);
        saved.Geometry.X.Should().Be(-9.09);
        saved.Geometry.Y.Should().Be(38.63);
    }

    [Fact]
    public async Task HandleAsync_InvalidatesCache()
    {
        var dbName = $"create_event_cache_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = new Mock<ICacheService>();
        var handler = new CreateEventHandler(db, cache.Object);
        var cmd = new CreateEventCommand(
            EventType.Storm, "Tempestade", null, 38.65, -9.11,
            RiskLevel.Critical, EventSource.Manual, DateTime.UtcNow, null);

        await handler.HandleAsync(cmd, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefixAsync("events:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SetsMetadataWhenProvided()
    {
        var dbName = $"create_event_metadata_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = new Mock<ICacheService>();
        var handler = new CreateEventHandler(db, cache.Object);
        var metadata = """{"area":"5ha","wind_speed":"30km/h"}""";
        var cmd = new CreateEventCommand(
            EventType.Fire, "Incendio", null, 38.64, -9.10,
            RiskLevel.High, EventSource.Manual, DateTime.UtcNow, metadata);

        await handler.HandleAsync(cmd, CancellationToken.None);

        var saved = await db.GeoEvents.FirstOrDefaultAsync(e => e.Title == "Incendio");
        saved.Should().NotBeNull();
        saved!.Metadata.Should().Be(metadata);
    }
}
```

- [ ] **Step 2: Write handler + endpoint**

Create: `src/GeoRisk.API/Features/Events/CreateEvent.cs`

```csharp
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Events;

public sealed record CreateEventCommand(
    EventType EventType,
    string Title,
    string? Description,
    double Latitude,
    double Longitude,
    RiskLevel Severity,
    EventSource Source,
    DateTime OccurredAt,
    string? Metadata) : ICommand<EventResponse>;

public sealed class CreateEventHandler(
    GeoRiskDbContext db,
    ICacheService cache) : ICommandHandler<CreateEventCommand, EventResponse>
{
    public async Task<EventResponse> HandleAsync(CreateEventCommand cmd, CancellationToken ct)
    {
        var geoEvent = new GeoEvent
        {
            Id = Guid.NewGuid(),
            EventType = cmd.EventType,
            Title = cmd.Title,
            Description = cmd.Description,
            Geometry = new Point(cmd.Longitude, cmd.Latitude) { SRID = 4326 },
            Severity = cmd.Severity,
            Source = cmd.Source,
            OccurredAt = cmd.OccurredAt,
            Metadata = cmd.Metadata
        };

        db.GeoEvents.Add(geoEvent);
        await db.SaveChangesAsync(ct);

        await cache.RemoveByPrefixAsync("events:", ct);

        return new EventResponse(
            geoEvent.Id, geoEvent.EventType, geoEvent.Title, geoEvent.Description,
            geoEvent.Geometry.Y, geoEvent.Geometry.X, geoEvent.Severity, geoEvent.Source,
            geoEvent.OccurredAt, geoEvent.AIClassification, geoEvent.AIInsight, geoEvent.CreatedAt);
    }
}

public static class CreateEventEndpoint
{
    public static RouteGroupBuilder MapCreateEvent(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateEventCommand cmd,
            ICommandHandler<CreateEventCommand, EventResponse> handler) =>
        {
            var result = await handler.HandleAsync(cmd, default);
            return Results.Created($"/api/events/{result.Id}", result);
        }).RequireAuthorization("AnalystOrAdmin");

        return group;
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test src/GeoRisk.API.Tests/ --filter "FullyQualifiedName~CreateEventHandlerTests" -v n`
Expected: ALL PASS

- [ ] **Step 4: Commit**

```bash
git add src/GeoRisk.API/Features/Events/CreateEvent.cs src/GeoRisk.API.Tests/Features/Events/CreateEventHandlerTests.cs
git commit -m "feat: add CreateEvent handler with endpoint and tests"
```

---

### Task 6: GetEvents (Handler + Endpoint with filters + Tests)

**Files:**
- Create: `src/GeoRisk.API/Features/Events/GetEvents.cs`
- Create: `src/GeoRisk.API.Tests/Features/Events/GetEventsHandlerTests.cs`

- [ ] **Step 1: Write handler tests**

Create: `src/GeoRisk.API.Tests/Features/Events/GetEventsHandlerTests.cs`

```csharp
using FluentAssertions;
using GeoRisk.API.Features.Events;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Moq;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Features.Events;

public sealed class GetEventsHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    private static async Task SeedEvents(GeoRiskDbContext db, int count = 5)
    {
        var types = new[] { EventType.Fire, EventType.Flood, EventType.Storm, EventType.Heatwave, EventType.Landslide };
        var severities = new[] { RiskLevel.Low, RiskLevel.Medium, RiskLevel.High, RiskLevel.Critical };

        for (int i = 0; i < count; i++)
        {
            db.GeoEvents.Add(new GeoEvent
            {
                Id = Guid.NewGuid(),
                EventType = types[i % types.Length],
                Title = $"Test Event {i}",
                Geometry = new Point(-9.10 + i * 0.01, 38.64 + i * 0.01) { SRID = 4326 },
                Severity = severities[i % severities.Length],
                Source = EventSource.Manual,
                OccurredAt = DateTime.UtcNow.AddDays(-i),
            });
        }
        await db.SaveChangesAsync();
    }

    private static Mock<ICacheService> CreateCacheMock()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<EventListResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventListResponse?)null);
        return cache;
    }

    [Fact]
    public async Task HandleAsync_NoFilters_ReturnsPaginatedEvents()
    {
        var dbName = $"get_events_no_filter_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db, 5);
        var handler = new GetEventsHandler(db, CreateCacheMock().Object);
        var query = new GetEventsQuery(1, 10, null, null, null, null, null, null, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_FilterByType_ReturnsMatchingEvents()
    {
        var dbName = $"get_events_type_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db, 5);
        var handler = new GetEventsHandler(db, CreateCacheMock().Object);
        var query = new GetEventsQuery(1, 10, EventType.Fire, null, null, null, null, null, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().OnlyContain(e => e.EventType == EventType.Fire);
    }

    [Fact]
    public async Task HandleAsync_FilterBySeverity_ReturnsMatchingEvents()
    {
        var dbName = $"get_events_severity_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db, 5);
        var handler = new GetEventsHandler(db, CreateCacheMock().Object);
        var query = new GetEventsQuery(1, 10, null, RiskLevel.High, null, null, null, null, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().OnlyContain(e => e.Severity == RiskLevel.High);
    }

    [Fact]
    public async Task HandleAsync_PaginatesCorrectly()
    {
        var dbName = $"get_events_page_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        await SeedEvents(db, 5);
        var handler = new GetEventsHandler(db, CreateCacheMock().Object);
        var query = new GetEventsQuery(2, 2, null, null, null, null, null, null, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_WithCachedResponse_ReturnsCachedData()
    {
        var dbName = $"get_events_cached_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cached = new EventListResponse(
            [new EventResponse(Guid.NewGuid(), EventType.Fire, "Cached", null, 38.64, -9.10, RiskLevel.High, EventSource.Manual, DateTime.UtcNow, null, null, DateTime.UtcNow)],
            1, 1, 10);
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<EventListResponse>("events:cached", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);
        var handler = new GetEventsHandler(db, cache.Object);
        var query = new GetEventsQuery(1, 10, null, null, null, null, null, null, null, null, "events:cached", null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Cached");
    }

    [Fact]
    public async Task HandleAsync_DateRangeFilter_ReturnsMatchingEvents()
    {
        var dbName = $"get_events_dates_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(), EventType = EventType.Fire, Title = "Old",
            Geometry = new Point(-9.10, 38.64) { SRID = 4326 },
            Severity = RiskLevel.Low, Source = EventSource.Manual, OccurredAt = DateTime.UtcNow.AddDays(-30)
        });
        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(), EventType = EventType.Fire, Title = "Recent",
            Geometry = new Point(-9.10, 38.64) { SRID = 4326 },
            Severity = RiskLevel.Low, Source = EventSource.Manual, OccurredAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var handler = new GetEventsHandler(db, CreateCacheMock().Object);
        var query = new GetEventsQuery(1, 10, null, null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null, null, null, null, null, null);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Recent");
    }
}
```

- [ ] **Step 2: Write handler + endpoint**

Create: `src/GeoRisk.API/Features/Events/GetEvents.cs`

```csharp
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Events;

public sealed record GetEventsQuery(
    int Page,
    int PageSize,
    EventType? Type,
    RiskLevel? Severity,
    DateTime? From,
    DateTime? To,
    string? Bbox,
    double? Lat,
    double? Lng,
    double? Radius,
    string? CacheKey,
    bool? InsideSeixal) : IQuery<EventListResponse>;

public sealed class GetEventsHandler(
    GeoRiskDbContext db,
    ICacheService cache) : IQueryHandler<GetEventsQuery, EventListResponse>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<EventListResponse> HandleAsync(GetEventsQuery query, CancellationToken ct)
    {
        if (query.CacheKey is not null)
        {
            var cached = await cache.GetAsync<EventListResponse>(query.CacheKey, ct);
            if (cached is not null) return cached;
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = db.GeoEvents.AsNoTracking().AsQueryable();

        if (query.Type is not null)
            q = q.Where(e => e.EventType == query.Type);

        if (query.Severity is not null)
            q = q.Where(e => e.Severity == query.Severity);

        if (query.From is not null)
            q = q.Where(e => e.OccurredAt >= query.From);

        if (query.To is not null)
            q = q.Where(e => e.OccurredAt <= query.To);

        if (query.Bbox is not null)
        {
            var coords = query.Bbox.Split(',');
            if (coords.Length == 4
                && double.TryParse(coords[0], out var x1)
                && double.TryParse(coords[1], out var y1)
                && double.TryParse(coords[2], out var x2)
                && double.TryParse(coords[3], out var y2))
            {
                var envelope = new Envelope(x1, x2, y1, y2);
                q = q.Where(e => envelope.Contains(e.Geometry.Coordinate));
            }
        }

        if (query.Lat is not null && query.Lng is not null)
        {
            var center = new Point(query.Lng.Value, query.Lat.Value) { SRID = 4326 };
            q = q.Where(e =>
                Math.Sqrt(Math.Pow(e.Geometry.X - center.X, 2) + Math.Pow(e.Geometry.Y - center.Y, 2))
                <= (query.Radius ?? 5000) * 0.00001);
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventResponse(
                e.Id, e.EventType, e.Title, e.Description,
                e.Geometry.Y, e.Geometry.X, e.Severity, e.Source,
                e.OccurredAt, e.AIClassification, e.AIInsight, e.CreatedAt))
            .ToListAsync(ct);

        var result = new EventListResponse(items, totalCount, page, pageSize);

        if (query.CacheKey is not null)
            await cache.SetAsync(query.CacheKey, result, CacheTtl, ct);

        return result;
    }
}

public static class GetEventsEndpoint
{
    public static RouteGroupBuilder MapGetEvents(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            int? page, int? pageSize, EventType? type, RiskLevel? severity,
            DateTime? from, DateTime? to, string? bbox,
            double? lat, double? lng, double? radius, bool? insideSeixal,
            IQueryHandler<GetEventsQuery, EventListResponse> handler) =>
        {
            var p = page ?? 1;
            var ps = pageSize ?? 20;
            var cacheKey = $"events:{type}:{severity}:{from}:{to}:{bbox}:{lat}:{lng}:{radius}:{insideSeixal}:{p}:{ps}";

            var query = new GetEventsQuery(p, ps, type, severity, from, to, bbox, lat, lng, radius, cacheKey, insideSeixal);
            var result = await handler.HandleAsync(query, default);
            return Results.Ok(result);
        }).RequireAuthorization();

        return group;
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test src/GeoRisk.API.Tests/ --filter "FullyQualifiedName~GetEventsHandlerTests" -v n`
Expected: ALL PASS

- [ ] **Step 4: Commit**

```bash
git add src/GeoRisk.API/Features/Events/GetEvents.cs src/GeoRisk.API.Tests/Features/Events/GetEventsHandlerTests.cs
git commit -m "feat: add GetEvents handler with pagination, filters, bbox, and cache"
```

---

### Task 7: GetEventById (Handler + Endpoint + Tests)

**Files:**
- Create: `src/GeoRisk.API/Features/Events/GetEventById.cs`
- Create: `src/GeoRisk.API.Tests/Features/Events/GetEventByIdHandlerTests.cs`

- [ ] **Step 1: Write handler tests**

Create: `src/GeoRisk.API.Tests/Features/Events/GetEventByIdHandlerTests.cs`

```csharp
using FluentAssertions;
using GeoRisk.API.Features.Events;
using GeoRisk.API.Features.Events.Dto;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Features.Events;

public sealed class GetEventByIdHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ExistingId_ReturnsEvent()
    {
        var dbName = $"getbyid_exists_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var eventId = Guid.NewGuid();
        db.GeoEvents.Add(new GeoEvent
        {
            Id = eventId, EventType = EventType.Fire, Title = "Test Fire",
            Geometry = new Point(-9.10, 38.64) { SRID = 4326 },
            Severity = RiskLevel.High, Source = EventSource.Manual, OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetEventByIdHandler(db);
        var result = await handler.HandleAsync(new GetEventByIdQuery(eventId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(eventId);
        result.Title.Should().Be("Test Fire");
        result.Latitude.Should().Be(38.64);
        result.Longitude.Should().Be(-9.10);
    }

    [Fact]
    public async Task HandleAsync_NonexistentId_ReturnsNull()
    {
        var dbName = $"getbyid_missing_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var handler = new GetEventByIdHandler(db);

        var result = await handler.HandleAsync(new GetEventByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
```

- [ ] **Step 2: Write handler + endpoint**

Create: `src/GeoRisk.API/Features/Events/GetEventById.cs`

```csharp
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;

namespace GeoRisk.API.Features.Events;

public sealed record GetEventByIdQuery(Guid Id) : IQuery<EventResponse?>;

public sealed class GetEventByIdHandler(GeoRiskDbContext db) : IQueryHandler<GetEventByIdQuery, EventResponse?>
{
    public async Task<EventResponse?> HandleAsync(GetEventByIdQuery query, CancellationToken ct)
    {
        var e = await db.GeoEvents.AsNoTracking()
            .FirstOrDefaultAsync(ev => ev.Id == query.Id, ct);

        if (e is null) return null;

        return new EventResponse(
            e.Id, e.EventType, e.Title, e.Description,
            e.Geometry.Y, e.Geometry.X, e.Severity, e.Source,
            e.OccurredAt, e.AIClassification, e.AIInsight, e.CreatedAt);
    }
}

public static class GetEventByIdEndpoint
{
    public static RouteGroupBuilder MapGetEventById(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetEventByIdQuery, EventResponse?> handler) =>
        {
            var result = await handler.HandleAsync(new GetEventByIdQuery(id), default);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        return group;
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test src/GeoRisk.API.Tests/ --filter "FullyQualifiedName~GetEventByIdHandlerTests" -v n`
Expected: ALL PASS

- [ ] **Step 4: Commit**

```bash
git add src/GeoRisk.API/Features/Events/GetEventById.cs src/GeoRisk.API.Tests/Features/Events/GetEventByIdHandlerTests.cs
git commit -m "feat: add GetEventById handler with endpoint and tests"
```

---

### Task 8: ImportEvents (Handler + Endpoint + Tests)

**Files:**
- Create: `src/GeoRisk.API/Features/Events/ImportEvents.cs`
- Create: `src/GeoRisk.API.Tests/Features/Events/ImportEventsHandlerTests.cs`

- [ ] **Step 1: Write handler tests**

Create: `src/GeoRisk.API.Tests/Features/Events/ImportEventsHandlerTests.cs`

```csharp
using FluentAssertions;
using GeoRisk.API.Features.Events;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Moq;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Tests.Features.Events;

public sealed class ImportEventsHandlerTests
{
    private static GeoRiskDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GeoRiskDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GeoRiskDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ImportsNewEvents()
    {
        var dbName = $"import_new_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = new Mock<ICacheService>();
        var handler = new ImportEventsHandler(db, cache.Object);
        var cmd = new ImportEventsCommand([
            new("ICNF-001", EventType.Fire, "Incendio 1", 38.64, -9.10, RiskLevel.High, EventSource.ICNF, DateTime.UtcNow, null),
            new("IPMA-001", EventType.Flood, "Inundacao", 38.63, -9.09, RiskLevel.Medium, EventSource.IPMA, DateTime.UtcNow, null)
        ]);

        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        result.Imported.Should().Be(2);
        result.Skipped.Should().Be(0);
        var events = await db.GeoEvents.ToListAsync();
        events.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_SkipsDuplicatesBySourceId()
    {
        var dbName = $"import_dedup_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        db.GeoEvents.Add(new GeoEvent
        {
            Id = Guid.NewGuid(), EventType = EventType.Fire, Title = "Existing",
            Geometry = new Point(-9.10, 38.64) { SRID = 4326 },
            Severity = RiskLevel.High, Source = EventSource.ICNF,
            SourceId = "ICNF-001", OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var cache = new Mock<ICacheService>();
        var handler = new ImportEventsHandler(db, cache.Object);
        var cmd = new ImportEventsCommand([
            new("ICNF-001", EventType.Fire, "Duplicate", 38.64, -9.10, RiskLevel.High, EventSource.ICNF, DateTime.UtcNow, null),
            new("ICNF-002", EventType.Fire, "New Event", 38.65, -9.11, RiskLevel.Low, EventSource.ICNF, DateTime.UtcNow, null)
        ]);

        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        result.Imported.Should().Be(1);
        result.Skipped.Should().Be(1);
        var events = await db.GeoEvents.ToListAsync();
        events.Should().HaveCount(2);
        events.Should().ContainSingle(e => e.SourceId == "ICNF-002");
    }

    [Fact]
    public async Task HandleAsync_InvalidatesCache()
    {
        var dbName = $"import_cache_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = new Mock<ICacheService>();
        var handler = new ImportEventsHandler(db, cache.Object);
        var cmd = new ImportEventsCommand([
            new("ANEPC-001", EventType.Storm, "Storm", 38.64, -9.10, RiskLevel.Critical, EventSource.ANEPC, DateTime.UtcNow, null)
        ]);

        await handler.HandleAsync(cmd, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefixAsync("events:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyList_ReturnsZeroCounts()
    {
        var dbName = $"import_empty_{Guid.NewGuid()}";
        using var db = CreateDbContext(dbName);
        var cache = new Mock<ICacheService>();
        var handler = new ImportEventsHandler(db, cache.Object);
        var cmd = new ImportEventsCommand([]);

        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        result.Imported.Should().Be(0);
        result.Skipped.Should().Be(0);
    }
}
```

- [ ] **Step 2: Write handler + endpoint**

Create: `src/GeoRisk.API/Features/Events/ImportEvents.cs`

```csharp
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Events.Dto;
using GeoRisk.API.Infrastructure.Cache;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.Events;

public sealed record ImportEventsCommand(List<ImportEventItem> Items) : ICommand<ImportEventsResponse>;

public sealed class ImportEventsHandler(
    GeoRiskDbContext db,
    ICacheService cache) : ICommandHandler<ImportEventsCommand, ImportEventsResponse>
{
    public async Task<ImportEventsResponse> HandleAsync(ImportEventsCommand cmd, CancellationToken ct)
    {
        var existingSourceIds = await db.GeoEvents
            .Where(e => e.SourceId != null)
            .Select(e => e.SourceId!)
            .ToHashSetAsync(ct);

        var imported = 0;
        var skipped = 0;

        foreach (var item in cmd.Items)
        {
            if (existingSourceIds.Contains(item.SourceId))
            {
                skipped++;
                continue;
            }

            db.GeoEvents.Add(new GeoEvent
            {
                Id = Guid.NewGuid(),
                EventType = item.EventType,
                Title = item.Title,
                Description = item.Description,
                Geometry = new Point(item.Longitude, item.Latitude) { SRID = 4326 },
                Severity = item.Severity,
                Source = item.Source,
                SourceId = item.SourceId,
                OccurredAt = item.OccurredAt
            });

            existingSourceIds.Add(item.SourceId);
            imported++;
        }

        if (imported > 0)
        {
            await db.SaveChangesAsync(ct);
            await cache.RemoveByPrefixAsync("events:", ct);
        }

        return new ImportEventsResponse(imported, skipped);
    }
}

public static class ImportEventsEndpoint
{
    public static RouteGroupBuilder MapImportEvents(this RouteGroupBuilder group)
    {
        group.MapPost("/import", async (
            List<ImportEventItem> items,
            ICommandHandler<ImportEventsCommand, ImportEventsResponse> handler) =>
        {
            var result = await handler.HandleAsync(new ImportEventsCommand(items), default);
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        return group;
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test src/GeoRisk.API.Tests/ --filter "FullyQualifiedName~ImportEventsHandlerTests" -v n`
Expected: ALL PASS

- [ ] **Step 4: Commit**

```bash
git add src/GeoRisk.API/Features/Events/ImportEvents.cs src/GeoRisk.API.Tests/Features/Events/ImportEventsHandlerTests.cs
git commit -m "feat: add ImportEvents handler with deduplication and tests"
```

---

### Task 9: Seed Data

**Files:**
- Create: `src/GeoRisk.API/Infrastructure/Persistence/SeedData.cs`

- [ ] **Step 1: Create seed data**

Create: `src/GeoRisk.API/Infrastructure/Persistence/SeedData.cs`

```csharp
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(GeoRiskDbContext db)
    {
        if (await db.GeoEvents.AnyAsync()) return;

        var events = new List<GeoEvent>
        {
            Make(EventType.Fire, "Incendio Florestal - Corroios", "Fogo em zona florestal", -9.155, 38.625, RiskLevel.High, EventSource.ICNF, "ICNF-2026-001", -1),
            Make(EventType.Fire, "Incendio - Aldeia de Paio Pires", "Fogo em vegetacao seca", -9.085, 38.615, RiskLevel.Critical, EventSource.ANEPC, "ANEPC-2026-001", -2),
            Make(EventType.Fire, "Queimada - Fogueteiro", "Queimada controlada que escalou", -9.100, 38.640, RiskLevel.Medium, EventSource.ICNF, "ICNF-2026-002", -5),
            Make(EventType.Flood, "Inundacao - Seixal Centro", "Cheia na zona ribeirinha", -9.103, 38.640, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-002", -3),
            Make(EventType.Flood, "Alagamento - Amora", "Chuva intensa causou alagamento", -9.115, 38.620, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-001", -10),
            Make(EventType.Flood, "Transbordo Ribeira - Corroios", "Ribeira transbordou apos chuva", -9.150, 38.630, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-002", -20),
            Make(EventType.Storm, "Tempestade - Torres da Marinha", "Ventos de 90 km/h", -9.080, 38.635, RiskLevel.Critical, EventSource.IPMA, "IPMA-2026-003", -7),
            Make(EventType.Storm, "Trovoada - Pinhal General", "Trovoada intensa com granizo", -9.120, 38.655, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-004", -15),
            Make(EventType.Storm, "Temporal - Seixal", "Chuva forte e vento", -9.100, 38.640, RiskLevel.High, EventSource.IPMA, "IPMA-2026-005", -30),
            Make(EventType.Landslide, "Deslizamento - Miratejo", "Deslizamento de terra apos chuvas", -9.090, 38.610, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-003", -12),
            Make(EventType.Landslide, "Erosao - Quinta do Conde", "Erosao costeira acentuada", -9.050, 38.590, RiskLevel.Medium, EventSource.Manual, null, -25),
            Make(EventType.Industrial, "Fuga de Gas - Paio Pires", "Fuga de gas industrial", -9.085, 38.615, RiskLevel.Critical, EventSource.ANEPC, "ANEPC-2026-004", -4),
            Make(EventType.Industrial, "Incendio Industrial - Seixal", "Fogo em armazem industrial", -9.105, 38.638, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-005", -18),
            Make(EventType.Heatwave, "Onda de Calor - Seixal", "Temperaturas acima de 40C", -9.100, 38.640, RiskLevel.High, EventSource.IPMA, "IPMA-2026-006", -8),
            Make(EventType.Heatwave, "Alerta Calor - Corroios", "Alerta laranja por calor extremo", -9.150, 38.625, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-007", -22),
            Make(EventType.Fire, "Fogo em Mato - Fernao Ferro", "Fogo em mato baixo", -9.130, 38.590, RiskLevel.Low, EventSource.ICNF, "ICNF-2026-003", -14),
            Make(EventType.Other, "Poluicao - Rio Judeu", "Contaminacao detetada no rio", -9.110, 38.635, RiskLevel.Medium, EventSource.Manual, null, -6),
            Make(EventType.Storm, "Vento Forte - Paio Pires", "Ramos partidos e telhas levantadas", -9.085, 38.615, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-008", -28),
            Make(EventType.Flood, "Subida Mare - Seixal", "Mare viva causou inundacao parcial", -9.103, 38.641, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-009", -35),
            Make(EventType.Industrial, "Derrame - Porto do Seixal", "Derrame de substancia no porto", -9.095, 38.645, RiskLevel.Medium, EventSource.ANEPC, "ANEPC-2026-006", -16)
        };

        db.GeoEvents.AddRange(events);
        await db.SaveChangesAsync();
    }

    private static GeoEvent Make(EventType type, string title, string desc,
        double lng, double lat, RiskLevel severity, EventSource source,
        string? sourceId, int daysOffset) => new()
    {
        Id = Guid.NewGuid(),
        EventType = type,
        Title = title,
        Description = desc,
        Geometry = new Point(lng, lat) { SRID = 4326 },
        Severity = severity,
        Source = source,
        SourceId = sourceId,
        OccurredAt = DateTime.UtcNow.AddDays(daysOffset)
    };
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/GeoRisk.API/GeoRisk.API.csproj`
Expected: BUILD SUCCEEDS

- [ ] **Step 3: Commit**

```bash
git add src/GeoRisk.API/Infrastructure/Persistence/SeedData.cs
git commit -m "feat: add seed data with 20 realistic events around Seixal"
```

---

### Task 10: Wire Up Program.cs

**Files:**
- Modify: `src/GeoRisk.API/Program.cs`
- Modify: `src/GeoRisk.API/appsettings.json`

- [ ] **Step 1: Add usings**

Add at top of `Program.cs` after existing usings:

```csharp
using GeoRisk.API.Features.Events;
using GeoRisk.API.Infrastructure.Cache;
using StackExchange.Redis;
```

- [ ] **Step 2: Add Redis and cache registration**

Add after `builder.Services.AddAuthorization();` in `Program.cs`:

```csharp
// Redis Cache
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetSection("Redis");
    var connectionString = config["ConnectionString"] ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AnalystOrAdmin", policy =>
        policy.RequireRole("Analyst", "Admin"))
    .AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
```

- [ ] **Step 3: Add event endpoints**

Add after the auth endpoints block:

```csharp
// Event endpoints
var events = api.MapGroup("/events")
    .WithTags("Events");
events.MapGetEvents();
events.MapGetEventById();
events.MapCreateEvent();
events.MapImportEvents();
```

- [ ] **Step 4: Add seed data call**

Replace `await app.RunAsync();` with:

```csharp
// Seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedAsync(db);
}

await app.RunAsync();
```

- [ ] **Step 5: Add Redis config to appsettings.json**

Add after the `"AllowedHosts": "*"` line in `appsettings.json`:

```json
,
"Redis": {
  "ConnectionString": "localhost:6379"
}
```

- [ ] **Step 6: Verify build**

Run: `dotnet build src/GeoRisk.API/GeoRisk.API.csproj`
Expected: BUILD SUCCEEDS

- [ ] **Step 7: Run all tests**

Run: `dotnet test src/GeoRisk.API.Tests/ -v n`
Expected: ALL PASS

- [ ] **Step 8: Commit**

```bash
git add src/GeoRisk.API/Program.cs src/GeoRisk.API/appsettings.json
git commit -m "feat: wire up cache, event endpoints, auth policies, and seed data"
```

---

### Task 11: Final Verification

- [ ] **Step 1: Full test suite**

Run: `dotnet test src/GeoRisk.API.Tests/ -v n`
Expected: ALL PASS

- [ ] **Step 2: Release build**

Run: `dotnet build src/GeoRisk.API/GeoRisk.API.csproj -c Release`
Expected: BUILD SUCCEEDS
