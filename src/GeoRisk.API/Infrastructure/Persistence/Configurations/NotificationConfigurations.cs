using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("PushSubscriptions");

        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => new { p.UserId, p.IsActive });
    }
}

public class CitizenAlertSubscriptionConfiguration : IEntityTypeConfiguration<CitizenAlertSubscription>
{
    public void Configure(EntityTypeBuilder<CitizenAlertSubscription> builder)
    {
        builder.ToTable("CitizenAlertSubscriptions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Location)
            .HasColumnType("geometry(Point, 4326)");

        builder.Property(c => c.EventTypes)
            .HasColumnType("text[]");

        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.Phone);
    }
}

public class NotificationQueueItemConfiguration : IEntityTypeConfiguration<NotificationQueueItem>
{
    public void Configure(EntityTypeBuilder<NotificationQueueItem> builder)
    {
        builder.ToTable("NotificationQueueItems");

        builder.HasKey(n => n.Id);

        builder.HasOne(n => n.Alert)
            .WithMany()
            .HasForeignKey(n => n.AlertId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.PushSubscription)
            .WithMany()
            .HasForeignKey(n => n.PushSubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.CitizenSubscription)
            .WithMany()
            .HasForeignKey(n => n.CitizenSubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.CreatedAt);
    }
}