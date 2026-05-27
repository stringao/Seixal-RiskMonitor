using Microsoft.EntityFrameworkCore;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasOne(a => a.GeoEvent)
            .WithMany()
            .HasForeignKey(a => a.GeoEventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.AlertRule)
            .WithMany()
            .HasForeignKey(a => a.AlertRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.EscalatedFromAlert)
            .WithMany()
            .HasForeignKey(a => a.EscalatedFromAlertId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.IsRead);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.AlertRuleId);
    }
}
