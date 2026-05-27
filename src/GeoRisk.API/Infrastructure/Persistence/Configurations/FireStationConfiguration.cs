using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class FireStationConfiguration : IEntityTypeConfiguration<FireStation>
{
    public void Configure(EntityTypeBuilder<FireStation> builder)
    {
        builder.ToTable("FireStations");

        builder.Property(e => e.Geometry)
            .HasColumnType("geometry(Point, 4326)");

        builder.HasIndex(e => e.Geometry)
            .HasMethod("GIST");

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.County);
        builder.HasIndex(e => e.District);
        builder.HasIndex(e => e.Type);
    }
}
