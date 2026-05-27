using System.Security.Claims;
using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Notifications;

public static class NotificationsEndpoints
{
    private const string TagName = "Notifications";

    public static RouteGroupBuilder MapNotifications(this RouteGroupBuilder group)
    {
        group.MapPostSubscribe();
        group.MapDeleteUnsubscribe();
        group.MapPutUpdateSubscription();
        group.MapPostZones();
        group.MapGetZones();
        group.MapDeleteZone();
        group.MapPostVerifyZone();
        group.MapGetPreferences();
        group.MapPutPreferences();

        return group;
    }

    private static void MapPostSubscribe(this RouteGroupBuilder group)
    {
        group.MapPost("/subscribe", async (
            HttpContext http,
            SubscribeRequest request,
            PushNotificationService service,
            GeoRiskDbContext db) =>
        {
            var userId = GetUserId(http);
            var subscription = await service.SubscribeAsync(
                userId,
                request.Endpoint,
                request.P256dh,
                request.Auth,
                request.DeviceType ?? "browser");

            return Results.Ok(new { subscriptionId = subscription.Id });
        }).WithTags(TagName);
    }

    private static void MapDeleteUnsubscribe(this RouteGroupBuilder group)
    {
        group.MapDelete("/unsubscribe", async (
            HttpContext http,
            [FromBody] UnsubscribeRequest request,
            PushNotificationService service) =>
        {
            await service.UnsubscribeAsync(request.Endpoint);
            return Results.Ok(new { success = true });
        }).WithTags(TagName);
    }

    private static void MapPutUpdateSubscription(this RouteGroupBuilder group)
    {
        group.MapPut("/update", async (
            HttpContext http,
            UpdateSubscriptionRequest request,
            PushNotificationService service) =>
        {
            await service.UpdateSubscriptionAsync(
                request.OldEndpoint,
                request.NewEndpoint,
                request.P256dh,
                request.Auth);

            return Results.Ok(new { success = true });
        }).WithTags(TagName);
    }

    private static void MapPostZones(this RouteGroupBuilder group)
    {
        group.MapPost("/zones", async (
            HttpContext http,
            CreateZoneRequest request,
            AlertNotificationService service,
            GeoRiskDbContext db) =>
        {
            var email = GetUserEmail(http);

            var subscription = await service.CreateZoneSubscriptionAsync(
                email,
                request.Phone,
                request.Latitude,
                request.Longitude,
                request.RadiusKm ?? 10.0,
                request.EventTypes ?? new List<EventType>(),
                request.SeverityThreshold ?? RiskLevel.Medium);

            return Results.Ok(new
            {
                zoneId = subscription.Id,
                verificationToken = subscription.VerificationToken
            });
        }).WithTags(TagName);
    }

    private static void MapGetZones(this RouteGroupBuilder group)
    {
        group.MapGet("/zones", async (
            HttpContext http,
            GeoRiskDbContext db) =>
        {
            var userId = GetUserId(http);
            var email = GetUserEmail(http);

            var query = db.CitizenAlertSubscriptions.Where(s => s.IsActive);

            if (userId.HasValue)
                query = query.Where(s => s.Email != null);
            else if (!string.IsNullOrEmpty(email))
                query = query.Where(s => s.Email == email);

            var zones = await query
                .Select(s => new ZoneResponse(
                    s.Id,
                    s.Email,
                    s.Phone,
                    s.Location.Y,
                    s.Location.X,
                    s.RadiusKm,
                    s.EventTypes.Select(e => e.ToString()).ToList(),
                    s.SeverityThreshold.ToString(),
                    s.IsActive,
                    s.ConfirmedAt,
                    s.CreatedAt))
                .ToListAsync();

            return Results.Ok(new { zones });
        }).WithTags(TagName);
    }

    private static void MapDeleteZone(this RouteGroupBuilder group)
    {
        group.MapDelete("/zones/{id:guid}", async (
            Guid id,
            AlertNotificationService service) =>
        {
            await service.DeleteZoneAsync(id);
            return Results.Ok(new { success = true });
        }).WithTags(TagName);
    }

    private static void MapPostVerifyZone(this RouteGroupBuilder group)
    {
        group.MapPost("/zones/{id:guid}/verify", async (
            Guid id,
            [FromBody] VerifyZoneRequest request,
            AlertNotificationService service) =>
        {
            var success = await service.VerifyZoneAsync(id, request.Token);
            return success
                ? Results.Ok(new { success = true, message = "Contact verified successfully" })
                : Results.BadRequest(new { success = false, message = "Invalid verification token" });
        }).WithTags(TagName);
    }

    private static void MapGetPreferences(this RouteGroupBuilder group)
    {
        group.MapGet("/preferences", async (
            HttpContext http,
            GeoRiskDbContext db) =>
        {
            var userId = GetUserId(http);
            var email = GetUserEmail(http);

            var subscriptions = await db.PushSubscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            var citizenZones = await db.CitizenAlertSubscriptions
                .Where(s => s.Email == email && s.IsActive)
                .ToListAsync();

            return Results.Ok(new
            {
                pushSubscriptions = subscriptions.Select(s => new
                {
                    s.Id,
                    s.DeviceType,
                    s.IsActive,
                    s.LastNotifiedAt
                }).ToList(),
                citizenZones = citizenZones.Select(z => new
                {
                    z.Id,
                    z.RadiusKm,
                    z.EventTypes,
                    SeverityThreshold = z.SeverityThreshold.ToString()
                }).ToList()
            });
        }).WithTags(TagName);
    }

    private static void MapPutPreferences(this RouteGroupBuilder group)
    {
        group.MapPut("/preferences", async (
            HttpContext http,
            UpdatePreferencesRequest request,
            GeoRiskDbContext db) =>
        {
            // Update severity threshold for citizen zones
            if (request.SeverityThreshold != null)
            {
                var email = GetUserEmail(http);
                var zones = await db.CitizenAlertSubscriptions
                    .Where(z => z.Email == email)
                    .ToListAsync();

                foreach (var zone in zones)
                {
                    zone.SeverityThreshold = request.SeverityThreshold.Value;
                }

                await db.SaveChangesAsync();
            }

            return Results.Ok(new { success = true });
        }).WithTags(TagName);
    }

    private static Guid? GetUserId(HttpContext http)
    {
        var userIdClaim = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string? GetUserEmail(HttpContext http)
    {
        return http.User.FindFirst(ClaimTypes.Email)?.Value
            ?? http.User.FindFirst("email")?.Value;
    }
}

// Request/Response DTOs
public sealed record SubscribeRequest(
    string Endpoint,
    string P256dh,
    string Auth,
    string? DeviceType);

public sealed record UnsubscribeRequest(string Endpoint);

public sealed record UpdateSubscriptionRequest(
    string OldEndpoint,
    string? NewEndpoint,
    string? P256dh,
    string? Auth);

public sealed record CreateZoneRequest(
    double Latitude,
    double Longitude,
    double? RadiusKm,
    List<EventType>? EventTypes,
    RiskLevel? SeverityThreshold,
    string? Phone);

public sealed record VerifyZoneRequest(string Token);

public sealed record UpdatePreferencesRequest(RiskLevel? SeverityThreshold);

public sealed record ZoneResponse(
    Guid Id,
    string? Email,
    string? Phone,
    double Latitude,
    double Longitude,
    double RadiusKm,
    List<string> EventTypes,
    string SeverityThreshold,
    bool IsActive,
    DateTime? ConfirmedAt,
    DateTime CreatedAt);