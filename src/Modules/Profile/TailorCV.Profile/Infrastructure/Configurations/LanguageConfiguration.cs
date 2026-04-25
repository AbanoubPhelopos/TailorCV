using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Profile.Domain;

namespace TailorCV.Profile.Infrastructure.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("languages", "profile");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProfileId).IsRequired();
        builder.Property(l => l.LanguageName).IsRequired().HasMaxLength(100);
    }
}
