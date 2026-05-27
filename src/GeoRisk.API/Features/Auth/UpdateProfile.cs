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
    GeoRiskDbContext _db,
    IPasswordHasher _hasher) : ICommandHandler<UpdateProfileCommand, UpdateProfileResult>
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
        group.MapPut("/profile", HandleUpdateProfileAsync)
            .RequireAuthorization()
            .WithTags("Auth");

        return group;
    }

    private static async Task<IResult> HandleUpdateProfileAsync(
        HttpContext http,
        UpdateProfileCommand command,
        GeoRiskDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct)
    {
        var userGuid = ExtractUserId(http);
        if (userGuid == null)
            return Results.Unauthorized();

        var user = await db.Users.FindAsync([userGuid.Value], ct);
        if (user == null)
            return Results.NotFound("User not found");

        var emailError = await ValidateAndApplyEmailChangeAsync(db, command, userGuid.Value, user, ct);
        if (emailError != null) return emailError;

        var passwordError = ValidateAndApplyPasswordChange(hasher, command, user);
        if (passwordError != null) return passwordError;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new UpdateProfileResult(user.Id, user.Email, user.Role.ToString()));
    }

    private static Guid? ExtractUserId(HttpContext http)
    {
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid)
            ? null
            : userGuid;
    }

    private static async Task<IResult?> ValidateAndApplyEmailChangeAsync(
        GeoRiskDbContext db,
        UpdateProfileCommand command,
        Guid userGuid,
        User user,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(command.Email) || command.Email == user.Email)
            return null;

        var emailTaken = await db.Users.AnyAsync(u => u.Email == command.Email && u.Id != userGuid, ct);
        if (emailTaken)
            return Results.BadRequest(new { error = "Email already in use" });

        user.Email = command.Email;
        return null;
    }

    private static IResult? ValidateAndApplyPasswordChange(
        IPasswordHasher hasher,
        UpdateProfileCommand command,
        User user)
    {
        if (string.IsNullOrEmpty(command.NewPassword))
            return null;

        if (string.IsNullOrEmpty(command.CurrentPassword))
            return Results.BadRequest(new { error = "Current password is required" });

        if (!hasher.Verify(command.CurrentPassword, user.PasswordHash))
            return Results.BadRequest(new { error = "Current password is incorrect" });

        user.PasswordHash = hasher.Hash(command.NewPassword);
        return null;
    }
}
