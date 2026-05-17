using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TailorCV.Templates.Domain;

namespace TailorCV.Templates.Infrastructure.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("templates", "templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(1000);
        builder.Property(t => t.HtmlContent).IsRequired();
        builder.Property(t => t.CssContent).IsRequired();
        builder.Property(t => t.ThumbnailUrl).IsRequired().HasMaxLength(2048);
        builder.Property(t => t.Category).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Style).IsRequired().HasMaxLength(50);
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => t.IsActive);
    }
}
