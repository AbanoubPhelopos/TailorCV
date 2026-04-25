using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Profile.Domain;

namespace TailorCV.Profile.Infrastructure.Configurations;

public class CustomSectionConfiguration : IEntityTypeConfiguration<CustomSection>
{
    public void Configure(EntityTypeBuilder<CustomSection> builder)
    {
        builder.ToTable("custom_sections", "profile");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ProfileId).IsRequired();
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Items)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<CustomSectionItem[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<CustomSectionItem>());
    }
}
