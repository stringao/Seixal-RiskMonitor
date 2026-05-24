namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");

        builder.Property(e => e.Area)
            .HasColumnType("geometry(Polygon, 4326)");
    }
}
