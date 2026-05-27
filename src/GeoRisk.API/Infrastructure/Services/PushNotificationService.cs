using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.Services;

public class PushNotificationService
{
    private readonly GeoRiskDbContext _db;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly VapidSettings _vapidSettings;
    private readonly HttpClient _httpClient;

    public PushNotificationService(
        GeoRiskDbContext db,
        ILogger<PushNotificationService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _logger = logger;
        _vapidSettings = new VapidSettings
        {
            Subject = configuration["WebPush:Subject"] ?? "mailto:notifications@georisk.pt",
            PublicKey = configuration["WebPush:PublicKey"] ?? "",
            PrivateKey = configuration["WebPush:PrivateKey"] ?? ""
        };
        _httpClient = httpClientFactory.CreateClient("WebPush");
    }

    public async Task<Domain.Entities.PushSubscription> SubscribeAsync(
        Guid? userId,
        string endpoint,
        string p256dh,
        string auth,
        string deviceType,
        CancellationToken ct = default)
    {
        // Check if subscription already exists
        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);

        if (existing != null)
        {
            existing.UserId = userId;
            existing.P256dh = p256dh;
            existing.Auth = auth;
            existing.DeviceType = deviceType;
            existing.IsActive = true;
            existing.LastNotifiedAt = null;
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        var subscription = new Domain.Entities.PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            DeviceType = deviceType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.PushSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("New push subscription registered for {DeviceType}", deviceType);
        return subscription;
    }

    public async Task UnsubscribeAsync(string endpoint, CancellationToken ct = default)
    {
        var subscription = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);

        if (subscription != null)
        {
            subscription.IsActive = false;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Push subscription deactivated: {Endpoint}", endpoint);
        }
    }

    public async Task UpdateSubscriptionAsync(
        string endpoint,
        string? newEndpoint,
        string? p256dh,
        string? auth,
        CancellationToken ct = default)
    {
        var subscription = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);

        if (subscription == null)
            return;

        if (newEndpoint != null)
            subscription.Endpoint = newEndpoint;
        if (p256dh != null)
            subscription.P256dh = p256dh;
        if (auth != null)
            subscription.Auth = auth;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> SendPushNotificationAsync(
        Domain.Entities.PushSubscription subscription,
        string title,
        string body,
        string? icon = null,
        string? badge = null,
        object? data = null,
        CancellationToken ct = default)
    {
        if (!subscription.IsActive)
            return false;

        try
        {
            // Create Web Push payload
            var payload = new
            {
                notification = new
                {
                    title,
                    body,
                    icon = icon ?? "/bell-icon.png",
                    badge = badge ?? "/badge-icon.png",
                    data = data ?? new { }
                }
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            // Create ECDH P-256 public key from VAPID public key
            // For simplicity, we'll use the standard Web Push protocol via HTTP request
            // The WebPush library has compatibility issues with .NET 10, so we'll make direct HTTP calls

            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Endpoint);
            request.Content = new ByteArrayContent(payloadBytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            // Add VAPID authentication header
            var vapidAuth = CreateVapidAuthHeader();
            request.Headers.TryAddWithoutValidation("Authorization", vapidAuth);
            request.Headers.TryAddWithoutValidation("Content-Encoding", "aes128gcm");
            request.Headers.TryAddWithoutValidation("TTL", "0");

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                subscription.LastNotifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                _logger.LogDebug("Push notification sent to {Endpoint}", subscription.Endpoint);
                return true;
            }

            var statusCode = (int)response.StatusCode;
            if (statusCode == 410 || statusCode == 404)
            {
                // Gone or Not Found - subscription is no longer valid
                subscription.IsActive = false;
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Push subscription expired: {Endpoint}", subscription.Endpoint);
            }

            _logger.LogWarning("Push notification failed to {Endpoint}: {StatusCode}", subscription.Endpoint, statusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send push notification to {Endpoint}", subscription.Endpoint);
            return false;
        }
    }

    private string CreateVapidAuthHeader()
    {
        // Simplified VAPID auth - in production, use proper JWT signing with WebPush library
        return "WebPush " + _vapidSettings.PublicKey;
    }

    public async Task<int> CleanupExpiredSubscriptionsAsync(CancellationToken ct = default)
    {
        // Remove subscriptions that haven't received notifications in 6 months
        var cutoff = DateTime.UtcNow.AddMonths(-6);
        var expired = await _db.PushSubscriptions
            .Where(s => s.IsActive && s.LastNotifiedAt != null && s.LastNotifiedAt < cutoff)
            .ToListAsync(ct);

        foreach (var sub in expired)
        {
            sub.IsActive = false;
        }

        await _db.SaveChangesAsync(ct);
        return expired.Count;
    }

    public async Task<List<Domain.Entities.PushSubscription>> GetActiveSubscriptionsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.PushSubscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync(ct);
    }

    public async Task<List<Domain.Entities.PushSubscription>> GetActiveSubscriptionsInAreaAsync(
        double latitude,
        double longitude,
        double radiusKm,
        CancellationToken ct = default)
    {
        // For Web Push subscriptions, we don't have location data
        // This is used when an alert is triggered and we want to notify all
        // subscribers in a geographic area - but push subscriptions don't store location
        // So this method is mainly for future use with user location tracking
        return await _db.PushSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync(ct);
    }
}

public class VapidSettings
{
    public string Subject { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}