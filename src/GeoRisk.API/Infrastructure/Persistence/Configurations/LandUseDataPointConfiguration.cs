using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class LandUseDataPointConfiguration : IEntityTypeConfiguration<LandUseDataPoint>
{
    public void Configure(EntityTypeBuilder<LandUseDataPoint> builder)
    {
        builder.ToTable("land_use_data_points");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Location)
            .HasColumnName("location")
            .HasColumnType("geometry(Point, 4326)")
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(x => x.CosCode)
            .HasColumnName("cos_code")
            .HasMaxLength(20);

        builder.Property(x => x.CosDescription)
            .HasColumnName("cos_description")
            .HasMaxLength(200);

        builder.Property(x => x.FuelCategory)
            .HasColumnName("fuel_category")
            .HasMaxLength(50);

        builder.Property(x => x.FuelLoad)
            .HasColumnName("fuel_load");

        builder.Property(x => x.FireRiskMultiplier)
            .HasColumnName("fire_risk_multiplier");

        builder.Property(x => x.DominantSpecies)
            .HasColumnName("dominant_species")
            .HasMaxLength(100);

        builder.Property(x => x.IsWildlandUrbanInterface)
            .HasColumnName("is_wildland_urban_interface")
            .HasDefaultValue(false);

        builder.Property(x => x.GridPointId)
            .HasColumnName("grid_point_id")
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => x.Location).HasMethod("GIST");
        builder.HasIndex(x => x.CosCode);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.GridPointId);
    }
}
