using System.Text.Json;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Risk.Dto;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace GeoRisk.API.Features.Risk;

public sealed record CreateRiskZoneCommand(string Name, string Wkt) : ICommand<RiskZoneResponse>;

public sealed class CreateRiskZoneHandler(GeoRiskDbContext db) : ICommandHandler<CreateRiskZoneCommand, RiskZoneResponse>
{
    public async Task<RiskZoneResponse> HandleAsync(CreateRiskZoneCommand cmd, CancellationToken ct)
    {
        var reader = new WKTReader();
        var geometry = reader.Read(cmd.Wkt);

        var zone = new RiskZone
        {
            Id = Guid.NewGuid(),
            Name = cmd.Name,
            Geometry = geometry as Polygon,
            RiskLevel = RiskLevel.Low,
            CalculatedAt = DateTime.UtcNow
        };

        db.RiskZones.Add(zone);
        await db.SaveChangesAsync(ct);

        return new RiskZoneResponse(zone.Id, zone.Name, zone.Geometry.AsText(), zone.RiskLevel, 0, zone.CalculatedAt);
    }
}

public static class CreateRiskZoneEndpoint
{
    public static RouteGroupBuilder MapCreateRiskZone(this RouteGroupBuilder group)
    {
        group.MapPost("/zones", async (
            HttpContext http,
            ICommandHandler<CreateRiskZoneCommand, RiskZoneResponse> h) =>
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var cmd = await JsonSerializer.DeserializeAsync<CreateRiskZoneCommand>(http.Request.Body, jsonOptions);
            if (cmd is null) return Results.BadRequest(new { error = "Request body is required" });
            var result = await h.HandleAsync(cmd, default);
            return Results.Created($"/api/risk/zones/{result.Id}", result);
        })
            .RequireAuthorization("AdminOnly");
        return group;
    }
}
