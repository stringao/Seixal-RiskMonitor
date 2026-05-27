namespace GeoRisk.API.Domain.Entities;

public class DashboardCache
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CacheKey { get; set; } = string.Empty; // e.g., "dashboard_today", "risk_summary_week25"
    public string Content { get; set; } = string.Empty; // JSON of structured dashboard data
    public string GeneratedBy { get; set; } = string.Empty; // AI model used
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
