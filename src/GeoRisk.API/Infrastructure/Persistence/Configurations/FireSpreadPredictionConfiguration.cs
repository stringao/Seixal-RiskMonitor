using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class FireSpreadPredictionConfiguration : IEntityTypeConfiguration<FireSpreadPrediction>
{
    public void Configure(EntityTypeBuilder<FireSpreadPrediction> builder)
    {
        builder.ToTable("FireSpreadPredictions");

        builder.Property(e => e.Polygon)
            .HasColumnType("geometry(Polygon, 4326)");

        builder.HasIndex(e => e.Polygon)
            .HasMethod("GIST");

        builder.HasIndex(e => e.FireEventId);

        builder.HasIndex(e => new { e.FireEventId, e.HorizonHours });

        builder.Property(e => e.AffectedMunicipalities)
            .HasColumnType("jsonb");

        builder.HasOne(e => e.FireEvent)
            .WithMany()
            .HasForeignKey(e => e.FireEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}