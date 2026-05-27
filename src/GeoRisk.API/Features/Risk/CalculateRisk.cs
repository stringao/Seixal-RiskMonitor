using System.Text.Json;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Risk.Dto;

using GeoRisk.API.Infrastructure.Services;
namespace GeoRisk.API.Features.Risk;

public sealed record CalculateRiskCommand(Guid? ZoneId) : ICommand<CalculateRiskResponse>;

public sealed class CalculateRiskHandler(GeoRiskDbContext db)
    : ICommandHandler<CalculateRiskCommand, CalculateRiskResponse>
{
    public async Task<CalculateRiskResponse> HandleAsync(CalculateRiskCommand cmd, CancellationToken ct)
    {
        var zones = cmd.ZoneId.HasValue
            ? await db.RiskZones.Where(z => z.Id == cmd.ZoneId.Value).ToListAsync(ct)
            : await db.RiskZones.ToListAsync(ct);

        var scores = await RiskCalculationService.CalculateZoneScoresAsync(db, ct);

        var updated = DateTime.UtcNow;
        foreach (var zone in zones)
        {
            zone.RiskLevel = RiskCalculationService.ScoreToRiskLevel(scores.GetValueOrDefault(zone.Id, 0));
            zone.CalculatedAt = updated;
        }

        await db.SaveChangesAsync(ct);

        var responses = zones.Select(z => new RiskZoneResponse(
            z.Id, z.Name, z.Geometry.AsText(),
            RiskCalculationService.ScoreToRiskLevel(scores.GetValueOrDefault(z.Id, 0)),
            Math.Round(scores.GetValueOrDefault(z.Id, 0), 2),
            updated)).ToList();

        return new CalculateRiskResponse(responses, updated);
    }
}

public static class CalculateRiskEndpoint
{
    private static readonly JsonSerializerOptions CachedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static RouteGroupBuilder MapCalculateRisk(this RouteGroupBuilder group)
    {
        group.MapPost("/calculate", async (
            HttpContext http,
            ICommandHandler<CalculateRiskCommand, CalculateRiskResponse> h) =>
        {
            var cmd = await JsonSerializer.DeserializeAsync<CalculateRiskCommand>(http.Request.Body, CachedJsonOptions);
            if (cmd is null) return Results.BadRequest(new { error = "Request body is required" });
            return Results.Ok(await h.HandleAsync(cmd, default));
        })
            .RequireAuthorization("AdminOnly");
        return group;
    }
}
