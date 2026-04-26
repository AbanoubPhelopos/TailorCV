using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Domain.Enums;

namespace TailorCV.Profile.Infrastructure.Configurations;

public class ParseJobConfiguration : IEntityTypeConfiguration<ParseJob>
{
    public void Configure(EntityTypeBuilder<ParseJob> builder)
    {
        builder.ToTable("parse_jobs");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.S3Key).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Status).IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ParseJobStatus>(v));
        builder.Property(p => p.Error).HasMaxLength(2000);

        builder.ComplexProperty(p => p.ParsedData, d => d.ToJson());
    }
}
