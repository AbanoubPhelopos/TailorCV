using Microsoft.EntityFrameworkCore;
using TailorCV.Templates.Domain;

namespace TailorCV.Templates.Infrastructure;

public class TemplatesDbContext : DbContext
{
    public DbSet<Template> Templates => Set<Template>();

    public TemplatesDbContext(DbContextOptions<TemplatesDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("templates");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TemplatesDbContext).Assembly);
    }
}
