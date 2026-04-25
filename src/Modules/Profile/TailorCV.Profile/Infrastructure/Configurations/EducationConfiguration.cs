using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Profile.Domain;

namespace TailorCV.Profile.Infrastructure.Configurations;

public class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.ToTable("education", "profile");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProfileId).IsRequired();
        builder.Property(e => e.Institution).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Degree).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Field).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Gpa).HasMaxLength(20);
    }
}
