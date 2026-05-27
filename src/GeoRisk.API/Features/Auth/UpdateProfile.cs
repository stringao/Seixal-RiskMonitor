using GeoRisk.API.Auth;
using GeoRisk.API.Common.CQRS;
using System.Security.Claims;

namespace GeoRisk.API.Features.Auth;

public sealed record UpdateProfileCommand(
    string? Email,
    string? CurrentPassword,
    string? NewPassword) : ICommand<UpdateProfileResult>;

public sealed record UpdateProfileResult(
    Guid Id,
    string Email,
    string Role);

public sealed class UpdateProfileHandler(
    GeoRiskDbContext db,
    IPasswordHasher hasher) : ICommandHandler<UpdateProfileCommand, UpdateProfileResult>
{
    public async Task<UpdateProfileResult> HandleAsync(UpdateProfileCommand cmd, CancellationToken ct)
    {
        throw new NotImplementedException("Handler needs context - use the endpoint implementation");
    }
}

public static class UpdateProfileEndpoint
{
    public static RouteGroupBuilder MapUpdateProfile(this RouteGroupBuilder group)
    {
        group.MapPut("/profile", async (
            HttpContext http,
            UpdateProfileCommand command,
            GeoRiskDbContext db,
            IPasswordHasher hasher,
            CancellationToken ct) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.FindAsync([userGuid], ct);
            if (user == null)
            {
                return Results.NotFound("User not found");
            }

            if (!string.IsNullOrEmpty(command.Email) && command.Email != user.Email)
            {
                if (await db.Users.AnyAsync(u => u.Email == command.Email && u.Id != userGuid, ct))
                {
                    return Results.BadRequest(new { error = "Email already in use" });
                }
                user.Email = command.Email;
            }

            if (!string.IsNullOrEmpty(command.NewPassword))
            {
                if (string.IsNullOrEmpty(command.CurrentPassword))
                {
                    return Results.BadRequest(new { error = "Current password is required" });
                }
                if (!hasher.Verify(command.CurrentPassword, user.PasswordHash))
                {
                    return Results.BadRequest(new { error = "Current password is incorrect" });
                }
                user.PasswordHash = hasher.Hash(command.NewPassword);
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new UpdateProfileResult(user.Id, user.Email, user.Role.ToString()));
        }).RequireAuthorization().WithTags("Auth");

        return group;
    }
}
