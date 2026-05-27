using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Features.Settings.Dto;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Settings;

public static class SettingsEndpoints
{
    private const string AdminOnlyPolicy = "AdminOnly";
    private const string SettingsTag = "Settings";

    public static RouteGroupBuilder MapSettings(this RouteGroupBuilder group)
    {
        group.MapGet("/settings", async (GeoRiskDbContext db) =>
        {
            var settings = await db.AppSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AppSettings { ActiveProvider = "DeepSeek" };
                db.AppSettings.Add(settings);
                await db.SaveChangesAsync();
            }

            var providers = await db.AiProviderConfigs.ToListAsync();

            return Results.Ok(new AppSettingsDto(
                settings.ActiveProvider,
                providers.Select(p => new AiProviderConfigDto(
                    p.Id, p.Provider, p.ApiKey, p.Model, p.BaseUrl, p.MaxTokens, p.IsEnabled)).ToList()));
        }).RequireAuthorization(AdminOnlyPolicy).WithTags(SettingsTag);

        group.MapPut("/settings", async (UpdateSettingsRequest request, GeoRiskDbContext db) =>
        {
            var settings = await db.AppSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AppSettings { ActiveProvider = request.ActiveProvider };
                db.AppSettings.Add(settings);
            }
            else
            {
                settings.ActiveProvider = request.ActiveProvider;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            // Update provider configs
            foreach (var providerDto in request.Providers)
            {
                var config = await db.AiProviderConfigs.FirstOrDefaultAsync(p => p.Provider == providerDto.Provider);
                if (config != null)
                {
                    config.ApiKey = providerDto.ApiKey;
                    config.Model = providerDto.Model;
                    config.BaseUrl = providerDto.BaseUrl;
                    config.MaxTokens = providerDto.MaxTokens;
                    config.IsEnabled = providerDto.IsEnabled;
                    config.UpdatedAt = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync();

            var providers = await db.AiProviderConfigs.ToListAsync();
            return Results.Ok(new AppSettingsDto(
                settings.ActiveProvider,
                providers.Select(p => new AiProviderConfigDto(
                    p.Id, p.Provider, p.ApiKey, p.Model, p.BaseUrl, p.MaxTokens, p.IsEnabled)).ToList()));
        }).RequireAuthorization(AdminOnlyPolicy).WithTags(SettingsTag);

        // Get single provider config
        group.MapGet("/settings/providers/{provider}", async (string provider, GeoRiskDbContext db) =>
        {
            var config = await db.AiProviderConfigs.FirstOrDefaultAsync(p => p.Provider == provider);
            if (config == null) return Results.NotFound();
            return Results.Ok(new AiProviderConfigDto(
                config.Id, config.Provider, config.ApiKey, config.Model, config.BaseUrl, config.MaxTokens, config.IsEnabled));
        }).RequireAuthorization(AdminOnlyPolicy).WithTags(SettingsTag);

        // Update single provider config
        group.MapPut("/settings/providers/{provider}", async (string provider, UpdateProviderConfigRequest request, GeoRiskDbContext db) =>
        {
            var config = await db.AiProviderConfigs.FirstOrDefaultAsync(p => p.Provider == provider);
            if (config == null) return Results.NotFound();

            config.ApiKey = request.ApiKey;
            config.Model = request.Model;
            config.BaseUrl = request.BaseUrl;
            config.MaxTokens = request.MaxTokens;
            config.IsEnabled = request.IsEnabled;
            config.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new AiProviderConfigDto(
                config.Id, config.Provider, config.ApiKey, config.Model, config.BaseUrl, config.MaxTokens, config.IsEnabled));
        }).RequireAuthorization(AdminOnlyPolicy).WithTags(SettingsTag);

        return group;
    }
}