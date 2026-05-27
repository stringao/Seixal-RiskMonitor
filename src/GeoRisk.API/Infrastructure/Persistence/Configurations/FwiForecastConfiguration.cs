using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class FwiForecastConfiguration : IEntityTypeConfiguration<FwiForecast>
{
    public void Configure(EntityTypeBuilder<FwiForecast> builder)
    {
        builder.ToTable("FwiForecasts");

        builder.Property(e => e.Location)
            .HasColumnType("geometry(Point, 4326)");

        builder.HasIndex(e => e.Location)
            .HasMethod("GIST");

        builder.HasIndex(e => e.ForecastDate);

        builder.HasIndex(e => e.GridPointId);

        builder.HasIndex(e => e.HorizonDays);

        // Composite index for querying forecasts by date range
        builder.HasIndex(e => new { e.ForecastDate, e.HorizonDays });
    }
}