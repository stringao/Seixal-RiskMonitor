namespace GeoRisk.API.Domain.Entities;

public class NotificationQueueItem
{
    public Guid Id { get; set; }
    public Guid? AlertId { get; set; }
    public Alert? Alert { get; set; }
    public Guid? PushSubscriptionId { get; set; }
    public PushSubscription? PushSubscription { get; set; }
    public Guid? CitizenSubscriptionId { get; set; }
    public CitizenAlertSubscription? CitizenSubscription { get; set; }
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public enum NotificationType
{
    Push,
    Email,
    Sms
}

public enum NotificationStatus
{
    Pending,
    Processing,
    Sent,
    Failed
}