using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.Infrastructure.AI;

public sealed class LlmProviderFactory(
    IServiceProvider serviceProvider,
    ILlmSettingsService settingsService) : ILlmProvider
{
    public async Task<string> CompleteAsync(string system, string user, CancellationToken ct = default)
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        var provider = GetProvider(settings.Provider);
        return await provider.CompleteAsync(system, user, ct);
    }

    public async Task<T> CompleteStructuredAsync<T>(string system, string user, CancellationToken ct = default) where T : class
    {
        var settings = await settingsService.GetSettingsAsync(ct);
        var provider = GetProvider(settings.Provider);
        return await provider.CompleteStructuredAsync<T>(system, user, ct);
    }

    private ILlmProvider GetProvider(string providerName)
    {
        return providerName switch
        {
            "DeepSeek" => serviceProvider.GetRequiredService<DeepSeekProvider>(),
            "Ollama" => serviceProvider.GetRequiredService<OllamaProvider>(),
            "Qwen" => serviceProvider.GetRequiredService<QwenProvider>(),
            "Anthropic" => serviceProvider.GetRequiredService<AnthropicProvider>(),
            "OpenAI" => serviceProvider.GetRequiredService<OpenAIProvider>(),
            _ => serviceProvider.GetRequiredService<DeepSeekProvider>()
        };
    }
}