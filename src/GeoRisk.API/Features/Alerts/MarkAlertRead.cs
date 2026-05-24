using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Alerts.Dto;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Alerts;

public sealed record MarkAlertReadCommand(
    List<Guid>? AlertIds,
    bool MarkAllRead) : ICommand<MarkAlertReadResponse>;

public sealed class MarkAlertReadHandler(GeoRiskDbContext db)
    : ICommandHandler<MarkAlertReadCommand, MarkAlertReadResponse>
{
    public async Task<MarkAlertReadResponse> HandleAsync(MarkAlertReadCommand command, CancellationToken ct)
    {
        if (command.MarkAllRead)
        {
            await db.Alerts.Where(a => !a.IsRead).ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.IsRead, true), ct);
            return new MarkAlertReadResponse(await db.Alerts.CountAsync(a => a.IsRead, ct));
        }

        if (command.AlertIds is not null && command.AlertIds.Count != 0)
        {
            await db.Alerts.Where(a => command.AlertIds.Contains(a.Id) && !a.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.IsRead, true), ct);
            return new MarkAlertReadResponse(command.AlertIds.Count);
        }

        return new MarkAlertReadResponse(0);
    }
}

public static class MarkAlertReadEndpoint
{
    public static RouteGroupBuilder MapMarkAlertRead(this RouteGroupBuilder group)
    {
        group.MapPost("/read", async (
            MarkAlertReadRequest request,
            ICommandHandler<MarkAlertReadCommand, MarkAlertReadResponse> handler) =>
        {
            var result = await handler.HandleAsync(new MarkAlertReadCommand(request.AlertIds, request.MarkAllRead ?? false), default);
            return Results.Ok(result);
        }).RequireAuthorization("AnalystOrAdmin");
        return group;
    }
}