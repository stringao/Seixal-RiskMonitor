using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class FireResourceConfiguration : IEntityTypeConfiguration<FireResource>
{
    public void Configure(EntityTypeBuilder<FireResource> builder)
    {
        builder.ToTable("fire_resources");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.FireStationId)
            .HasColumnName("fire_station_id")
            .IsRequired();

        builder.Property(x => x.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PersonnelCount)
            .HasColumnName("personnel_count");

        builder.Property(x => x.WaterCapacityLiters)
            .HasColumnName("water_capacity_liters");

        builder.Property(x => x.IsAvailable)
            .HasColumnName("is_available")
            .HasDefaultValue(true);

        builder.Property(x => x.DeployedToEventId)
            .HasColumnName("deployed_to_event_id");

        builder.Property(x => x.DeployedAt)
            .HasColumnName("deployed_at");

        builder.Property(x => x.ExpectedReturnAt)
            .HasColumnName("expected_return_at");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("Available");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(x => x.FireStation)
            .WithMany(s => s.FireResources)
            .HasForeignKey(x => x.FireStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsAvailable);
        builder.HasIndex(x => x.FireStationId);
    }
}
