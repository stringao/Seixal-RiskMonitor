using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoRisk.API.Infrastructure.AI;

public class AnthropicProvider : ILlmProvider
{
#pragma warning disable S1075
    private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";
#pragma warning restore S1075

    private readonly HttpClient _httpClient;
    private readonly ILlmSettingsService _settingsService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AnthropicProvider(HttpClient httpClient, ILlmSettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> CompleteAsync(string system, string user, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Anthropic API is not configured. Please add your API key in Settings.");

        var request = new
        {
            model = settings.ModelName,
            max_tokens = settings.MaxTokens,
            system,
            messages = new[] { new { role = "user", content = user } }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl);
        req.Headers.Add("x-api-key", settings.ApiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(ct);

        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async Task<T> CompleteStructuredAsync<T>(string system, string user, CancellationToken ct = default) where T : class
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Anthropic API is not configured. Please add your API key in Settings.");

        var request = new
        {
            model = settings.ModelName,
            max_tokens = settings.MaxTokens,
            system,
            messages = new[] { new { role = "user", content = user } },
            response_format = new { type = "json_object" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl);
        req.Headers.Add("x-api-key", settings.ApiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(text, JsonOptions) ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private sealed record AnthropicResponse(List<AnthropicContent>? Content);
    private sealed record AnthropicContent(string Text);
}
