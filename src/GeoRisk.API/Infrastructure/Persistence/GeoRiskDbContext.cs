namespace GeoRisk.API.Infrastructure.Persistence;

public class GeoRiskDbContext(DbContextOptions<GeoRiskDbContext> options) : DbContext(options)
{
    public DbSet<GeoEvent> GeoEvents => Set<GeoEvent>();
    public DbSet<RiskZone> RiskZones => Set<RiskZone>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GeoRiskDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity is GeoEvent geoEvent)
            {
                geoEvent.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    geoEvent.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
