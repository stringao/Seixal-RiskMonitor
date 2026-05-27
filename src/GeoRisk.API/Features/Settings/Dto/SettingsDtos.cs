namespace GeoRisk.API.Features.Settings.Dto;

public sealed record AppSettingsDto(
    string LlmProvider,
    string ApiKey,
    string ModelName,
    int MaxTokens);

public sealed record UpdateSettingsRequest(
    string LlmProvider,
    string ApiKey,
    string ModelName,
    int MaxTokens);
