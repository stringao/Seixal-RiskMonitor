using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoRisk.API.Infrastructure.AI;

/// <summary>
/// Qwen AI provider (Alibaba) - alternative LLM
/// Uses OpenAI-compatible API format
/// </summary>
public class QwenProvider : ILlmProvider
{
#pragma warning disable S1075
    private const string ApiBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
#pragma warning restore S1075

    private readonly HttpClient _httpClient;
    private readonly ILlmSettingsService _settingsService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public QwenProvider(HttpClient httpClient, ILlmSettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> CompleteAsync(string system, string user, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Qwen API is not configured. Please add your API key in Settings.");

        var request = new
        {
            model = settings.ModelName,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var result = await response.Content.ReadFromJsonAsync<QwenResponse>(ct);

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    public async Task<T> CompleteStructuredAsync<T>(string system, string user, CancellationToken ct = default) where T : class
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Qwen API is not configured. Please add your API key in Settings.");

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

        using var req = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(text, JsonOptions) ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private sealed record QwenResponse(List<QwenChoice>? Choices);
    private sealed record QwenChoice(QwenMessage? Message);
    private sealed record QwenMessage(string Content);
}
