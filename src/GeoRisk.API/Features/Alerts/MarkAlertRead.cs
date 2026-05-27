using System.Text.Json;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Alerts.Dto;
using GeoRisk.API.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Alerts;

public sealed record MarkAlertReadCommand(
    List<Guid>? AlertIds,
    bool MarkAllRead) : ICommand<MarkAlertReadResponse>;

public sealed class MarkAlertReadHandler(
    GeoRiskDbContext db,
    ICacheService cache) : ICommandHandler<MarkAlertReadCommand, MarkAlertReadResponse>
{
    public async Task<MarkAlertReadResponse> HandleAsync(MarkAlertReadCommand command, CancellationToken ct)
    {
        if (command.MarkAllRead)
        {
            await db.Alerts.Where(a => !a.IsRead).ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.IsRead, true), ct);
        }
        else if (command.AlertIds is not null && command.AlertIds.Count != 0)
        {
            await db.Alerts.Where(a => command.AlertIds.Contains(a.Id) && !a.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.IsRead, true), ct);
        }

        // Invalidate cache after updates
        await cache.RemoveByPrefixAsync("alerts:", ct);

        var count = await db.Alerts.CountAsync(a => a.IsRead, ct);
        return new MarkAlertReadResponse(count);
    }
}

public static class MarkAlertReadEndpoint
{
    public static RouteGroupBuilder MapMarkAlertRead(this RouteGroupBuilder group)
    {
        group.MapPost("/read", async (
            HttpContext http,
            ICommandHandler<MarkAlertReadCommand, MarkAlertReadResponse> handler) =>
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var request = await JsonSerializer.DeserializeAsync<MarkAlertReadRequest>(http.Request.Body, jsonOptions);
            if (request is null) return Results.BadRequest(new { error = "Request body is required" });
            var result = await handler.HandleAsync(new MarkAlertReadCommand(request.AlertIds, request.MarkAllRead ?? false), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin");
        return group;
    }
}