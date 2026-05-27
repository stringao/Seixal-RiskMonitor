using StackExchange.Redis;

namespace GeoRisk.API.Infrastructure.Cache;

public interface ISyncStatusService
{
    Task<SyncStatus> GetStatusAsync(string source, CancellationToken ct = default);
    Task<IReadOnlyList<SyncStatus>> GetAllStatusesAsync(CancellationToken ct = default);
    Task RecordSuccessAsync(string source, int itemsSynced, CancellationToken ct = default);
    Task RecordFailureAsync(string source, string error, CancellationToken ct = default);
}

public sealed record SyncStatus(
    string Source,
    string DisplayName,
    DateTime? LastSuccessAt,
    DateTime? LastAttemptAt,
    string Status,
    int ItemsSyncedLastRun,
    string? LastError);

public sealed class SyncStatusService(IConnectionMultiplexer redis) : ISyncStatusService
{
    private static readonly Dictionary<string, string> SourceDisplayNames = new()
    {
        ["icnf"] = "ICNF (Incêndios)",
        ["anepc"] = "ANEPC (Emergências)",
        ["ipma"] = "IPMA (Risco de Fogo)"
    };

    public async Task<SyncStatus> GetStatusAsync(string source, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var keyPrefix = $"sync:{source}";

        var lastSuccess = await db.StringGetAsync($"{keyPrefix}:last_success_at");
        var lastAttempt = await db.StringGetAsync($"{keyPrefix}:last_attempt_at");
        var status = await db.StringGetAsync($"{keyPrefix}:status");
        var itemCount = await db.StringGetAsync($"{keyPrefix}:items_count");
        var error = await db.StringGetAsync($"{keyPrefix}:last_error");

        return new SyncStatus(
            Source: source.ToUpperInvariant(),
            DisplayName: SourceDisplayNames.GetValueOrDefault(source, source.ToUpperInvariant()),
            LastSuccessAt: lastSuccess.HasValue ? DateTime.Parse((string)lastSuccess!) : null,
            LastAttemptAt: lastAttempt.HasValue ? DateTime.Parse((string)lastAttempt!) : null,
            Status: status.HasValue ? (string)status! : "Never run",
            ItemsSyncedLastRun: itemCount.HasValue ? int.Parse((string)itemCount!) : 0,
            LastError: error.HasValue ? (string)error! : null
        );
    }

    public async Task<IReadOnlyList<SyncStatus>> GetAllStatusesAsync(CancellationToken ct = default)
    {
        var sources = new[] { "icnf", "anepc", "ipma" };
        var statuses = new List<SyncStatus>();

        foreach (var source in sources)
        {
            statuses.Add(await GetStatusAsync(source, ct));
        }

        return statuses;
    }

    public async Task RecordSuccessAsync(string source, int itemsSynced, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var keyPrefix = $"sync:{source}";
        var now = DateTime.UtcNow.ToString("O");

        await Task.WhenAll(
            db.StringSetAsync($"{keyPrefix}:last_success_at", now),
            db.StringSetAsync($"{keyPrefix}:last_attempt_at", now),
            db.StringSetAsync($"{keyPrefix}:status", "Success"),
            db.StringSetAsync($"{keyPrefix}:items_count", itemsSynced.ToString()),
            db.KeyDeleteAsync($"{keyPrefix}:last_error")
        );
    }

    public async Task RecordFailureAsync(string source, string error, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var keyPrefix = $"sync:{source}";
        var now = DateTime.UtcNow.ToString("O");

        await Task.WhenAll(
            db.StringSetAsync($"{keyPrefix}:last_attempt_at", now),
            db.StringSetAsync($"{keyPrefix}:status", "Failed"),
            db.StringSetAsync($"{keyPrefix}:last_error", error)
        );
    }
}