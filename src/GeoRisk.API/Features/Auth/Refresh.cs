using System.Security.Claims;
using GeoRisk.API.Auth;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Auth.Dto;

namespace GeoRisk.API.Features.Auth;

public sealed record RefreshCommand(string AccessToken, string RefreshToken)
    : ICommand<AuthResponse>;

public sealed class RefreshHandler(
    GeoRiskDbContext db,
    IJwtService jwt) : ICommandHandler<RefreshCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(RefreshCommand cmd, CancellationToken ct)
    {
        var principal = jwt.GetPrincipalFromExpiredToken(cmd.AccessToken)
            ?? throw new UnauthorizedAccessException("Invalid access token");

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Invalid token claims");

        var userId = Guid.Parse(userIdClaim);

        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == cmd.RefreshToken && rt.UserId == userId, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");

        if (storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired or revoked");

        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UnauthorizedAccessException("User not found");

        storedToken.IsRevoked = true;

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = jwt.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        db.RefreshTokens.Add(newRefreshToken);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            jwt.GenerateAccessToken(user),
            newRefreshToken.Token,
            new UserInfo(user.Id, user.Email, user.Role.ToString()));
    }
}

public static class RefreshEndpoint
{
    public static RouteGroupBuilder MapRefresh(this RouteGroupBuilder group)
    {
        group.MapPost("/refresh", async (
            RefreshCommand cmd,
            ICommandHandler<RefreshCommand, AuthResponse> handler) =>
        {
            var result = await handler.HandleAsync(cmd, default);
            return Results.Ok(result);
        });

        return group;
    }
}
