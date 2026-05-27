using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Services;

public class AlertNotificationService
{
    private readonly GeoRiskDbContext _db;
    private readonly PushNotificationService _pushService;
    private readonly ILogger<AlertNotificationService> _logger;

    // Rate limiting: max 1 notification per subscriber per event type per hour
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

    public AlertNotificationService(
        GeoRiskDbContext db,
        PushNotificationService pushService,
        ILogger<AlertNotificationService> logger)
    {
        _db = db;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task QueueNotificationsForAlertAsync(Alert alert, GeoEvent? geoEvent = null, CancellationToken ct = default)
    {
        if (geoEvent == null && alert.GeoEventId.HasValue)
        {
            geoEvent = await _db.GeoEvents.FindAsync(new object[] { alert.GeoEventId.Value }, ct);
        }

        var latitude = geoEvent?.Geometry?.Y ?? 0;
        var longitude = geoEvent?.Geometry?.X ?? 0;

        // Queue push notifications for all active push subscribers
        var pushSubscriptions = await _db.PushSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        foreach (var subscriptionId in pushSubscriptions.Select(subscription => subscription.Id))
        {
            // Check rate limiting
            if (await HasRecentNotificationAsync(subscriptionId, ct))
                continue;

            var queueItem = new NotificationQueueItem
            {
                Id = Guid.NewGuid(),
                AlertId = alert.Id,
                PushSubscriptionId = subscriptionId,
                Type = NotificationType.Push,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.NotificationQueueItems.Add(queueItem);
        }

        // Queue notifications for citizen subscriptions in the affected area
        const double Epsilon = 1e-10;
        if (Math.Abs(latitude) > Epsilon && Math.Abs(longitude) > Epsilon)
        {
            var citizenSubscriptions = await GetCitizenSubscriptionsInAreaAsync(ct);

            foreach (var citizen in citizenSubscriptions)
            {
                if (await HasRecentCitizenNotificationAsync(citizen.Id, ct))
                    continue;

                var queueItem = new NotificationQueueItem
                {
                    Id = Guid.NewGuid(),
                    AlertId = alert.Id,
                    CitizenSubscriptionId = citizen.Id,
                    Type = citizen.Email != null ? NotificationType.Email : NotificationType.Sms,
                    Status = NotificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _db.NotificationQueueItems.Add(queueItem);
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Queued notifications for alert {AlertId}", alert.Id);
    }

    private async Task<bool> HasRecentNotificationAsync(Guid subscriptionId, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - RateLimitWindow;
        return await _db.NotificationQueueItems
            .AnyAsync(n => n.PushSubscriptionId == subscriptionId
                && n.Status == NotificationStatus.Sent
                && n.CreatedAt >= cutoff, ct);
    }

    private async Task<bool> HasRecentCitizenNotificationAsync(Guid subscriptionId, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - RateLimitWindow;
        return await _db.NotificationQueueItems
            .AnyAsync(n => n.CitizenSubscriptionId == subscriptionId
                && n.Status == NotificationStatus.Sent
                && n.CreatedAt >= cutoff, ct);
    }

    private async Task<List<CitizenAlertSubscription>> GetCitizenSubscriptionsInAreaAsync(
        CancellationToken ct)
    {
        // Get subscriptions within radius that match event type and severity
        return await _db.CitizenAlertSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync(ct);
    }

    public async Task ProcessPendingNotificationsAsync(int batchSize = 50, CancellationToken ct = default)
    {
        var pendingItems = await _db.NotificationQueueItems
            .Where(n => n.Status == NotificationStatus.Pending && n.RetryCount < 3)
            .OrderBy(n => n.CreatedAt)
            .Take(batchSize)
            .Include(n => n.Alert)
            .Include(n => n.PushSubscription)
            .Include(n => n.CitizenSubscription)
            .ToListAsync(ct);

        foreach (var item in pendingItems)
        {
            try
            {
                item.Status = NotificationStatus.Processing;
                await _db.SaveChangesAsync(ct);

                bool success = false;

                if (item.Type == NotificationType.Push && item.PushSubscription != null)
                {
                    var alert = item.Alert;
                    success = await _pushService.SendPushNotificationAsync(
                        item.PushSubscription,
                        alert?.Title ?? "GeoRisk Alert",
                        alert?.Message ?? "A new alert has been triggered.",
                        data: new { alertId = alert?.Id, severity = alert?.Severity.ToString() },
                        ct: ct);
                }
                else if (item.Type == NotificationType.Email && item.CitizenSubscription?.Email != null)
                {
                    // Stub: Send email via SendGrid
                    _logger.LogInformation("Would send email to {Email} for alert {AlertId}",
                        item.CitizenSubscription.Email, item.AlertId);
                    success = true;
                }
                else if (item.Type == NotificationType.Sms && item.CitizenSubscription?.Phone != null)
                {
                    // Stub: Send SMS via Twilio
                    _logger.LogInformation("Would send SMS to {Phone} for alert {AlertId}",
                        item.CitizenSubscription.Phone, item.AlertId);
                    success = true;
                }

                item.Status = success ? NotificationStatus.Sent : NotificationStatus.Failed;
                item.ProcessedAt = DateTime.UtcNow;

                if (!success)
                    item.RetryCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process notification {NotificationId}", item.Id);
                item.Status = NotificationStatus.Failed;
                item.ErrorMessage = ex.Message;
                item.RetryCount++;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

#pragma warning disable S107 // Method has too many parameters — public API contract
    public async Task<CitizenAlertSubscription> CreateZoneSubscriptionAsync(
        string? email,
        string? phone,
        double latitude,
        double longitude,
        double radiusKm,
        List<EventType> eventTypes,
        RiskLevel severityThreshold,
        CancellationToken ct = default)
    {
        var location = new Point(longitude, latitude) { SRID = 4326 };

        var subscription = new CitizenAlertSubscription
        {
            Id = Guid.NewGuid(),
            Email = email,
            Phone = phone,
            Location = location,
            RadiusKm = radiusKm,
            EventTypes = eventTypes,
            SeverityThreshold = severityThreshold,
            IsActive = true,
            VerificationToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        _db.CitizenAlertSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created citizen alert subscription at ({Lat}, {Lon}) with radius {Radius}km",
            latitude, longitude, radiusKm);

        return subscription;
    }

    public async Task<List<CitizenAlertSubscription>> GetUserZonesAsync(Guid? userId = null, string? email = null, CancellationToken ct = default)
    {
        var query = _db.CitizenAlertSubscriptions.AsQueryable();

        if (userId.HasValue)
            query = query.Where(s => s.Email != null); // User zones identified by email
        else if (!string.IsNullOrEmpty(email))
            query = query.Where(s => s.Email == email);

        return await query.Where(s => s.IsActive).ToListAsync(ct);
    }

    public async Task<bool> VerifyZoneAsync(Guid subscriptionId, string token, CancellationToken ct = default)
    {
        var subscription = await _db.CitizenAlertSubscriptions.FindAsync(new object[] { subscriptionId }, ct);

        if (subscription == null || subscription.VerificationToken != token)
            return false;

        subscription.ConfirmedAt = DateTime.UtcNow;
        subscription.VerificationToken = null;
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task DeleteZoneAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var subscription = await _db.CitizenAlertSubscriptions.FindAsync(new object[] { subscriptionId }, ct);

        if (subscription != null)
        {
            subscription.IsActive = false;
            await _db.SaveChangesAsync(ct);
        }
    }
}