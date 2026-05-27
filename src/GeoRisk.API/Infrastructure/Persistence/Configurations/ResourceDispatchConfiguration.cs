using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class ResourceDispatchConfiguration : IEntityTypeConfiguration<ResourceDispatch>
{
    public void Configure(EntityTypeBuilder<ResourceDispatch> builder)
    {
        builder.ToTable("resource_dispatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(x => x.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();

        builder.Property(x => x.DispatchedAt)
            .HasColumnName("dispatched_at")
            .IsRequired();

        builder.Property(x => x.ArrivedAt)
            .HasColumnName("arrived_at");

        builder.Property(x => x.DistanceKm)
            .HasColumnName("distance_km");

        builder.Property(x => x.TravelTimeMinutes)
            .HasColumnName("travel_time_minutes");

        builder.Property(x => x.RoutePolyline)
            .HasColumnName("route_polyline")
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("Dispatched");

        builder.HasOne(x => x.Event)
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Resource)
            .WithMany()
            .HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => x.ResourceId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DispatchedAt);
    }
}
