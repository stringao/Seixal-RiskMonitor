using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.AI;

public sealed class LlmSettingsService(GeoRiskDbContext db) : ILlmSettingsService
{
    public async Task<LlmSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var settings = await db.AppSettings.FirstOrDefaultAsync(ct);

        if (settings == null || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return new LlmSettings(
                Provider: "OpenAI",
                ApiKey: "",
                ModelName: "gpt-4o",
                MaxTokens: 1024,
                IsConfigured: false);
        }

        return new LlmSettings(
            Provider: settings.LlmProvider,
            ApiKey: settings.ApiKey,
            ModelName: settings.ModelName,
            MaxTokens: settings.MaxTokens,
            IsConfigured: true);
    }
}
