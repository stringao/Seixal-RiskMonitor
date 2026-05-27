namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Configuration for AI providers.
/// Each provider (DeepSeek, Ollama, Qwen, Anthropic, OpenAI) has its own settings.
/// </summary>
public class AiProviderConfig
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty; // DeepSeek, Ollama, Qwen, Anthropic, OpenAI
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? BaseUrl { get; set; } // For Ollama - local URL
    public int MaxTokens { get; set; } = 1024;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}