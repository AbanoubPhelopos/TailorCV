using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Domain.Enums;

namespace TailorCV.Profile.Infrastructure.Configurations;

public class SectionOrderConfiguration : IEntityTypeConfiguration<SectionOrder>
{
    public void Configure(EntityTypeBuilder<SectionOrder> builder)
    {
        builder.ToTable("section_orders", "profile");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProfileId).IsRequired();
        builder.Property(s => s.SectionId).IsRequired();
        builder.Property(s => s.SectionType).IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<SectionType>(v));
        builder.HasIndex(s => new { s.ProfileId, s.SectionId }).IsUnique();
    }
}
