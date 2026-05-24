namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class RiskZoneConfiguration : IEntityTypeConfiguration<RiskZone>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<RiskZone> builder)
    {
        builder.ToTable("RiskZones");

        builder.Property(e => e.Geometry)
            .HasColumnType("geometry(Polygon, 4326)");

        builder.HasIndex(e => e.Geometry)
            .HasMethod("GIST");
    }
}
