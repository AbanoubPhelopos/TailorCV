using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.JobScraper.Domain;
using TailorCV.JobScraper.Domain.Enums;

namespace TailorCV.JobScraper.Infrastructure.Configurations;

public class ParseJobConfiguration : IEntityTypeConfiguration<ParseJob>
{
    public void Configure(EntityTypeBuilder<ParseJob> builder)
    {
        builder.ToTable("parse_jobs", "jobscraper");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Type).IsRequired().HasConversion<string>();
        builder.Property(p => p.RawInput).IsRequired();
        builder.Property(p => p.Status).IsRequired().HasConversion<string>();
        builder.Property(p => p.Error);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CompletedAt);

        builder.ComplexProperty(p => p.ParsedData, d => d.ToJson());

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Status);
    }
}
