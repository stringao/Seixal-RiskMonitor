using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoRisk.API.Infrastructure.AI;

/// <summary>
/// Ollama provider - self-hosted LLM (runs locally)
/// Uses OpenAI-compatible API format
/// Supports Qwen, Llama, Mistral, and other local models
/// </summary>
public class OllamaProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILlmSettingsService _settingsService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaProvider(HttpClient httpClient, ILlmSettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> CompleteAsync(string system, string user, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Ollama is not configured. Please add your BaseUrl in Settings.");

        var request = new
        {
            model = settings.ModelName,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl}/chat/completions");
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama API error ({response.StatusCode}): {text}");

        // Ollama returns OpenAI-compatible format - extract content from choices
        using var doc = JsonDocument.Parse(text);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return content;
    }

    public async Task<T> CompleteStructuredAsync<T>(string system, string user, CancellationToken ct = default) where T : class
    {
        var settings = await _settingsService.GetSettingsAsync(ct);
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Ollama is not configured. Please add your BaseUrl in Settings.");

        var request = new
        {
            model = settings.ModelName,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            format = "json"
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl}/chat/completions");
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama API error ({response.StatusCode}): {text}");

        // Ollama returns OpenAI-compatible format - extract content from choices
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // Navigate to content: choices[0].message.content
            var content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            // Remove markdown code blocks if present
            if (content.StartsWith("```json"))
                content = content.Substring(7);
            else if (content.StartsWith("```"))
                content = content.Substring(3);

            if (content.EndsWith("```"))
                content = content.Substring(0, content.Length - 3);

            return JsonSerializer.Deserialize<T>(content.Trim(), JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse Ollama response: {ex.Message}. Raw response: {text.Substring(0, Math.Min(500, text.Length))}");
        }
    }

    }