using System.Text.Json;
using GeoRisk.API.Auth;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Auth.Dto;

namespace GeoRisk.API.Features.Auth;

public sealed record LoginQuery(string Email, string Password)
    : IQuery<AuthResponse>;

public sealed class LoginHandler(
    GeoRiskDbContext db,
    IPasswordHasher hasher,
    IJwtService jwt) : IQueryHandler<LoginQuery, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(LoginQuery query, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == query.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        if (!hasher.Verify(query.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = jwt.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        db.RefreshTokens.Add(refreshToken);
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            jwt.GenerateAccessToken(user),
            refreshToken.Token,
            new UserInfo(user.Id, user.Email, user.Role.ToString()));
    }
}

public static class LoginEndpoint
{
    public static RouteGroupBuilder MapLogin(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (
            HttpContext http,
            IQueryHandler<LoginQuery, AuthResponse> handler) =>
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            try
            {
                var query = await JsonSerializer.DeserializeAsync<LoginQuery>(http.Request.Body, jsonOptions);
                if (query is null) return Results.BadRequest(new { error = "Request body is required" });
                var result = await handler.HandleAsync(query, default);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 401);
            }
        });

        return group;
    }
}
