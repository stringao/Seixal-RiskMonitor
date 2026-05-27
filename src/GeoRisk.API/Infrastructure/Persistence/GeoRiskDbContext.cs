using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.Persistence;

public class GeoRiskDbContext(DbContextOptions<GeoRiskDbContext> options) : DbContext(options)
{
    public DbSet<GeoEvent> GeoEvents => Set<GeoEvent>();
    public DbSet<RiskZone> RiskZones => Set<RiskZone>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<FireStation> FireStations => Set<FireStation>();
    public DbSet<AiProviderConfig> AiProviderConfigs => Set<AiProviderConfig>();
    public DbSet<WeatherRiskDataPoint> WeatherRiskDataPoints => Set<WeatherRiskDataPoint>();
    public DbSet<AirQualityDataPoint> AirQualityDataPoints => Set<AirQualityDataPoint>();
    public DbSet<FireSpreadPrediction> FireSpreadPredictions => Set<FireSpreadPrediction>();
    public DbSet<FireHotspot> FireHotspots => Set<FireHotspot>();
    public DbSet<HotspotAlert> HotspotAlerts => Set<HotspotAlert>();
    public DbSet<TerrainAnalysis> TerrainAnalyses => Set<TerrainAnalysis>();
    public DbSet<FwiForecast> FwiForecasts => Set<FwiForecast>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<CitizenAlertSubscription> CitizenAlertSubscriptions => Set<CitizenAlertSubscription>();
    public DbSet<NotificationQueueItem> NotificationQueueItems => Set<NotificationQueueItem>();
    public DbSet<FireReport> FireReports => Set<FireReport>();
    public DbSet<SeasonalStatistics> SeasonalStatistics => Set<SeasonalStatistics>();
    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();
    public DbSet<DashboardCache> DashboardCaches => Set<DashboardCache>();
    public DbSet<AiContextCache> AiContextCaches => Set<AiContextCache>();
    public DbSet<FireResource> FireResources => Set<FireResource>();
    public DbSet<ResourceDispatch> ResourceDispatches => Set<ResourceDispatch>();
    public DbSet<SatelliteFireImage> SatelliteFireImages => Set<SatelliteFireImage>();
    public DbSet<LandUseDataPoint> LandUseDataPoints => Set<LandUseDataPoint>();
    public DbSet<EventChainAnalysis> EventChainAnalyses => Set<EventChainAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GeoRiskDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            ApplyTimestampForEntity(entry.Entity, entry.State);
        }
    }

    private static void ApplyTimestampForEntity(object entity, EntityState state)
    {
        switch (entity)
        {
            case GeoEvent geoEvent:
                geoEvent.UpdatedAt = DateTime.UtcNow;
                if (state == EntityState.Added)
                    geoEvent.CreatedAt = DateTime.UtcNow;
                break;
            case FireStation fireStation:
                fireStation.UpdatedAt = DateTime.UtcNow;
                if (state == EntityState.Added)
                    fireStation.CreatedAt = DateTime.UtcNow;
                break;
            case AiProviderConfig config:
                config.UpdatedAt = DateTime.UtcNow;
                if (state == EntityState.Added)
                    config.CreatedAt = DateTime.UtcNow;
                break;
            case TerrainAnalysis terrain:
                terrain.CalculatedAt = DateTime.UtcNow;
                break;
            case FireHotspot hotspot:
                hotspot.LastUpdated = DateTime.UtcNow;
                break;
            case FireReport report:
                report.UpdatedAt = DateTime.UtcNow;
                if (state == EntityState.Added)
                    report.CreatedAt = DateTime.UtcNow;
                break;
            case SeasonalStatistics stats:
                stats.CalculatedAt = DateTime.UtcNow;
                break;
            case FireResource resource:
                resource.UpdatedAt = DateTime.UtcNow;
                if (state == EntityState.Added)
                    resource.CreatedAt = DateTime.UtcNow;
                break;
            case SatelliteFireImage satelliteImage:
                if (state == EntityState.Added)
                    satelliteImage.CreatedAt = DateTime.UtcNow;
                break;
        }
    }
}
