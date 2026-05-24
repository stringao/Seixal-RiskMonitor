using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class GeoEventConfiguration : IEntityTypeConfiguration<GeoEvent>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<GeoEvent> builder)
    {
        builder.ToTable("GeoEvents");

        builder.Property(e => e.Geometry)
            .HasColumnType("geometry(Point, 4326)");

        builder.HasIndex(e => e.Geometry)
            .HasMethod("GIST");

        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.Severity);
        builder.HasIndex(e => e.OccurredAt);
    }
}
