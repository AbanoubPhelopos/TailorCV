using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Profile.Domain;

namespace TailorCV.Profile.Infrastructure.Configurations;

public class ProfileUserConfiguration : IEntityTypeConfiguration<ProfileUser>
{
    public void Configure(EntityTypeBuilder<ProfileUser> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserId).IsRequired();
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);

        builder.HasIndex(u => u.UserId).IsUnique();
    }
}
