namespace GeoRisk.API.Features.Settings.Dto;

public sealed record AiProviderConfigDto(
    int Id,
    string Provider,
    string ApiKey,
    string Model,
    string? BaseUrl,
    int MaxTokens,
    bool IsEnabled);

public sealed record AppSettingsDto(
    string ActiveProvider,
    List<AiProviderConfigDto> Providers);

public sealed record UpdateSettingsRequest(
    string ActiveProvider,
    List<AiProviderConfigDto> Providers);

public sealed record UpdateProviderConfigRequest(
    string Provider,
    string ApiKey,
    string Model,
    string? BaseUrl,
    int MaxTokens,
    bool IsEnabled);