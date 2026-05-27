using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace GeoRisk.API.Infrastructure.AI;

public class AnthropicProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AnthropicProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration.GetSection("Anthropic")["ApiKey"] ?? "";
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Anthropic:ApiKey is not configured");
    }

    public async Task<string> CompleteAsync(string system, string user, CancellationToken ct = default)
    {
        var request = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 1024,
            system,
            messages = new[] { new { role = "user", content = user } }
        };

        EnsureApiKey();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(ct);

        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async Task<T> CompleteStructuredAsync<T>(string system, string user, CancellationToken ct = default) where T : class
    {
        var request = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 1024,
            system,
            messages = new[] { new { role = "user", content = user } },
            response_format = new { type = "json_object" }
        };

        EnsureApiKey();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(req, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(text, JsonOptions) ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private record AnthropicResponse(List<AnthropicContent>? Content);
    private record AnthropicContent(string Text);
}
