using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class FireHotspotConfiguration : IEntityTypeConfiguration<FireHotspot>
{
    public void Configure(EntityTypeBuilder<FireHotspot> builder)
    {
        builder.ToTable("FireHotspots");

        builder.Property(e => e.Location)
            .HasColumnType("geometry(Point, 4326)");

        builder.Property(e => e.CellGeometry)
            .HasColumnType("geometry(Polygon, 4326)");

        builder.HasIndex(e => e.Location)
            .HasMethod("GIST");

        builder.HasIndex(e => e.GridCellId);

        builder.HasIndex(e => e.RiskLevel);

        builder.HasMany(e => e.Alerts)
            .WithOne(a => a.FireHotspot)
            .HasForeignKey(a => a.FireHotspotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}