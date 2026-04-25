using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TailorCV.Profile.Infrastructure;

public class ProfileDbContextFactory : IDesignTimeDbContextFactory<ProfileDbContext>
{
    public ProfileDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ProfileDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=tailorcv;Username=tailorcv;Password=tailorcv_secret",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "profile"))
            .UseSnakeCaseNamingConvention();

        return new ProfileDbContext(optionsBuilder.Options);
    }
}
