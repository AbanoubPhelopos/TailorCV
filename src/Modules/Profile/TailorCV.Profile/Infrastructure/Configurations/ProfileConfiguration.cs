using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Profile.Domain;

namespace TailorCV.Profile.Infrastructure.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Domain.Profile>
{
    public void Configure(EntityTypeBuilder<Domain.Profile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Headline).HasMaxLength(200);
        builder.Property(p => p.Summary).HasMaxLength(2000);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.Location).HasMaxLength(200);
        builder.Property(p => p.Website).HasMaxLength(500);
        builder.Property(p => p.LinkedinUrl).HasMaxLength(500);
        builder.Property(p => p.GithubUrl).HasMaxLength(500);
        builder.Property(p => p.ShareId).HasMaxLength(100);
        builder.Property(p => p.Sections).HasColumnType("jsonb");

        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasIndex(p => p.ShareId).IsUnique().HasFilter("\"share_id\" IS NOT NULL");
    }
}
