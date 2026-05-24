using System.Security.Claims;
using System.Text;
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
using GeoRisk.API.Features.Alerts;
using GeoRisk.API.Features.Insights;
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
        ?? "Host=localhost;Database=georisk;Username=georisk;Password=ChangeMe_InProduction";

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

    builder.Services.AddSingleton<RiskCalculationService>();

    // External API HTTP clients
    builder.Services.AddHttpClient<IIcnfClient, IcnfClient>(client =>
        client.BaseAddress = new Uri("https://www.icnf.pt/"));

    builder.Services.AddHttpClient<IIpmaClient, IpmaClient>(client =>
        client.BaseAddress = new Uri("https://api.ipma.pt/"));

    builder.Services.AddHttpClient<IAnepcClient, AnepcClient>(client =>
        client.BaseAddress = new Uri("https://www.procivil.pt/"));

    // Background jobs
    builder.Services.AddHostedService<EventImportJob>();
    builder.Services.AddHostedService<AIClassificationJob>();
    builder.Services.AddHostedService<PatternDetectionJob>();
    builder.Services.AddHostedService<ReportGenerationJob>();
    builder.Services.AddHostedService<AlertEvaluationJob>();

    // AI/LLM providers
    var llmProvider = builder.Configuration.GetSection("LLM")["Provider"] ?? "OpenAI";
    if (llmProvider == "Anthropic")
    {
        builder.Services.AddHttpClient<ILlmProvider, AnthropicProvider>();
    }
    else
    {
        builder.Services.AddHttpClient<ILlmProvider, OpenAIProvider>();
    }

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

    builder.Services.AddOpenApiDocument(options =>
    {
        options.Title = "GeoRisk API";
        options.Version = "1.0";
        options.DocumentName = "GeoRisk";
    });

    var app = builder.Build();

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Internal server error\"}");
        });
    });

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
    risk.MapGetRiskDashboard();
    risk.MapCalculateRisk();
    risk.MapCreateRiskZone();

    var alerts = api.MapGroup("/alerts")
        .WithTags("Alerts");
    alerts.MapAlerts();

    var insights = api.MapGroup("/insights")
        .WithTags("Insights");
    insights.MapClassifyIncident();
    insights.MapGenerateReport();
    insights.MapDetectPatterns();

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