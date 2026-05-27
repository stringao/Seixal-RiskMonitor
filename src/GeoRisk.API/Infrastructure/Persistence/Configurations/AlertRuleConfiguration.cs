using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    private const string DecimalType = "decimal(8,2)";

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");

        builder.Property(e => e.Area)
            .HasColumnType("geometry(Polygon, 4326)");

        builder.Property(e => e.MinFwi)
            .HasColumnType(DecimalType);

        builder.Property(e => e.MaxFwi)
            .HasColumnType(DecimalType);

        builder.Property(e => e.MinWindSpeed)
            .HasColumnType(DecimalType);

        builder.Property(e => e.MinTemperature)
            .HasColumnType(DecimalType);

        builder.Property(e => e.AreaKm2Threshold)
            .HasColumnType("decimal(10,4)");

        builder.Property(e => e.NotifyRoles)
            .HasColumnType("text[]");
    }
}
