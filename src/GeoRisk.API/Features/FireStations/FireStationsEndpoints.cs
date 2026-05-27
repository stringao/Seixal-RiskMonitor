using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Features.FireStations;

public static class FireStationsEndpoints
{
    private const string FireStationsTag = "FireStations";

    public static RouteGroupBuilder MapFireStations(this RouteGroupBuilder group)
    {
        group.MapGetFireStations();
        group.MapGetFireStationById();
        group.MapGetNearestFireStations();
        group.MapGetFireStationsByCounty();

        return group;
    }

    private static void MapGetFireStations(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (GeoRiskDbContext db, string? type, string? county, string? district, bool includeInactive = false) =>
        {
            var query = db.FireStations.AsNoTracking();

            if (!includeInactive)
                query = query.Where(f => f.IsActive);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(f => f.Type == type);

            if (!string.IsNullOrEmpty(county))
                query = query.Where(f => f.County == county);

            if (!string.IsNullOrEmpty(district))
                query = query.Where(f => f.District == district);

            var stations = await query
                .OrderBy(f => f.Name)
                .Select(f => new FireStationResponse(
                    f.Id,
                    f.Name,
                    f.Code,
                    f.Type,
                    f.Address,
                    f.PostalCode,
                    f.City,
                    f.District,
                    f.County,
                    f.Parish,
                    f.Geometry.Y, // latitude
                    f.Geometry.X, // longitude
                    f.Phone,
                    f.Email,
                    f.Website,
                    f.OperationalZone,
                    f.Cim,
                    f.PersonnelCount,
                    f.VehicleCount,
                    f.IsActive))
                .ToListAsync();

            return Results.Ok(new FireStationListResponse(stations));
        }).WithName("GetFireStations")
        .WithTags(FireStationsTag)
        .WithDescription("Get all fire stations with optional filters")
        .RequireAuthorization();
    }

    private static void MapGetFireStationById(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, GeoRiskDbContext db) =>
        {
            var station = await db.FireStations.AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => new FireStationResponse(
                    f.Id,
                    f.Name,
                    f.Code,
                    f.Type,
                    f.Address,
                    f.PostalCode,
                    f.City,
                    f.District,
                    f.County,
                    f.Parish,
                    f.Geometry.Y,
                    f.Geometry.X,
                    f.Phone,
                    f.Email,
                    f.Website,
                    f.OperationalZone,
                    f.Cim,
                    f.PersonnelCount,
                    f.VehicleCount,
                    f.IsActive))
                .FirstOrDefaultAsync();

            if (station is null)
                return Results.NotFound(new { message = "Fire station not found" });

            return Results.Ok(station);
        }).WithName("GetFireStationById")
        .WithTags(FireStationsTag)
        .WithDescription("Get a fire station by ID")
        .RequireAuthorization();
    }

    private static void MapGetNearestFireStations(this RouteGroupBuilder group)
    {
        group.MapGet("/nearest", async (double lat, double lng, int count = 5, bool includeInactive = false, GeoRiskDbContext? db = null) =>
        {
            if (db is null)
                return Results.BadRequest(new { message = "Database context not available" });

            var point = new Point(lng, lat) { SRID = 4326 }; // SRID 4326 = WGS84

            var query = db.FireStations.AsNoTracking()
                .Where(f => f.IsActive || includeInactive);

            // Calculate distance in meters and order by nearest
            var stations = await query
                .OrderBy(f => f.Geometry.Distance(point))
                .Take(count)
                .Select(f => new NearestFireStationResponse(
                    f.Id,
                    f.Name,
                    f.Code,
                    f.Type,
                    f.County,
                    f.Parish,
                    f.Geometry.Y,
                    f.Geometry.X,
                    f.Phone,
                    f.OperationalZone,
                    f.Geometry.Distance(point) / 1000, // Distance in km
                    f.PersonnelCount,
                    f.VehicleCount))
                .ToListAsync();

            return Results.Ok(new NearestFireStationListResponse(stations));
        }).WithName("GetNearestFireStations")
        .WithTags(FireStationsTag)
        .WithDescription("Get the nearest fire stations to a given location")
        .RequireAuthorization();
    }

    private static void MapGetFireStationsByCounty(this RouteGroupBuilder group)
    {
        group.MapGet("/by-county/{county}", async (string county, GeoRiskDbContext db, bool includeInactive = false) =>
        {
            var query = db.FireStations.AsNoTracking()
                .Where(f => string.Equals(f.County, county, StringComparison.OrdinalIgnoreCase));

            if (!includeInactive)
                query = query.Where(f => f.IsActive);

            var stations = await query
                .OrderBy(f => f.Name)
                .Select(f => new FireStationResponse(
                    f.Id,
                    f.Name,
                    f.Code,
                    f.Type,
                    f.Address,
                    f.PostalCode,
                    f.City,
                    f.District,
                    f.County,
                    f.Parish,
                    f.Geometry.Y,
                    f.Geometry.X,
                    f.Phone,
                    f.Email,
                    f.Website,
                    f.OperationalZone,
                    f.Cim,
                    f.PersonnelCount,
                    f.VehicleCount,
                    f.IsActive))
                .ToListAsync();

            return Results.Ok(new FireStationListResponse(stations));
        }).WithName("GetFireStationsByCounty")
        .WithTags(FireStationsTag)
        .WithDescription("Get fire stations by county")
        .RequireAuthorization();
    }
}

// DTOs
public record FireStationResponse(
    Guid Id,
    string Name,
    string Code,
    string Type,
    string Address,
    string PostalCode,
    string City,
    string District,
    string County,
    string? Parish,
    double Latitude,
    double Longitude,
    string? Phone,
    string? Email,
    string? Website,
    string? OperationalZone,
    string? Cim,
    int PersonnelCount,
    int VehicleCount,
    bool IsActive);

public record NearestFireStationResponse(
    Guid Id,
    string Name,
    string Code,
    string Type,
    string County,
    string? Parish,
    double Latitude,
    double Longitude,
    string? Phone,
    string? OperationalZone,
    double DistanceKm,
    int PersonnelCount,
    int VehicleCount);

public record FireStationListResponse(List<FireStationResponse> Items, int TotalCount = 0);
public record NearestFireStationListResponse(List<NearestFireStationResponse> Items);
