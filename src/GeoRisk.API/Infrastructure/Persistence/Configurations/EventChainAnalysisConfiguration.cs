using GeoRisk.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

public class EventChainAnalysisConfiguration : IEntityTypeConfiguration<EventChainAnalysis>
{
    public void Configure(EntityTypeBuilder<EventChainAnalysis> builder)
    {
        builder.ToTable("event_chain_analyses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.AnalysisType)
            .HasColumnName("analysis_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PrimaryEventId)
            .HasColumnName("primary_event_id");

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(x => x.ConfidenceScore)
            .HasColumnName("confidence_score");

        builder.Property(x => x.Findings)
            .HasColumnName("findings")
            .HasColumnType("jsonb");

        builder.Property(x => x.ContributingFactors)
            .HasColumnName("contributing_factors")
            .HasColumnType("text[]");

        builder.Property(x => x.RecommendedAction)
            .HasColumnName("recommended_action")
            .HasMaxLength(500);

        builder.Property(x => x.AnalyzedAt)
            .HasColumnName("analyzed_at")
            .IsRequired();

        builder.HasIndex(x => x.AnalysisType);
        builder.HasIndex(x => x.PrimaryEventId);
        builder.HasIndex(x => x.AnalyzedAt);
        builder.HasIndex(x => x.ConfidenceScore);
    }
}
