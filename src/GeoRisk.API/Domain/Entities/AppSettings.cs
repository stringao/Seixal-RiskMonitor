namespace GeoRisk.API.Domain.Entities;

public class AppSettings
{
    public int Id { get; set; }
    public string LlmProvider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 1024;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
