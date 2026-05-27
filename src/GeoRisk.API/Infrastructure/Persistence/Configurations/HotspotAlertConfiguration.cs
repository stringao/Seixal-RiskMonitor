using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class HotspotAlertConfiguration : IEntityTypeConfiguration<HotspotAlert>
{
    public void Configure(EntityTypeBuilder<HotspotAlert> builder)
    {
        builder.ToTable("HotspotAlerts");

        builder.HasOne(e => e.GeoEvent)
            .WithMany()
            .HasForeignKey(e => e.GeoEventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.AlertType);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.IsRead);
    }
}