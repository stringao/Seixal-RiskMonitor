namespace GeoRisk.API.Domain.Entities;

public enum ReportType
{
    DailySummary,
    WeeklyReport,
    MonthlyReport,
    EventAlert,
    HotspotAlert
}

public class ScheduledReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public string Schedule { get; set; } = string.Empty; // cron-like: "0 8 * * *" = daily at 8am
    public List<string> Recipients { get; set; } = new();
    public DateTime? LastGeneratedAt { get; set; }
    public string? LastContent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
