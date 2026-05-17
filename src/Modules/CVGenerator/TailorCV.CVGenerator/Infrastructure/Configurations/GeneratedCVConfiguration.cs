using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.CVGenerator.Domain;

namespace TailorCV.CVGenerator.Infrastructure.Configurations;

public class GeneratedCVConfiguration : IEntityTypeConfiguration<GeneratedCV>
{
    public void Configure(EntityTypeBuilder<GeneratedCV> builder)
    {
        builder.ToTable("generated_cvs");

        builder.Property(e => e.Status)
            .HasConversion<string>();

        builder.Property(e => e.GenerationType)
            .HasConversion<string>();

        builder.Property(e => e.PdfStatus)
            .HasConversion<string>();

        builder.Property(e => e.ProfileSnapshot)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.JobSnapshot)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Content)
            .HasColumnType("jsonb");

        builder.Property(e => e.MatchScore)
            .HasColumnType("jsonb");

        builder.Property(e => e.TailoringPrompt)
            .HasMaxLength(2000);

        builder.Property(e => e.Error)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
    }
}
