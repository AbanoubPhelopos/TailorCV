using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TailorCV.Templates.Infrastructure;

public class TemplatesDbContextFactory : IDesignTimeDbContextFactory<TemplatesDbContext>
{
    public TemplatesDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<TemplatesDbContext> optionsBuilder = new();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=tailorcv;Username=tailorcv;Password=tailorcv_secret",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "templates"))
            .UseSnakeCaseNamingConvention();

        return new TemplatesDbContext(optionsBuilder.Options);
    }
}
