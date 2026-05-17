using Microsoft.EntityFrameworkCore;
using TailorCV.CVGenerator.Domain;

namespace TailorCV.CVGenerator.Infrastructure;

public class CVGeneratorDbContext : DbContext
{
    public DbSet<GeneratedCV> GeneratedCVs => Set<GeneratedCV>();

    public CVGeneratorDbContext(DbContextOptions<CVGeneratorDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cvgenerator");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CVGeneratorDbContext).Assembly);
    }
}
