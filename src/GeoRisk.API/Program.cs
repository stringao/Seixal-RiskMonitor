using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using GeoRisk.API.Auth;
using GeoRisk.API.BackgroundJobs;
using GeoRisk.API.Common.CQRS;
using GeoRisk.API.Features.Auth;
using GeoRisk.API.Features.Auth.Dto;
using GeoRisk.API.Features.Events;
using GeoRisk.API.Features.Health;
using GeoRisk.API.Features.Risk;
using GeoRisk.API.Features.Risk.Forecast;
using GeoRisk.API.Features.Alerts;
using GeoRisk.API.Features.Environment;
using GeoRisk.API.Features.Insights;
using GeoRisk.API.Features.Resources;
using GeoRisk.API.Features.FireStations;
using GeoRisk.API.Features.FireSpread;
using GeoRisk.API.Features.Settings;
using GeoRisk.API.Features.Hotspots;
using GeoRisk.API.Features.Notifications;
using GeoRisk.API.Features.Reports;
using GeoRisk.API.Features.Ai;
using GeoRisk.API.Infrastructure.AI;
using GeoRisk.API.Infrastructure.Cache;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration));

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    builder.Services.AddDbContext<GeoRiskDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql =>
            npgsql.UseNetTopologySuite()));

    var jwtOptions = new JwtOptions();
    builder.Configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);
    builder.Services.AddSingleton(jwtOptions);
    builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
    builder.Services.AddScoped<IJwtService, JwtService>();

    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.Scan(scan => scan
        .FromAssemblyOf<Program>()
        .AddClasses(classes => classes
            .AssignableToAny(typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)))
        .AsImplementedInterfaces()
        .WithScopedLifetime());

builder.Services.AddScoped<AlertTriggerService>();
    builder.Services.AddSingleton(new NetTopologySuite.Geometries.GeometryFactory());
    builder.Services.AddSingleton<SrtmTerrainService>();
    builder.Services.AddSingleton<TerrainRiskService>();
    builder.Services.AddSingleton<FireSpreadCalculator>();
    builder.Services.AddScoped<LandUseService>();
    builder.Services.AddSingleton<FwiForecastService>();
    builder.Services.AddSingleton<AirQualityService>();
    builder.Services.AddSingleton<HotspotAnalysisService>();
    builder.Services.AddSingleton<EventChainAnalysisService>();
    builder.Services.AddScoped<ResourceRoutingService>();

    // RAG AI Services
    builder.Services.AddScoped<RagContextBuilder>();
    builder.Services.AddScoped<RagQueryService>();
    builder.Services.AddScoped<SimilaritySearchService>();

    // External API HTTP clients
#pragma warning disable S1075
    builder.Services.AddHttpClient<IIcnfClient, IcnfClient>(client =>
        client.BaseAddress = new Uri("https://www.icnf.pt/"));

    builder.Services.AddHttpClient<IIpmaClient, IpmaClient>(client =>
        client.BaseAddress = new Uri("https://api.ipma.pt/"));

    builder.Services.AddHttpClient<IAnepcClient, AnepcClient>(client =>
        client.BaseAddress = new Uri("https://www.procivil.pt/"));

    builder.Services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>(client =>
        client.BaseAddress = new Uri("https://api.open-meteo.com/"));

    builder.Services.AddHttpClient<IFirmsClient, FirmsClient>(client =>
        client.BaseAddress = new Uri("https://firms.modaps.eosdis.nasa.gov/"));

    builder.Services.AddHttpClient("OSRM", client =>
        client.BaseAddress = new Uri("http://localhost:5001"));
#pragma warning restore S1075

    builder.Services.AddSingleton<SatelliteFireService>();

    // Background jobs
    builder.Services.AddHostedService<EventImportJob>();
    builder.Services.AddHostedService<WeatherRiskUpdateJob>();
    builder.Services.AddHostedService<FwiForecastJob>();
    builder.Services.AddHostedService<AIClassificationJob>();
    builder.Services.AddHostedService<PatternDetectionJob>();
    builder.Services.AddHostedService<ReportGenerationJob>();
    builder.Services.AddHostedService<AlertEvaluationJob>();
    builder.Services.AddHostedService<HotspotUpdateJob>();
    builder.Services.AddHostedService<PushNotificationJob>();
    builder.Services.AddHostedService<PostIncidentAnalysisJob>();
    builder.Services.AddHostedService<DashboardGenerationJob>();
    builder.Services.AddHostedService<LandUseUpdateJob>();
    builder.Services.AddHostedService<SatelliteFireUpdateJob>();
    builder.Services.AddHostedService<ResourceStatusUpdateJob>();
    builder.Services.AddHostedService<EventChainAnalysisJob>();
    builder.Services.AddScoped<PushNotificationService>();
    builder.Services.AddScoped<AlertNotificationService>();
    builder.Services.AddScoped<PostIncidentAnalysisService>();
    builder.Services.AddScoped<IDashboardGeneratorService, DashboardGeneratorService>();
    builder.Services.AddScoped<AlertRuleEvaluationEngine>();

    builder.Services.AddHttpClient("WebPush", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // AI/LLM providers - read from database at runtime
    builder.Services.AddScoped<OllamaProvider>(sp =>
        new OllamaProvider(new HttpClient(), sp.GetRequiredService<ILlmSettingsService>()));
    builder.Services.AddScoped<DeepSeekProvider>(sp =>
        new DeepSeekProvider(new HttpClient(), sp.GetRequiredService<ILlmSettingsService>()));
    builder.Services.AddScoped<QwenProvider>(sp =>
        new QwenProvider(new HttpClient(), sp.GetRequiredService<ILlmSettingsService>()));
    builder.Services.AddScoped<AnthropicProvider>(sp =>
        new AnthropicProvider(new HttpClient(), sp.GetRequiredService<ILlmSettingsService>()));
    builder.Services.AddScoped<OpenAIProvider>(sp =>
        new OpenAIProvider(new HttpClient(), sp.GetRequiredService<ILlmSettingsService>()));
    builder.Services.AddScoped<LlmProviderFactory>();
    builder.Services.AddScoped<ILlmProvider>(sp => sp.GetRequiredService<LlmProviderFactory>());

    // LLM settings service (for provider configs)
    builder.Services.AddScoped<ILlmSettingsService, LlmSettingsService>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var redisConn = builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";
        return ConnectionMultiplexer.Connect(redisConn);
    });
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
    builder.Services.AddSingleton<ISyncStatusService, SyncStatusService>();

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("AnalystOrAdmin", policy => policy.RequireRole("Analyst", "Admin"))
        .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    builder.Services.AddCors(options =>
    {
        var frontendOrigin = builder.Configuration.GetSection("CORS")["FrontendOrigin"] ?? "http://localhost:3000";
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(frontendOrigin)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials());
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;
        options.AddPolicy("fixed", context =>
            RateLimitPartition.GetFixedWindowLimiter("fixed", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
        options.AddPolicy("auth", context =>
            RateLimitPartition.GetFixedWindowLimiter("auth", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
        options.OnRejected = async (context, cancellation) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            await context.HttpContext.Response.WriteAsync("{\"error\":\"Too many requests. Please try again later.\"}", cancellation);
        };
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApiDocument(options =>
    {
        options.Title = "GeoRisk API";
        options.Version = "1.0";
        options.DocumentName = "GeoRisk";
    });

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddProblemDetails();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Internal server error\"}");
            });
        });
    }

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseOpenApi();
        app.UseSwaggerUi();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    var api = app.MapGroup("/api");

    api.MapGroup("/health")
       .WithTags("Health")
       .MapHealthEndpoints()
       .MapGetJobStatuses();

    var auth = api.MapGroup("/auth")
        .WithTags("Auth");
    auth.MapRegister();
    auth.MapLogin();
    auth.MapRefresh();
    auth.MapUpdateProfile();

    api.MapGet("/me", (HttpContext http) =>
    {
        var user = http.User;
        return Results.Ok(new
        {
            Id = user.FindFirstValue("sub"),
            Email = user.FindFirstValue(System.Security.Claims.ClaimTypes.Email),
            Role = user.FindFirstValue(System.Security.Claims.ClaimTypes.Role)
        });
    }).RequireAuthorization().WithTags("Auth");

    var events = api.MapGroup("/events")
        .WithTags("Events");
    events.MapGetEvents();
    events.MapGetEventById();
    events.MapCreateEvent();
    events.MapImportEvents();

    var risk = api.MapGroup("/risk")
        .WithTags("Risk");
    risk.MapGetRiskZones();
    risk.MapGetDetailedRiskZones();
    risk.MapGetRiskDashboard();
    risk.MapCalculateRisk();
    risk.MapCreateRiskZone();
    risk.MapTerrainEndpoints();
    risk.MapFuelEndpoints();
    risk.MapRiskForecast();

    var fireSpread = api.MapGroup("/fires")
        .WithTags("FireSpread");
    fireSpread.MapFireSpread();

    var alerts = api.MapGroup("/alerts")
        .WithTags("Alerts");
    alerts.MapAlerts();

    var insights = api.MapGroup("/insights")
        .WithTags("Insights");
    insights.MapClassifyIncident();
    insights.MapGenerateReport();
    insights.MapDetectPatterns();
    HotspotsEndpointExtensions.MapHotspots(insights);
    insights.MapDashboardEndpoints();
    insights.MapStatistics();
    insights.MapChainAnalysis();

    var reports = api.MapGroup("/reports")
        .WithTags("Reports");
    reports.MapFireReports();

    var ai = api.MapGroup("/ai")
        .WithTags("AI");
    ai.MapAiEndpoints();

    var fireStations = api.MapGroup("/fire-stations")
        .WithTags("FireStations");
    fireStations.MapFireStations();

    var resources = api.MapGroup("/resources")
        .WithTags("Resources");
    resources.MapResources();

    api.MapSettings();

    var notifications = api.MapGroup("/notifications")
        .WithTags("Notifications");
    notifications.MapNotifications();

    var environment = api.MapGroup("/environment")
        .WithTags("Environment");
    environment.MapAirQualityEndpoints();
    environment.MapSatelliteEndpoints();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoRiskDbContext>();
        await db.Database.MigrateAsync();
        await SeedData.SeedAsync(db);
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
