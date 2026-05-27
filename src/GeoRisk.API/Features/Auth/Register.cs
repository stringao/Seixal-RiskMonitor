using System.Text.Json;
using GeoRisk.API.Auth;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Auth.Dto;

namespace GeoRisk.API.Features.Auth;

public sealed record RegisterCommand(string Email, string Password, string Role)
    : ICommand<AuthResponse>;

public sealed class RegisterHandler(
    GeoRiskDbContext db,
    IPasswordHasher hasher,
    IJwtService jwt) : ICommandHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(RegisterCommand cmd, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == cmd.Email, ct))
            throw new InvalidOperationException("Email already registered");

        if (!Enum.TryParse<UserRole>(cmd.Role, out var role))
            throw new InvalidOperationException($"Invalid role: {cmd.Role}");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = cmd.Email,
            PasswordHash = hasher.Hash(cmd.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = jwt.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            jwt.GenerateAccessToken(user),
            refreshToken.Token,
            new UserInfo(user.Id, user.Email, user.Role.ToString()));
    }
}

public static class RegisterEndpoint
{
    public static RouteGroupBuilder MapRegister(this RouteGroupBuilder group)
    {
        group.MapPost("/register", async (
            HttpContext http,
            ICommandHandler<RegisterCommand, AuthResponse> handler) =>
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var cmd = await JsonSerializer.DeserializeAsync<RegisterCommand>(http.Request.Body, jsonOptions);
            if (cmd is null) return Results.BadRequest(new { error = "Request body is required" });
            var result = await handler.HandleAsync(cmd, default);
            return Results.Ok(result);
        });

        return group;
    }
}
