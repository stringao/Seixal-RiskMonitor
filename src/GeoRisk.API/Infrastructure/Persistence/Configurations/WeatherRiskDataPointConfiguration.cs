using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class WeatherRiskDataPointConfiguration : IEntityTypeConfiguration<WeatherRiskDataPoint>
{
    public void Configure(EntityTypeBuilder<WeatherRiskDataPoint> builder)
    {
        builder.ToTable("WeatherRiskDataPoints");

        builder.Property(e => e.Location)
            .HasColumnType("geometry(Point, 4326)");

        builder.HasIndex(e => e.Location)
            .HasMethod("GIST");

        builder.HasIndex(e => e.Timestamp);

        builder.HasIndex(e => e.GridPointId);
    }
}