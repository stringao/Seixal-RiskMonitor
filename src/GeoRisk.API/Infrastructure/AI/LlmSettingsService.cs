using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.AI;

public sealed class LlmSettingsService(GeoRiskDbContext db) : ILlmSettingsService
{
    public async Task<LlmSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var appSettings = await db.AppSettings.FirstOrDefaultAsync(ct);
        var activeProvider = appSettings?.ActiveProvider ?? "DeepSeek";

        var providerConfig = await db.AiProviderConfigs
            .FirstOrDefaultAsync(p => p.Provider == activeProvider, ct);

        if (providerConfig == null)
        {
            return new LlmSettings(
                Provider: activeProvider,
                ApiKey: "",
                ModelName: "deepseek-chat",
                BaseUrl: null,
                MaxTokens: 1024,
                IsConfigured: false);
        }

        // For Ollama, we check IsEnabled and BaseUrl instead of ApiKey
        if (activeProvider == "Ollama")
        {
            var isConfigured = providerConfig.IsEnabled && !string.IsNullOrWhiteSpace(providerConfig.BaseUrl);
            return new LlmSettings(
                Provider: providerConfig.Provider,
                ApiKey: "",
                ModelName: providerConfig.Model,
                BaseUrl: providerConfig.BaseUrl,
                MaxTokens: providerConfig.MaxTokens,
                IsConfigured: isConfigured);
        }

        // For other providers, check ApiKey
        var hasApiKey = !string.IsNullOrWhiteSpace(providerConfig.ApiKey);
        return new LlmSettings(
            Provider: providerConfig.Provider,
            ApiKey: providerConfig.ApiKey,
            ModelName: providerConfig.Model,
            BaseUrl: providerConfig.BaseUrl,
            MaxTokens: providerConfig.MaxTokens,
            IsConfigured: hasApiKey);
    }
}