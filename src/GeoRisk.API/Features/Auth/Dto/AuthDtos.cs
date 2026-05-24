namespace GeoRisk.API.Features.Auth.Dto;

public sealed record RegisterRequest(string Email, string Password, string Role);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string AccessToken, string RefreshToken);

public sealed record AuthResponse(string AccessToken, string RefreshToken, UserInfo User);

public sealed record UserInfo(Guid Id, string Email, string Role);
