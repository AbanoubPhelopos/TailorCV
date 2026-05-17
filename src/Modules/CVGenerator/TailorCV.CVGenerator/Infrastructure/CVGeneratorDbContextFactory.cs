using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TailorCV.CVGenerator.Infrastructure;

public class CVGeneratorDbContextFactory : IDesignTimeDbContextFactory<CVGeneratorDbContext>
{
    public CVGeneratorDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<CVGeneratorDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=tailorcv;Username=tailorcv;Password=changeme_strong_password",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "cvgenerator"))
            .UseSnakeCaseNamingConvention();

        return new CVGeneratorDbContext(optionsBuilder.Options);
    }
}
