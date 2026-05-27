using GeoRisk.API.Domain.Entities;
using GeoRisk.API.Features.Settings.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Features.Settings;

public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettings(this RouteGroupBuilder group)
    {
        group.MapGet("/settings", async (GeoRiskDbContext db) =>
        {
            var settings = await db.AppSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AppSettings
                {
                    LlmProvider = "OpenAI",
                    ApiKey = "",
                    ModelName = "gpt-4o",
                    MaxTokens = 1024
                };
                db.AppSettings.Add(settings);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new AppSettingsDto(
                settings.LlmProvider,
                settings.ApiKey,
                settings.ModelName,
                settings.MaxTokens));
        }).RequireAuthorization("AdminOnly").WithTags("Settings");

        group.MapPut("/settings", async (UpdateSettingsRequest request, GeoRiskDbContext db) =>
        {
            var settings = await db.AppSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AppSettings
                {
                    LlmProvider = request.LlmProvider,
                    ApiKey = request.ApiKey,
                    ModelName = request.ModelName,
                    MaxTokens = request.MaxTokens
                };
                db.AppSettings.Add(settings);
            }
            else
            {
                settings.LlmProvider = request.LlmProvider;
                settings.ApiKey = request.ApiKey;
                settings.ModelName = request.ModelName;
                settings.MaxTokens = request.MaxTokens;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new AppSettingsDto(
                settings.LlmProvider,
                settings.ApiKey,
                settings.ModelName,
                settings.MaxTokens));
        }).RequireAuthorization("AdminOnly").WithTags("Settings");

        return group;
    }
}
