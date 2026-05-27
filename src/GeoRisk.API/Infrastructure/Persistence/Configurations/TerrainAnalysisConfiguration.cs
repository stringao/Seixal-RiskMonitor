using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class TerrainAnalysisConfiguration : IEntityTypeConfiguration<TerrainAnalysis>
{
    public void Configure(EntityTypeBuilder<TerrainAnalysis> builder)
    {
        builder.ToTable("terrain_analyses");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.GridPointId)
            .HasColumnName("grid_point_id")
            .HasMaxLength(50);

        builder.Property(t => t.Location)
            .HasColumnName("location")
            .HasColumnType("geometry(Point, 4326)");

        builder.Property(t => t.ElevationMeters)
            .HasColumnName("elevation_meters");

        builder.Property(t => t.SlopeDegrees)
            .HasColumnName("slope_degrees");

        builder.Property(t => t.AspectDegrees)
            .HasColumnName("aspect_degrees");

        builder.Property(t => t.SolarExposureIndex)
            .HasColumnName("solar_exposure_index");

        builder.Property(t => t.TerrainComplexity)
            .HasColumnName("terrain_complexity")
            .HasMaxLength(20);

        builder.Property(t => t.NorthFacingPercent)
            .HasColumnName("north_facing_percent");

        builder.Property(t => t.TerrainRiskScore)
            .HasColumnName("terrain_risk_score");

        builder.Property(t => t.TerrainFireRiskContribution)
            .HasColumnName("terrain_fire_risk_contribution")
            .HasMaxLength(20);

        builder.Property(t => t.CalculatedAt)
            .HasColumnName("calculated_at");

        builder.Property(t => t.Latitude)
            .HasColumnName("latitude");

        builder.Property(t => t.Longitude)
            .HasColumnName("longitude");

        // Index for spatial queries
        builder.HasIndex(t => t.Location)
            .HasDatabaseName("ix_terrain_analyses_location");

        // Index for grid point lookups
        builder.HasIndex(t => t.GridPointId)
            .HasDatabaseName("ix_terrain_analyses_grid_point_id");
    }
}