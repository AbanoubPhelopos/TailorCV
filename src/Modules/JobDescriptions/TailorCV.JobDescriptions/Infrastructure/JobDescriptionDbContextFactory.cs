using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TailorCV.JobDescriptions.Infrastructure;

public class JobDescriptionsDbContextFactory : IDesignTimeDbContextFactory<JobDescriptionsDbContext>
{
    public JobDescriptionsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<JobDescriptionsDbContext> optionsBuilder = new();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=tailorcv;Username=tailorcv;Password=tailorcv_secret",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "jobdescription"))
            .UseSnakeCaseNamingConvention();

        return new JobDescriptionsDbContext(optionsBuilder.Options);
    }
}