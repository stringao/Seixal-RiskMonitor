namespace GeoRisk.API.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AiProviderConfigConfiguration : IEntityTypeConfiguration<AiProviderConfig>
{
    public void Configure(EntityTypeBuilder<AiProviderConfig> builder)
    {
        builder.ToTable("AiProviderConfigs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ApiKey).HasMaxLength(500);
        builder.Property(x => x.Model).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BaseUrl).HasMaxLength(200);
        builder.Property(x => x.MaxTokens).HasDefaultValue(1024);
        builder.Property(x => x.IsEnabled).HasDefaultValue(true);

        // Seed data for all providers
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new AiProviderConfig { Id = 1, Provider = "DeepSeek", ApiKey = "", Model = "deepseek-chat", MaxTokens = 1024, IsEnabled = true, CreatedAt = now, UpdatedAt = now },
            new AiProviderConfig { Id = 2, Provider = "Ollama", ApiKey = "", Model = "qwen3:8b", BaseUrl = "http://localhost:11434/v1", MaxTokens = 1024, IsEnabled = true, CreatedAt = now, UpdatedAt = now },
            new AiProviderConfig { Id = 3, Provider = "Qwen", ApiKey = "", Model = "qwen-turbo", MaxTokens = 1024, IsEnabled = true, CreatedAt = now, UpdatedAt = now },
            new AiProviderConfig { Id = 4, Provider = "Anthropic", ApiKey = "", Model = "claude-sonnet-4-20250514", MaxTokens = 1024, IsEnabled = true, CreatedAt = now, UpdatedAt = now },
            new AiProviderConfig { Id = 5, Provider = "OpenAI", ApiKey = "", Model = "gpt-4o", MaxTokens = 1024, IsEnabled = true, CreatedAt = now, UpdatedAt = now }
        );
    }
}