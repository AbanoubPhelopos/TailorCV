using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.JobDescriptions.Domain;
using TailorCV.JobDescriptions.Domain.Enums;

namespace TailorCV.JobDescriptions.Infrastructure.Configurations;

public class ParseJobConfiguration : IEntityTypeConfiguration<ParseJob>
{
    public void Configure(EntityTypeBuilder<ParseJob> builder)
    {
        builder.ToTable("parse_jobs", "jobdescriptions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Type).IsRequired().HasConversion<string>();
        builder.Property(p => p.RawText).IsRequired();
        builder.Property(p => p.Status).IsRequired().HasConversion<string>();
        builder.Property(p => p.Error);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CompletedAt);
        builder.Property(p => p.SourceUrl).HasMaxLength(2048);

        builder.ComplexProperty(p => p.ParsedData, d => d.ToJson());

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Status);
    }
}
