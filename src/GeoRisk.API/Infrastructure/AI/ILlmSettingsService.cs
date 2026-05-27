namespace GeoRisk.API.Infrastructure.AI;

public interface ILlmSettingsService
{
    Task<LlmSettings> GetSettingsAsync(CancellationToken ct = default);
}

public sealed record LlmSettings(
    string Provider,
    string ApiKey,
    string ModelName,
    int MaxTokens,
    bool IsConfigured);
