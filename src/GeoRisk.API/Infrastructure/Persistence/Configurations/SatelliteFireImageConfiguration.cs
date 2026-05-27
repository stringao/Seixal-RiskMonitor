using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class SatelliteFireImageConfiguration : IEntityTypeConfiguration<SatelliteFireImage>
{
    public void Configure(EntityTypeBuilder<SatelliteFireImage> builder)
    {
        builder.ToTable("satellite_fire_images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.LinkedEventId)
            .HasColumnName("linked_event_id");

        builder.Property(x => x.CaptureTime)
            .HasColumnName("capture_time")
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(x => x.FullResolutionUrl)
            .HasColumnName("full_resolution_url")
            .HasMaxLength(500);

        builder.Property(x => x.Footprint)
            .HasColumnName("footprint")
            .HasColumnType("geometry(Polygon, 4326)");

        builder.Property(x => x.FireRadiativePower)
            .HasColumnName("fire_radiative_power");

        builder.Property(x => x.BrightnessTemperature)
            .HasColumnName("brightness_temperature");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.DetectionConfidence)
            .HasColumnName("detection_confidence")
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Index for queries
        builder.HasIndex(x => x.CaptureTime);
        builder.HasIndex(x => x.LinkedEventId);
    }
}
