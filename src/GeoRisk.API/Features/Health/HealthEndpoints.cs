using GeoRisk.API.Infrastructure.Cache;
using StackExchange.Redis;

namespace GeoRisk.API.Features.Health;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (GeoRiskDbContext db, IConnectionMultiplexer redis, ILogger<Program> logger) =>
        {
            var health = new Dictionary<string, object>
            {
                ["status"] = "Healthy",
                ["timestamp"] = DateTime.UtcNow
            };

            try
            {
                var dbCanConnect = await db.Database.CanConnectAsync();
                health["database"] = dbCanConnect ? "Connected" : "Disconnected";
                if (!dbCanConnect) health["status"] = "Degraded";
            }
            catch (Exception ex)
            {
                health["database"] = "Error: " + ex.Message;
                health["status"] = "Unhealthy";
            }

            try
            {
                var redisDb = redis.GetDatabase();
                var ping = await redisDb.PingAsync();
                health["redis"] = $"Connected (ping: {ping.Milliseconds}ms)";
            }
            catch (Exception ex)
            {
                health["redis"] = "Error: " + ex.Message;
                health["status"] = "Degraded";
            }

            var statusCode = health["status"].ToString() == "Healthy" ? 200 :
                             health["status"].ToString() == "Degraded" ? 200 : 503;

            return statusCode == 200
                ? Results.Ok(health)
                : Results.Json(health, statusCode: statusCode);
        });

        return group;
    }
}
