using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.JobScraper.Domain;
using TailorCV.JobScraper.Domain.Enums;

namespace TailorCV.JobScraper.Infrastructure.Configurations;

public class JobDescriptionConfiguration : IEntityTypeConfiguration<JobDescription>
{
    public void Configure(EntityTypeBuilder<JobDescription> builder)
    {
        builder.ToTable("job_descriptions", "jobscraper");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.UserId).IsRequired();
        builder.Property(j => j.Title).IsRequired().HasMaxLength(200);
        builder.Property(j => j.Company).IsRequired().HasMaxLength(200);
        builder.Property(j => j.Location).HasMaxLength(200);
        builder.Property(j => j.SeniorityLevel).HasConversion<string>().HasMaxLength(50);
        builder.Property(j => j.SourceUrl).HasMaxLength(2048);
        builder.Property(j => j.Label).HasMaxLength(100);
        builder.Property(j => j.RawText).HasMaxLength(10000);
        builder.Property(j => j.CreatedAt).IsRequired();
        builder.Property(j => j.UpdatedAt).IsRequired();

        builder.HasIndex(j => j.UserId);
    }
}
