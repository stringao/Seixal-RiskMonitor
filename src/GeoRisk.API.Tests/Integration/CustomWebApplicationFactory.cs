using GeoRisk.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoRisk.API.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly object DbLock = new();
    private static int DbCounter;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core and Npgsql-related service registrations
            // to prevent provider conflict between Npgsql and InMemory.
            // We remove anything from the EntityFrameworkCore namespace as well
            // as Npgsql-specific registrations.
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<GeoRiskDbContext>) ||
                    d.ServiceType == typeof(GeoRiskDbContext) ||
                    d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                    d.ServiceType.Namespace?.StartsWith("Npgsql") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Build a clean isolated service provider for the InMemory provider
            var internalServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            int dbId;
            lock (DbLock) { dbId = DbCounter++; }

            services.AddDbContext<GeoRiskDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{dbId}");
                options.UseInternalServiceProvider(internalServiceProvider);
            });
        });
    }
}
