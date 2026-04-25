using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Profile.Domain;

namespace TailorCV.Profile.Infrastructure.Configurations;

public class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.ToTable("experiences", "profile");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProfileId).IsRequired();
        builder.Property(e => e.Company).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
    }
}
