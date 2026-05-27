using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoRisk.API.Infrastructure.AI;

/// <summary>
/// DeepSeek AI provider - primary LLM for production use
/// Uses OpenAI-compatible API format
/// </summary>
public class DeepSeekProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILlmSettingsService _settingsService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public DeepSeekProvider(HttpClient httpClient, ILlmSettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> CompleteAsync(string system, string user, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
            throw new InvalidOperationException("DeepSeek API is not configured. Please add your API key in Settings.");

        var request = new
        {
            model = settings.ModelName,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"DeepSeek API error ({response.StatusCode}): {text}");

        var result = JsonSerializer.Deserialize<DeepSeekResponse>(text, JsonOptions);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    public async Task<T> CompleteStructuredAsync<T>(string system, string user, CancellationToken ct = default) where T : class
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
            throw new InvalidOperationException("DeepSeek API is not configured. Please add your API key in Settings.");

        var request = new
        {
            model = settings.ModelName,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            response_format = new { type = "json_object" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"DeepSeek API error ({response.StatusCode}): {text}");

        return JsonSerializer.Deserialize<T>(text, JsonOptions) ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private record DeepSeekResponse(List<DeepSeekChoice>? Choices);
    private record DeepSeekChoice(DeepSeekMessage? Message);
    private record DeepSeekMessage(string Content);
}