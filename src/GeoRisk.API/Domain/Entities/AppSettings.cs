namespace GeoRisk.API.Domain.Entities;

public class AppSettings
{
    public int Id { get; set; }
    public string ActiveProvider { get; set; } = "DeepSeek"; // Points to which provider is active
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}